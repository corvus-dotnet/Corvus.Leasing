// <copyright file="LeaseProviderExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Extensions;
    using Corvus.Leasing.Exceptions;
    using Corvus.Leasing.Retry.Policies;
    using Corvus.Retry;
    using Corvus.Retry.Policies;
    using Corvus.Retry.Strategies;

    /// <summary>
    /// Extension methods on the lease provider for standard leasing scenarios.
    /// </summary>
    public static class LeaseProviderExtensions
    {
        /// <summary>
        /// Apply to a call so that it will return a success/failure code rather than throw the <see cref="LeaseAcquisitionUnsuccessfulException"/>.
        /// </summary>
        /// <param name="leaseTask">The task returned from the lease function.</param>
        /// <returns>A value indicating whether the lease was acquired successfully.</returns>
        /// <remarks>
        /// This will still throw any other exception.
        /// </remarks>
        public static async Task<bool> DoNotThrowIfLeaseNotAcquired(this Task leaseTask)
        {
            try
            {
                await leaseTask.ConfigureAwait(false);
                return true;
            }
            catch (LeaseAcquisitionUnsuccessfulException)
            {
                return false;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.LastOrDefault() is LeaseAcquisitionUnsuccessfulException)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Apply to a call so that it will return a success/failure code rather than throw the <see cref="LeaseAcquisitionUnsuccessfulException"/>.
        /// </summary>
        /// <typeparam name="T">The type of the task result.</typeparam>
        /// <param name="leaseTask">The task returned from the lease function.</param>
        /// <returns>A tuple containing a value indicating whether the lease was acquired successfully, and the return value from the task.</returns>
        /// <remarks>
        /// This will still throw any other exception.
        /// </remarks>
        public static async Task<(bool, T)> DoNotThrowIfLeaseNotAcquired<T>(this Task<T> leaseTask)
        {
            try
            {
                T result = await leaseTask.ConfigureAwait(false);
                return (true, result);
            }
            catch (LeaseAcquisitionUnsuccessfulException)
            {
                return (false, default(T));
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.LastOrDefault() is LeaseAcquisitionUnsuccessfulException)
                {
                    return (false, default(T));
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="retryStrategy">The retry strategy.</param>
        /// <param name="retryPolicy">The retry policy.</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, LeasePolicy leasePolicy, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, string proposedLeaseId = null)
        {
            return ExecuteWithMutexAsync(leaseProvider, action, leasePolicy?.Name, retryStrategy, retryPolicy, leasePolicy?.Duration, leasePolicy?.ActorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="retryStrategy">The retry strategy.</param>
        /// <param name="retryPolicy">The retry policy.</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, LeasePolicy leasePolicy, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, string proposedLeaseId = null)
        {
            return ExecuteWithMutexAsync(leaseProvider, action, leasePolicy?.Name, retryStrategy, retryPolicy, leasePolicy?.Duration, leasePolicy?.ActorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task ExecuteWithMutexTryOnceAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, string leaseName, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    Lease lease = null;
                    try
                    {
                        lease = await AcquireLeaseWithRenewalTask(leaseProvider, leaseName, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            action(cts.Token);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (lease != null)
                        {
                            await lease.ReleaseAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Action<CancellationToken> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    LeaseSet leaseSet = null;
                    try
                    {
                        leaseSet = await AcquireLeaseWithRenewalTask(leaseProvider, leaseNames, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            action(cts.Token);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (leaseSet != null)
                        {
                            await leaseSet.ReleaseAllAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, string leaseName, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    Lease lease = null;
                    try
                    {
                        lease = await AcquireLeaseWithRenewalTask(leaseProvider, leaseName, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            await action(cts.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (lease != null)
                        {
                            await lease.ReleaseAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Executes an action with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task ExecuteWithMutexAsync(this ILeaseProvider leaseProvider, Func<CancellationToken, Task> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    LeaseSet leaseSet = null;
                    try
                    {
                        leaseSet = await AcquireLeaseWithRenewalTask(leaseProvider, leaseNames, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            await action(cts.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (leaseSet != null)
                        {
                            await leaseSet.ReleaseAllAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexTryOnceAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexTryOnceAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that retries until the lease is acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexTryOnceAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        public static Task<T> ExecuteWithMutexTryOnceAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new DoNotRetryOnLeaseAcquisitionUnsuccessfulPolicy();

            return ExecuteWithMutexAsync(leaseProvider, action, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, string leaseName, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    Lease lease = null;
                    try
                    {
                        lease = await AcquireLeaseWithRenewalTask(leaseProvider, leaseName, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            return await action(cts.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (lease != null)
                        {
                            await lease.ReleaseAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Executes a function with mutex semantics that tries once and fails if the lease is not acquired.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="leaseNames">The names of the leases. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically relinquished.</param>
        /// <param name="actorName">The name of the actor executing the action (for diagnostics and logging).</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the operation has executed.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "This object is owned by Retry")]
        public static Task<T> ExecuteWithMutexAsync<T>(this ILeaseProvider leaseProvider, Func<CancellationToken, Task<T>> action, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var cts = new CancellationTokenSource();

            return Retriable.RetryAsync(
                async () =>
                {
                    LeaseSet leaseSet = null;
                    try
                    {
                        leaseSet = await AcquireLeaseWithRenewalTask(leaseProvider, leaseNames, actorName, duration, proposedLeaseId, cts.Token).ConfigureAwait(false);
                        try
                        {
                            return await action(cts.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        if (leaseSet != null)
                        {
                            await leaseSet.ReleaseAllAsync().ConfigureAwait(false);
                        }
                    }
                },
                cts.Token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease that was acquired.</returns>
        public static Task<Lease> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, string leaseName, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseNames">The names of the leases. The name is the shared key across lease acquisitions.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease set that was acquired.</returns>
        public static Task<LeaseSet> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, IEnumerable<string> leaseNames, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease that was acquired.</returns>
        public static Task<Lease> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, string leaseName, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseNames">The names of the leases. The name is the shared key across lease acquisitions.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease set that was acquired.</returns>
        public static Task<LeaseSet> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, IEnumerable<string> leaseNames, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            IRetryStrategy retryStrategy = GetDefaultRetryStrategy(leaseProvider, duration);

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease that was acquired.</returns>
        public static Task<Lease> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, string leaseName, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseName, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseNames">The names of the leases. The name is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease set that was acquired.</returns>
        public static Task<LeaseSet> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            var retryPolicy = new RetryUntilLeaseAcquiredPolicy();

            return AcquireAutorenewingLeaseAsync(leaseProvider, token, leaseNames, retryStrategy, retryPolicy, duration, actorName, proposedLeaseId);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseName">The name of the lease. This is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease that was acquired.</returns>
        public static Task<Lease> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, string leaseName, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            return Retriable.RetryAsync(
                () => AcquireLeaseWithRenewalTask(leaseProvider, leaseName, actorName, duration, proposedLeaseId, token),
                token,
                retryStrategy,
                retryPolicy);
        }

        /// <summary>
        /// Acquire an auto renewing lease from the lease provider.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="leaseNames">The names of the leases. The name is the shared key across lease acquisitions.</param>
        /// <param name="retryStrategy">The retry strategy for the task.</param>
        /// <param name="retryPolicy">The retry policy for the task.</param>
        /// <param name="duration">The duration for which the lease should be acquired before being automatically renewed.</param>
        /// <param name="actorName">The name of the actor acquiring the lease.</param>
        /// <param name="proposedLeaseId">The ID to use for the lease.</param>
        /// <returns>A task whose result is an instance of the lease set that was acquired.</returns>
        public static Task<LeaseSet> AcquireAutorenewingLeaseAsync(this ILeaseProvider leaseProvider, CancellationToken token, IEnumerable<string> leaseNames, IRetryStrategy retryStrategy, IRetryPolicy retryPolicy, TimeSpan? duration = null, string actorName = "", string proposedLeaseId = null)
        {
            return Retriable.RetryAsync(
                () => AcquireLeaseWithRenewalTask(leaseProvider, leaseNames, actorName, duration, proposedLeaseId, token),
                token,
                retryStrategy,
                retryPolicy);
        }

        private static IRetryStrategy GetDefaultRetryStrategy(ILeaseProvider leaseProvider, TimeSpan? duration)
        {
            TimeSpan calculatedDuration = duration ?? leaseProvider.DefaultLeaseDuration;
            return new Linear(TimeSpan.FromSeconds(Math.Max(1, Math.Round(calculatedDuration.TotalSeconds / 10))), int.MaxValue);
        }

        private static async Task<Lease> AcquireLeaseWithRenewalTask(ILeaseProvider leaseProvider, string leaseName, string actorName, TimeSpan? duration, string proposedLeaseId, CancellationToken token)
        {
            TimeSpan calculatedDuration = duration ?? leaseProvider.DefaultLeaseDuration;

            Lease lease = await leaseProvider.AcquireAsync(
                                    new LeasePolicy { ActorName = actorName, Duration = calculatedDuration, Name = leaseName },
                                    proposedLeaseId).ConfigureAwait(false);

            StartRenewalBackgroundTask(token, lease, GetRenewalPeriod(lease.LeasePolicy, leaseProvider));

            return lease;
        }

        private static async Task<LeaseSet> AcquireLeaseWithRenewalTask(ILeaseProvider leaseProvider, IEnumerable<string> leaseNames, string actorName, TimeSpan? duration, string proposedLeaseId, CancellationToken token)
        {
            TimeSpan calculatedDuration = duration ?? leaseProvider.DefaultLeaseDuration;
            IEnumerable<Task<Lease>> tasks = leaseNames.Select(leaseName =>
            {
                return leaseProvider.AcquireAsync(
                                        new LeasePolicy { ActorName = actorName, Duration = calculatedDuration, Name = leaseName },
                                        proposedLeaseId);
            });

            var results = new Lease[0];
            try
            {
                results = await Task.WhenAll(tasks).ConfigureAwait(false);
                var leaseSet = new LeaseSet(results);
                StartRenewalBackgroundTask(token, leaseSet, GetRenewalPeriod(results[0].LeasePolicy, leaseProvider));
                return leaseSet;
            }
            catch (Exception)
            {
                try
                {
                    await results.ForEachFailEndAsync(r => r.ReleaseAsync()).ConfigureAwait(false);
                }
                catch
                {
                    // NOP
                }

                throw;
            }
        }

        private static TimeSpan GetRenewalPeriod(LeasePolicy leasePolicy, ILeaseProvider leaseProvider)
        {
            TimeSpan leaseDuration = leasePolicy.Duration ?? leaseProvider.DefaultLeaseDuration;
            return TimeSpan.FromSeconds(Math.Round(leaseDuration.TotalSeconds / 3));
        }

        private static void StartRenewalBackgroundTask(CancellationToken cancellationToken, Lease lease, TimeSpan renewEvery)
        {
            Task.Factory.StartNew(
                () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.WaitHandle.WaitOne(renewEvery);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // Block this background thread until it is extended
                        try
                        {
                            lease.LeaseProvider.ExtendAsync(lease).Wait();
                        }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
                        catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
                        {
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static void StartRenewalBackgroundTask(CancellationToken cancellationToken, LeaseSet leases, TimeSpan renewEvery)
        {
            // This task is terminated through the cancellation token when the lease is released
            Task.Factory.StartNew(
                () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.WaitHandle.WaitOne(renewEvery);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // Block this background thread until it is extended
                        try
                        {
                            leases.ExtendAllAsync().Wait();
                        }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
                        catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
                        {
                            // Why isn't this blowing up if it fails?
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}
