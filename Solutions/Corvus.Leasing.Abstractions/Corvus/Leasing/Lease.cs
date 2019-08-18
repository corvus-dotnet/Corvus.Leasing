// <copyright file="Lease.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A base class for platform-specific lease implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You acquire a lease by calling one of the extension methods on <see cref="ILeaseProvider"/>.
    /// </para>
    /// </remarks>
    public abstract class Lease
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Lease"/> class.
        /// </summary>
        /// <param name="leaseProvider">The platform-specific lease provider.</param>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="id">The unique lease ID.</param>
        protected Lease(ILeaseProvider leaseProvider, LeasePolicy leasePolicy, string id)
        {
            this.LeaseProvider = leaseProvider;
            this.LeasePolicy = leasePolicy;
            this.Id = id;
        }

        /// <summary>
        /// Gets or sets the lease id.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// Gets the platform-specific lease provider for this lease.
        /// </summary>
        public ILeaseProvider LeaseProvider { get; }

        /// <summary>
        /// Gets the lease policy for this lease.
        /// </summary>
        public LeasePolicy LeasePolicy { get; }

        /// <summary>
        /// Gets the time at which this lease was last acquired.
        /// </summary>
        public abstract DateTimeOffset? LastAcquired { get; }

        /// <summary>
        /// Gets the time at which this lease expires.
        /// </summary>
        /// <remarks>
        /// This will be null.
        /// </remarks>
        public DateTimeOffset? Expires
        {
            get
            {
                if (!this.LastAcquired.HasValue)
                {
                    return null;
                }

                return !this.LeasePolicy.Duration.HasValue ? DateTimeOffset.MaxValue : this.LastAcquired.Value.Add(this.LeasePolicy.Duration.Value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the lease has been acquired.
        /// </summary>
        public bool HasLease
        {
            get { return !this.LeaseHasExpired() && !string.IsNullOrEmpty(this.Id); }
        }

        /// <summary>
        /// Release the lease.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes once the lease is released.</returns>
        public Task ReleaseAsync()
        {
            if (this.HasLease)
            {
                return this.LeaseProvider.ReleaseAsync(this);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Extend the lease.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes once the lease is extended.</returns>
        public Task ExtendAsync()
        {
            return this.LeaseProvider.ExtendAsync(this);
        }

        private bool LeaseHasExpired()
        {
            if (!this.LastAcquired.HasValue)
            {
                return false;
            }

            TimeSpan remaining = DateTimeOffset.UtcNow - this.LastAcquired.Value;

            if (this.LeasePolicy.Duration.HasValue)
            {
                if ((this.LeasePolicy.Duration.Value - remaining).Seconds > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
