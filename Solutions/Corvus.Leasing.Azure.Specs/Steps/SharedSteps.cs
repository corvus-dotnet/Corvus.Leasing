// <copyright file="SharedSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#pragma warning disable

namespace Corvus.Leasing.Azure.Specs.Steps
{
    #region Using Directives

    using System;
    using System.Threading.Tasks;
    using Corvus.Testing.SpecFlow;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    #endregion Using Directives

    [Binding]
    public class SharedSteps
    {
        private readonly ScenarioContext scenarioContext;

        public SharedSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

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
            var hasException = this.scenarioContext.TryGetValue("Exception", out exception) || this.scenarioContext.TryGetValue("AggregateException", out aggregateException);
            Assert.False(hasException, exception?.Message);
        }

        [Then(@"it should throw a (.*)")]
        public void ThenItShouldThrowAn(string exceptionName)
        {
            var exception = this.scenarioContext.Get<Exception>("Exception");

            Assert.AreEqual(exceptionName, exception.GetType().Name);
        }

        [Then(@"it should throw an AggregateException containing (.*)")]
        public void ThenItShouldThrowAnContainingA(string innerExceptionName)
        {
            var exception = this.scenarioContext.Get<AggregateException>("AggregateException");

            Assert.AreEqual(innerExceptionName, exception.InnerException.GetType().Name);
        }
    }
}

#pragma warning restore