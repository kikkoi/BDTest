﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using BDTest.Maps;
using BDTest.Output;
using BDTest.Reporters;

namespace BDTest.Test
{
    public class Scenario
    {

        static Scenario()
        {
            if (BDTestSettings.InterceptConsoleOutput)
            {
                Console.SetOut(TestOutputData.Instance);
            }
        }

        private readonly Reporter _reporters;

        [JsonProperty]
        public DateTime StartTime;

        [JsonProperty]
        public DateTime EndTime;

        [JsonConstructor]
        private Scenario() { }

        internal Scenario(List<Step> steps, TestDetails testDetails)
        {
            TestMap.NotRun.TryRemove(testDetails.GetGuid(), out _);
            TestMap.StoppedEarly.TryAdd(testDetails.GetGuid(), this);

            StoryText = testDetails.StoryText;
            ScenarioText = testDetails.ScenarioText;

            _reporters = new Reporters.Reporters();
            Steps = steps;
            
            try
            {
                Execute();
            }
            finally
            {
                JsonLogger.WriteScenario(this);
                TestMap.StoppedEarly.TryRemove(testDetails.GetGuid(), out _);
            }
        }

        [JsonIgnore]
        public bool IsAsync { get; set; }

        [JsonProperty]
        public List<Step> Steps { get; set; }

        [JsonProperty]
        public string Output { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty]
        public Status Status { get; set; } = Status.Inconclusive;

        [JsonProperty] public StoryText StoryText;

        [JsonProperty] public ScenarioText ScenarioText;

        public string GetScenarioText()
        {
            return ScenarioText?.Scenario ?? "Scenario Title Not Defined";
        }

        public string GetStoryText()
        {
            return StoryText?.Story ?? "Story Text Not Defined";
        }

        [JsonConverter(typeof(TimespanConverter))]
        [JsonProperty]
        public TimeSpan TimeTaken;

        internal void Execute()
        {
            var task = new Task( () => 
            {
                try
                {
                    StartTime = DateTime.Now;
                    _reporters.WriteStory(StoryText);
                    _reporters.WriteScenario(ScenarioText);
                    Steps.ForEach(step => _reporters.WriteLine(step.StepText));
                    _reporters.NewLine();
                    Steps.ForEach(step => step.Execute());
                    Status = Status.Passed;
                }
                catch (NotImplementedException)
                {
                    Status = Status.NotImplemented;
                }
                catch (Exception e)
                {
                    Status = Status.Failed;
                    _reporters.WriteLine($"Exception: {e.StackTrace}");
                    throw;
                }
                finally
                {
                    _reporters.WriteLine($"{Environment.NewLine}Test Result: {Status}");
                    EndTime = DateTime.Now;
                    TimeTaken = EndTime - StartTime;
                    Output = string.Join(Environment.NewLine, Steps.Where(step => !string.IsNullOrWhiteSpace(step.Output)).Select(step => step.Output));
                }
            });

            task.Start();
            task.Wait();
        }
    }
}
