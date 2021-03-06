﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BDTest.Test.Steps.Then
{
    public class Then : StepBuilder
    {

        protected override StepType StepType { get; } = StepType.Then;

        protected internal Then(List<Step> previousSteps, Expression<Action> action, TestDetails testDetails) : base(previousSteps, action, testDetails)
        {
        }

        public AndThen And(Expression<Action> step)
        {
            return new AndThen(ExistingSteps, step, TestDetails);
        }

        public Scenario BDTest()
        {
            return Invoke(TestDetails);
        }

        public Task<Scenario> BDTestAsync()
        {
            return Task.Run(() => Invoke(TestDetails));
        }
    }
}
