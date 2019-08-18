// <copyright file="LeasableSteps.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

#pragma warning disable

namespace Endjin.Leasing.Azure.Specs.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Endjin.Composition;
    using Endjin.Leasing.Azure.Specs.Helpers;
    using Endjin.Leasing.Retry.Policies;
    using Endjin.Retry.Policies;
    using Endjin.Retry.Strategies;
    using Endjin.SpecFlow.Bindings;

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

        public LeasableSteps(FeatureContext featureContext)
        {
            this.featureContext = featureContext;
        }

        [Given(@"actor A executes the task")]
        public void GivenActorAExecutesTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = ScenarioContext.Current.Get<string>(LeaseName);

            var task = provider.ExecuteWithMutexAsync(this.DoSomething, leaseName, actorName: "Actor A");
            SetContinuations(task);
            AddToTasks(task);
        }

        [Given(@"actor A executes the task with a try once mutex")]
        public void GivenActorAExecutesTheTaskWithATryOnceMutex()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = ScenarioContext.Current.Get<string>(LeaseName);

            var task = provider.ExecuteWithMutexTryOnceAsync(this.DoSomething, leaseName, actorName: "Actor A");
            SetContinuations(task);
            AddToTasks(task);
        }

        [Given(@"actor A executes the task with options")]
        public void GivenActorAExecutesTheTaskWithOptions()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();

            string leaseName;
            TimeSpan leaseDuration;
            IRetryPolicy retryPolicy;
            IRetryStrategy retryStrategy;

            ScenarioContext.Current.TryGetValue(LeaseName, out leaseName);
            ScenarioContext.Current.TryGetValue(LeaseDuration, out leaseDuration);
            ScenarioContext.Current.TryGetValue(RetryPolicy, out retryPolicy);
            ScenarioContext.Current.TryGetValue(RetryStrategy, out retryStrategy);

            var policy = new LeasePolicy { ActorName = "Actor A", Duration = leaseDuration, Name = leaseName };

            var task = provider.ExecuteWithMutexAsync(this.DoSomething, policy, retryStrategy, retryPolicy);
            SetContinuations(task);
            AddToTasks(task);
        }

        [Given(@"actor B is currently running the task")]
        public async Task GivenActorBIsCurrentlyRunningTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = ScenarioContext.Current.Get<string>(LeaseName);

            var started = new TaskCompletionSource<object>();
            var task = provider.ExecuteWithMutexAsync(ct => this.DoSomething(ct, started), leaseName, actorName: "Actor B");

            AddToTasks(task);

            // The task only gets completed inside the mutex action, so we know the lease has been obtained at that point.
            // We also await the task returned by ExecuteWithMutexAsync, so that if it fails with an error before running
            // our action, we don't just sit here forever.
            await Task.WhenAny(started.Task, task);
        }

        [Given(@"the lease duration is (.*) seconds")]
        public void GivenTheLeaseDurationIsSeconds(int leaseDurationInSeconds)
        {
            ScenarioContext.Current.Set(TimeSpan.FromSeconds(leaseDurationInSeconds), LeaseDuration);
        }

        [Given(@"the lease name is ""(.*)""")]
        public void GivenTheLeaseNameIs(string leaseName)
        {
            ScenarioContext.Current.Set($"{leaseName}_{Guid.NewGuid()}", LeaseName);
        }

        [Given(@"the lease names are")]
        public void GivenTheLeaseNamesAre(Table table)
        {
            var leaseNames = table.CreateSet<LeaseName>();
            ScenarioContext.Current.Set(leaseNames, LeaseNames);
        }

        [Given(@"the long running task takes (.*) seconds to complete")]
        public void GivenTheLongRunningTaskTakesSecondsToComplete(double durationInSeconds)
        {
            var duration = TimeSpan.FromSeconds(durationInSeconds);

            ScenarioContext.Current.Set(duration, TaskDuration);
        }

        [Given(@"we use a do not retry on lease acquisition unsuccessful policy")]
        public void GivenWeUseADoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy()
        {
            var policy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();
            ScenarioContext.Current.Set(policy, RetryPolicy);
        }

        [Given(@"we use a do not retry policy")]
        public void GivenWeUseADoNotRetryPolicy()
        {
            var policy = new DoNotRetryPolicy();
            ScenarioContext.Current.Set(policy, RetryPolicy);
        }

        [Given(@"we use a linear retry strategy with periodicity of (.*) seconds and (.*) max retries")]
        public void GivenWeUseALinearRetryStrategy(int periodicityInSeconds, int maxRetries)
        {
            var strategy = new Linear(TimeSpan.FromSeconds(periodicityInSeconds), maxRetries);
            ScenarioContext.Current.Set(strategy, RetryStrategy);
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

            if (!ScenarioContext.Current.TryGetValue("ActionsCompleted", out actionsCompleted))
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
            var result = ScenarioContext.Current.Get<bool>(Result);
            Assert.True(result);
        }

        [Then(@"it should return unsuccessfully")]
        public void ThenItShouldReturnUnsuccessfully()
        {
            var result = ScenarioContext.Current.Get<bool>(Result);
            Assert.False(result);
        }

        [Then(@"the task result should be correct")]
        public void ThenTheTaskResultShouldBeCorrect()
        {
            var result = ScenarioContext.Current.Get<bool>(TaskResult);
            Assert.True(result);
        }

        [When(@"I execute the task")]
        public void WhenIExecuteTheTask()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = ScenarioContext.Current.Get<string>(LeaseName);

            try
            {
                provider.ExecuteWithMutexAsync(this.DoSomething, leaseName, actorName: "Actor A").Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(true, Result);
        }

        [When(@"I execute the task using all the leases")]
        public void WhenIExecuteTheTaskUsingAllTheLeases()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseNames = ScenarioContext.Current.Get<IEnumerable<LeaseName>>(LeaseNames).Select(l => l.Name);

            try
            {
                provider.ExecuteWithMutexAsync(this.DoSomething, leaseNames, actorName: "Actor A").Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(true, Result);
        }

        [When(@"I execute the task with a result")]
        public void WhenIExecuteTheTaskWithAResult()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var leaseName = ScenarioContext.Current.Get<string>(LeaseName);

            var result = false;

            try
            {
                result = provider.ExecuteWithMutexAsync(this.DoSomethingWithResult, leaseName, actorName: "Actor A")
                    .Result;
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(true, Result);
            ScenarioContext.Current.Set(result, TaskResult);
        }

        [When(@"I execute the task with options")]
        public void WhenIExecuteTheTaskWithOptions()
        {
            var provider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();

            string leaseName;
            TimeSpan leaseDuration;
            IRetryPolicy retryPolicy;
            IRetryStrategy retryStrategy;
            LeasePolicy policy = null;

            ScenarioContext.Current.TryGetValue(LeaseName, out leaseName);
            ScenarioContext.Current.TryGetValue(LeaseDuration, out leaseDuration);
            ScenarioContext.Current.TryGetValue(RetryPolicy, out retryPolicy);
            ScenarioContext.Current.TryGetValue(RetryStrategy, out retryStrategy);

            if (leaseName != null || leaseDuration != default(TimeSpan))
            {
                policy = new LeasePolicy { ActorName = "Actor A", Duration = leaseDuration, Name = leaseName };
            }

            try
            {
                provider.ExecuteWithMutexAsync(this.DoSomething, policy, retryStrategy, retryPolicy).Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(true, Result);
        }

        [When(@"the tasks have completed")]
        public void WhenTheTasksHaveCompleted()
        {
            var tasks = ScenarioContext.Current.Get<List<Task>>(Tasks);

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception)
            {
                // Catch everything
            }
        }

        private static void AddToTasks(Task task)
        {
            List<Task> tasks;
            if (ScenarioContext.Current.TryGetValue(Tasks, out tasks))
            {
                tasks.Add(task);
            }
            else
            {
                tasks = new List<Task> { task };
            }

            ScenarioContext.Current.Set(tasks, Tasks);
        }

        private static void SetContinuations(Task<bool> task)
        {
            task.ContinueWith(
                t =>
                    {
                        var aggregateException = t.Exception;
                        ScenarioContext.Current.Add("AggregateException", aggregateException);
                    },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(
                t => ScenarioContext.Current.Set(t.Result, Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static void SetContinuations(Task task)
        {
            task.ContinueWith(
                t =>
                    {
                        t.Exception.Handle(e => { return true; });

                        var aggregateException = t.Exception;
                        ScenarioContext.Current.Add("AggregateException", aggregateException);
                    },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(
                t => ScenarioContext.Current.Set(true, Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private async Task DoSomething(CancellationToken cancellationToken)
        {
            var duration = ScenarioContext.Current.Get<TimeSpan>(TaskDuration);

            Trace.WriteLine($"Starting to do something for {duration}");
            await Task.Delay(duration, cancellationToken);
            Trace.WriteLine($"Finished doing something for {duration}");

            int actionsCompleted;

            if (!ScenarioContext.Current.TryGetValue("ActionsCompleted", out actionsCompleted))
            {
                actionsCompleted = 0;
            }

            actionsCompleted++;

            ScenarioContext.Current.Set(actionsCompleted, "ActionsCompleted");
        }

        private async Task DoSomething(CancellationToken cancellationToken, TaskCompletionSource<object> completion)
        {
            completion?.SetResult(null);

            await this.DoSomething(cancellationToken);
        }

        private async Task<bool> DoSomethingWithResult(CancellationToken cancellationToken)
        {
            var duration = ScenarioContext.Current.Get<TimeSpan>(TaskDuration);

            Trace.WriteLine($"Starting to do something for {duration}");
            await Task.Delay(duration, cancellationToken);
            Trace.WriteLine($"Finished doing something for {duration}");

            int actionsCompleted;

            if (!ScenarioContext.Current.TryGetValue("ActionsCompleted", out actionsCompleted))
            {
                actionsCompleted = 0;
            }

            actionsCompleted++;

            ScenarioContext.Current.Set(actionsCompleted, "ActionsCompleted");

            return true;
        }
    }
}

#pragma warning restore