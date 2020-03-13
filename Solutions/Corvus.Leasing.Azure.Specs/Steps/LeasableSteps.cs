// <copyright file="LeasableSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#pragma warning disable

namespace Corvus.Leasing.Azure.Specs.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Leasing;
    using Corvus.Leasing.Azure.Specs.Helpers;
    using Corvus.Leasing.Retry.Policies;
    using Corvus.Retry.Policies;
    using Corvus.Retry.Strategies;
    using Corvus.SpecFlow.Extensions;
    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    [Binding]
    public class LeasableSteps
    {
        private const string LeaseDuration = "LeaseDuration";

        private const string LeaseName = "LeaseName";

        private const string LeaseNames = "LeaseNames";

        private const string Result = "Result";

        private const string RetryPolicy = "RetryPolicy";

        private const string RetryStrategy = "RetryStrategy";

        private const string TaskDuration = "TaskDuration";

        private const string TaskResult = "TaskResult";

        private const string Tasks = "Tasks";
        private readonly FeatureContext featureContext;
        private readonly ScenarioContext scenarioContext;

        public LeasableSteps(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            this.featureContext = featureContext;
            this.scenarioContext = scenarioContext;
        }

        [Given(@"actor A executes the task")]
        public void GivenActorAExecutesTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = this.scenarioContext.Get<string>(LeaseName);

            var task = provider.ExecuteWithMutexAsync(this.DoSomething, leaseName, actorName: "Actor A");
            SetContinuations(this.scenarioContext, task);
            AddToTasks(this.scenarioContext, task);
        }

        [Given(@"actor A executes the task with a try once mutex")]
        public void GivenActorAExecutesTheTaskWithATryOnceMutex()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = this.scenarioContext.Get<string>(LeaseName);

            var task = provider.ExecuteWithMutexTryOnceAsync(this.DoSomething, leaseName, actorName: "Actor A");
            SetContinuations(this.scenarioContext, task);
            AddToTasks(this.scenarioContext, task);
        }

        [Given(@"actor A executes the task with options")]
        public void GivenActorAExecutesTheTaskWithOptions()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();

            string leaseName;
            TimeSpan leaseDuration;
            IRetryPolicy retryPolicy;
            IRetryStrategy retryStrategy;

            this.scenarioContext.TryGetValue(LeaseName, out leaseName);
            this.scenarioContext.TryGetValue(LeaseDuration, out leaseDuration);
            this.scenarioContext.TryGetValue(RetryPolicy, out retryPolicy);
            this.scenarioContext.TryGetValue(RetryStrategy, out retryStrategy);

            var policy = new LeasePolicy { Name = leaseName, ActorName = "Actor A", Duration = leaseDuration };

            var task = provider.ExecuteWithMutexAsync(this.DoSomething, policy, retryStrategy, retryPolicy);
            SetContinuations(this.scenarioContext, task);
            AddToTasks(this.scenarioContext, task);
        }

        [Given(@"actor B is currently running the task")]
        public async Task GivenActorBIsCurrentlyRunningTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = this.scenarioContext.Get<string>(LeaseName);

            var started = new TaskCompletionSource<object>();
            var task = provider.ExecuteWithMutexAsync(ct => this.DoSomething(ct, started), leaseName, actorName: "Actor B");

            AddToTasks(this.scenarioContext, task);

            // The task only gets completed inside the mutex action, so we know the lease has been obtained at that point.
            // We also await the task returned by ExecuteWithMutexAsync, so that if it fails with an error before running
            // our action, we don't just sit here forever.
            await Task.WhenAny(started.Task, task);
        }

        [Given(@"the lease duration is (.*) seconds")]
        public void GivenTheLeaseDurationIsSeconds(int leaseDurationInSeconds)
        {
            this.scenarioContext.Set(TimeSpan.FromSeconds(leaseDurationInSeconds), LeaseDuration);
        }

        [Given(@"the lease name is ""(.*)""")]
        public void GivenTheLeaseNameIs(string leaseName)
        {
            this.scenarioContext.Set($"{leaseName}_{Guid.NewGuid()}", LeaseName);
        }

        [Given(@"the lease names are")]
        public void GivenTheLeaseNamesAre(Table table)
        {
            var leaseNames = table.CreateSet<LeaseName>();
            this.scenarioContext.Set(leaseNames, LeaseNames);
        }

        [Given(@"the long running task takes (.*) seconds to complete")]
        public void GivenTheLongRunningTaskTakesSecondsToComplete(double durationInSeconds)
        {
            var duration = TimeSpan.FromSeconds(durationInSeconds);

            this.scenarioContext.Set(duration, TaskDuration);
        }

        [Given(@"we use a do not retry on lease acquisition unsuccessful policy")]
        public void GivenWeUseADoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy()
        {
            var policy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();
            this.scenarioContext.Set(policy, RetryPolicy);
        }

        [Given(@"we use a do not retry policy")]
        public void GivenWeUseADoNotRetryPolicy()
        {
            var policy = new DoNotRetryPolicy();
            this.scenarioContext.Set(policy, RetryPolicy);
        }

        [Given(@"we use a linear retry strategy with periodicity of (.*) seconds and (.*) max retries")]
        public void GivenWeUseALinearRetryStrategy(int periodicityInSeconds, int maxRetries)
        {
            var strategy = new Linear(TimeSpan.FromSeconds(periodicityInSeconds), maxRetries);
            this.scenarioContext.Set(strategy, RetryStrategy);
        }

        [Given(@"we use no lease policy")]
        public void GivenWeUseNoLeasePolicy()
        {
        }

        [Given(@"we use no retry strategy")]
        public void GivenWeUseNoRetryStrategy()
        {
        }

        [Then(@"(.*) action\(s\) should have completed successfully")]
        public void ThenActionSShouldHaveCompletedSuccessfully(int numberOfActions)
        {
            int actionsCompleted;

            if (!this.scenarioContext.TryGetValue("ActionsCompleted", out actionsCompleted))
            {
                actionsCompleted = 0;
            }

            Assert.AreEqual(numberOfActions, actionsCompleted);
        }

        [Then(@"after (.*) seconds")]
        public void ThenAfterSeconds(int seconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        [Then(@"all leases should be disposed")]
        public void ThenAllLeasesShouldBeDisposed()
        {
            Console.WriteLine("Check here...");
        }

        [Then(@"it should return successfully")]
        public void ThenItShouldReturnSuccessfully()
        {
            var result = this.scenarioContext.Get<bool>(Result);
            Assert.True(result);
        }

        [Then(@"it should return unsuccessfully")]
        public void ThenItShouldReturnUnsuccessfully()
        {
            var result = this.scenarioContext.Get<bool>(Result);
            Assert.False(result);
        }

        [Then(@"the task result should be correct")]
        public void ThenTheTaskResultShouldBeCorrect()
        {
            var result = this.scenarioContext.Get<bool>(TaskResult);
            Assert.True(result);
        }

        [When(@"I execute the task")]
        public async Task WhenIExecuteTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = this.scenarioContext.Get<string>(LeaseName);

            try
            {
                await provider.ExecuteWithMutexAsync(this.DoSomething, leaseName, actorName: "Actor A");
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(true, Result);
        }

        [When(@"I execute the task using all the leases")]
        public async Task WhenIExecuteTheTaskUsingAllTheLeases()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseNames = this.scenarioContext.Get<IEnumerable<LeaseName>>(LeaseNames).Select(l => l.Name);

            try
            {
                await provider.ExecuteWithMutexAsync(this.DoSomething, leaseNames, actorName: "Actor A");
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(true, Result);
        }

        [When(@"I execute the task with a result")]
        public void WhenIExecuteTheTaskWithAResult()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = this.scenarioContext.Get<string>(LeaseName);

            var result = false;

            try
            {
                result = provider.ExecuteWithMutexAsync(this.DoSomethingWithResult, leaseName, actorName: "Actor A")
                    .Result;
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(true, Result);
            this.scenarioContext.Set(result, TaskResult);
        }

        [When(@"I execute the task with options")]
        public async Task WhenIExecuteTheTaskWithOptions()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();

            string leaseName;
            TimeSpan leaseDuration;
            IRetryPolicy retryPolicy;
            IRetryStrategy retryStrategy;
            LeasePolicy policy = null;

            this.scenarioContext.TryGetValue(LeaseName, out leaseName);
            this.scenarioContext.TryGetValue(LeaseDuration, out leaseDuration);
            this.scenarioContext.TryGetValue(RetryPolicy, out retryPolicy);
            this.scenarioContext.TryGetValue(RetryStrategy, out retryStrategy);

            if (leaseName != null || leaseDuration != default(TimeSpan))
            {
                policy = new LeasePolicy { Name = leaseName, ActorName = "Actor A", Duration = leaseDuration };
            }

            try
            {
                await provider.ExecuteWithMutexAsync(this.DoSomething, policy, retryStrategy, retryPolicy);
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(true, Result);
        }

        [When(@"the tasks have completed")]
        public async Task WhenTheTasksHaveCompleted()
        {
            var tasks = this.scenarioContext.Get<List<Task>>(Tasks);

            try
            {
                await Task.WhenAll(tasks.ToArray());
            }
            catch (Exception)
            {
                // Catch everything
            }
        }

        private static void AddToTasks(ScenarioContext scenarioContext, Task task)
        {
            List<Task> tasks;
            if (scenarioContext.TryGetValue(Tasks, out tasks))
            {
                tasks.Add(task);
            }
            else
            {
                tasks = new List<Task> { task };
            }

            scenarioContext.Set(tasks, Tasks);
        }

        private static void SetContinuations(ScenarioContext scenarioContext, Task<bool> task)
        {
            task.ContinueWith(
                t =>
                    {
                        var aggregateException = t.Exception;
                        scenarioContext.Add("AggregateException", aggregateException);
                    },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(
                t => scenarioContext.Set(t.Result, Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static void SetContinuations(ScenarioContext scenarioContext, Task task)
        {
            task.ContinueWith(
                t =>
                    {
                        t.Exception.Handle(e => { return true; });

                        var aggregateException = t.Exception;
                        scenarioContext.Add("AggregateException", aggregateException);
                    },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(
                t => scenarioContext.Set(true, Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private async Task DoSomething(CancellationToken cancellationToken)
        {
            var duration = this.scenarioContext.Get<TimeSpan>(TaskDuration);

            Trace.WriteLine($"Starting to do something for {duration}");
            await Task.Delay(duration, cancellationToken);
            Trace.WriteLine($"Finished doing something for {duration}");

            int actionsCompleted;

            if (!this.scenarioContext.TryGetValue("ActionsCompleted", out actionsCompleted))
            {
                actionsCompleted = 0;
            }

            actionsCompleted++;

            this.scenarioContext.Set(actionsCompleted, "ActionsCompleted");
        }

        private async Task DoSomething(CancellationToken cancellationToken, TaskCompletionSource<object> completion)
        {
            completion?.SetResult(null);

            await this.DoSomething(cancellationToken);
        }

        private async Task<bool> DoSomethingWithResult(CancellationToken cancellationToken)
        {
            var duration = this.scenarioContext.Get<TimeSpan>(TaskDuration);

            Trace.WriteLine($"Starting to do something for {duration}");
            await Task.Delay(duration, cancellationToken);
            Trace.WriteLine($"Finished doing something for {duration}");

            int actionsCompleted;

            if (!this.scenarioContext.TryGetValue("ActionsCompleted", out actionsCompleted))
            {
                actionsCompleted = 0;
            }

            actionsCompleted++;

            this.scenarioContext.Set(actionsCompleted, "ActionsCompleted");

            return true;
        }
    }
}

#pragma warning restore