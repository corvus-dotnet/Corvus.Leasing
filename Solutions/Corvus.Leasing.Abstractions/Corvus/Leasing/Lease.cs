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
            this.LeaseProvider = leaseProvider ?? throw new ArgumentNullException(nameof(leaseProvider));
            this.LeasePolicy = leasePolicy ?? throw new ArgumentNullException(nameof(leasePolicy));
            this.Id = id;
        }

        /// <summary>
        /// Gets the lease id.
        /// </summary>
        public string Id { get; }

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
        /// Gets or sets a value indicating whether the lease has been explicitly released.
        /// </summary>
        /// <remarks>
        /// This flag is only set if the lease is explicitly released; if it expires, this flag will still be true. If you need
        /// to know whether the lease is still held, check the <see cref="HasLease"/> property instead.
        /// </remarks>
        public bool Released { get; set; }

        /// <summary>
        /// Gets a value indicating whether the lease has been acquired.
        /// </summary>
        public bool HasLease
        {
            get { return !this.LeaseHasExpired() && !this.Released; }
        }

        /// <summary>
        /// Release the lease.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes once the lease is released.</returns>
        public async Task ReleaseAsync()
        {
            if (this.HasLease)
            {
                await this.LeaseProvider.ReleaseAsync(this);
                this.Released = true;
            }
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
