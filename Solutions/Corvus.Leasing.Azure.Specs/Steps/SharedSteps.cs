// <copyright file="SharedSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#pragma warning disable

namespace Corvus.Leasing.Azure.Specs.Steps
{
    #region Using Directives

    using System;
    using System.Threading.Tasks;
    using Corvus.SpecFlow.Extensions;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    #endregion Using Directives

    [Binding]
    public class SharedSteps
    {
        [AfterScenario("ReleaseLeases")]
        public async Task ReleaseLeases(ScenarioContext scenarioContext)
        {
            await scenarioContext.RunAndStoreExceptionsAsync(
                async () =>
                {
                    if (scenarioContext.TryGetValue<Lease>(out var lease) && lease != null)
                    {
                        await lease.ReleaseAsync().ConfigureAwait(false);
                    }
                });

            await scenarioContext.RunAndStoreExceptionsAsync(
                async () =>
                {
                    if (scenarioContext.TryGetValue<Lease>("ActorALease", out var existingLease))
                    {
                        await existingLease.ReleaseAsync().ConfigureAwait(false);
                    }
                });
        }

        [Then(@"it should not throw any exceptions")]
        public void ThenItShouldNotThrowAnyExceptions()
        {
            Exception exception;
            AggregateException aggregateException;
            var hasException = ScenarioContext.Current.TryGetValue("Exception", out exception) || ScenarioContext.Current.TryGetValue("AggregateException", out aggregateException);
            Assert.False(hasException);
        }

        [Then(@"it should throw a (.*)")]
        public void ThenItShouldThrowAn(string exceptionName)
        {
            var exception = ScenarioContext.Current.Get<Exception>("Exception");

            Assert.AreEqual(exceptionName, exception.GetType().Name);
        }

        [Then(@"it should throw an AggregateException containing (.*)")]
        public void ThenItShouldThrowAnContainingA(string innerExceptionName)
        {
            var exception = ScenarioContext.Current.Get<AggregateException>("AggregateException");

            Assert.AreEqual(innerExceptionName, exception.InnerException.GetType().Name);
        }
    }
}

#pragma warning restore