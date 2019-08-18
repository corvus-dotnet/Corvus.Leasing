// <copyright file="ILeaseProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Threading.Tasks;
    using Corvus.Leasing.Exceptions;

    /// <summary>
    /// A distributed lease provider.
    /// </summary>
    /// <exception cref="InvalidOperationException">The lease name or lease duration was invalid.</exception>
    /// <exception cref="InitializationFailureException">The lease provider failed during initialization. The inner exception provides the failure reason.</exception>
    /// <exception cref="LeaseAcquisitionUnsuccessfulException">The lease provider was unable to acquire the lease.</exception>
    /// <remarks>
    /// <para>
    /// This provides an abstraction for various different leasing models. The basic interface
    /// supports and Acquire (for a duration), Extend(renewing a lease before it expires for another lease period)
    /// and Release (relinquish the lease) cycle.
    /// </para>
    /// <para>
    /// Typically, you use the lease provider through one of the extension methods in <see cref="LeaseProviderExtensions"/>.
    /// These give you a mutex model, with try-once or try-until-acquire-or-timeout semantics, and auto-renewal behaviour.
    /// </para>
    /// <para>
    /// At its simplest, you can execute an action with guaranteed distributed mutex semantics.
    ///
    /// <code>
    /// leaseProvider.ExecuteWithMutexAsync(() => { /* my action */ }, "myuniqueleasename");
    /// </code>
    ///
    ///  This will block until the lease is acquired, and then execute the action, holding the lease until it is done.
    ///  The operation has a default timeout. If the lease cannot be acquired in time, it will throw a <see cref="LeaseAcquisitionUnsuccessfulException"/>.
    /// </para>
    ///
    /// <para>
    /// You can also use the "try once" semantics.
    ///
    /// <code>
    /// leaseProvider.ExecuteWithMutexTryOnceAsync(() => { /* my action */ }, "myuniqueleasename");
    /// </code>
    ///
    ///  This try to acquire the lease, and then execute the action, holding the lease until it is done.
    ///  If it cannot acquire the lease, it will immediately throw a <see cref="LeaseAcquisitionUnsuccessfulException"/> without retrying.
    /// </para>
    /// <para>
    /// If you do not want to see the <see cref="LeaseAcquisitionUnsuccessfulException"/>, you can suppress it and turn it into a boolean return value
    /// with an extension method.
    /// <code>
    /// leaseProvider.ExecuteWithMutexAsync(() => { /* my action */ }, "myuniqueleasename").DoNotThrowIfLeaseNotAcquired();
    /// </code>
    /// </para>
    /// <para>
    /// There are various overloads that allow you to control the duration of the lease, and the strategies and policy for retrying in the event of failure.
    /// </para>
    /// <para>
    /// Most providers also have constraints on the lease name and lease duration - e.g. special characters, minimum duration. If you use an invalid parameter,
    /// it will raise an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// It is also possible that the leasing operation will fail during initialization. In that case you will see an <see cref="InitializationFailureException"/>.
    /// </para>
    /// </remarks>
    public interface ILeaseProvider
    {
        /// <summary>
        /// Gets the default lease duration.
        /// </summary>
        TimeSpan DefaultLeaseDuration { get; }

        /// <summary>
        /// Acquires the lease.
        /// </summary>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="proposedLeaseId">A proposed ID for the lease.</param>
        /// <returns>A task which completes when the lease is acquired.</returns>
        Task<Lease> AcquireAsync(LeasePolicy leasePolicy, string proposedLeaseId = null);

        /// <summary>
        /// Extends the lease for a period according to the lease's policy.
        /// </summary>
        /// <param name="lease">The lease to extend.</param>
        /// <returns>A task which completes when the lease is extended.</returns>
        Task ExtendAsync(Lease lease);

        /// <summary>
        /// Releases the specified lease.
        /// </summary>
        /// <param name="lease">The lease to release.</param>
        /// <returns>A task which completes when the lease is released.</returns>
        Task ReleaseAsync(Lease lease);

        /// <summary>
        /// Tokenizes a <see cref="Lease"/> to a URL-friendly string.
        /// </summary>
        /// <param name="lease">The lease to tokenize.</param>
        /// <returns>A string providing a portable representation of the lease.</returns>
        string ToLeaseToken(Lease lease);

        /// <summary>
        /// Creates a <see cref="Lease"/> from a portable lease token.
        /// </summary>
        /// <param name="leaseToken">The tokenized version of the lease.</param>
        /// <returns>A <see cref="Lease"/> corresponding to the provided lease token.</returns>
        Lease FromLeaseToken(string leaseToken);
    }
}