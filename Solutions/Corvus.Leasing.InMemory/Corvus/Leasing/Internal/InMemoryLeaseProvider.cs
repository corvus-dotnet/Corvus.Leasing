// <copyright file="InMemoryLeaseProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Corvus.Leasing.Exceptions;

    /// <summary>
    /// An in-memory implementation of the <see cref="ILeaseProvider"/> interface. Should
    /// only be used for testing purposes.
    /// </summary>
    public class InMemoryLeaseProvider : ILeaseProvider
    {
        private static readonly ConcurrentDictionary<string, Lease> Leases = new ConcurrentDictionary<string, Lease>();

        /// <inheritdoc/>
        public TimeSpan DefaultLeaseDuration => TimeSpan.FromSeconds(59);

        /// <inheritdoc/>
        public Task<Lease> AcquireAsync(LeasePolicy leasePolicy, string proposedLeaseId = null)
        {
            if (leasePolicy is null)
            {
                throw new ArgumentNullException(nameof(leasePolicy));
            }

            proposedLeaseId ??= Guid.NewGuid().ToString();

            Leases.TryGetValue(proposedLeaseId, out Lease lease);

            if (lease?.Expires.HasValue == true && lease.Expires > DateTimeOffset.UtcNow)
            {
                throw new LeaseAcquisitionUnsuccessfulException(leasePolicy, null);
            }

            lease = new InMemoryLease(this, leasePolicy, proposedLeaseId);
            Leases.AddOrUpdate(proposedLeaseId, lease, (_, __) => lease);

            return Task.FromResult(lease);
        }

        /// <inheritdoc/>
        public Task ExtendAsync(Lease lease)
        {
            if (lease is null)
            {
                throw new ArgumentNullException(nameof(lease));
            }

            (lease as InMemoryLease)?.SetLastAcquired();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Lease FromLeaseToken(string leaseToken)
        {
            if (leaseToken is null)
            {
                throw new ArgumentNullException(nameof(leaseToken));
            }

            return InMemoryLease.FromToken(this, leaseToken);
        }

        /// <inheritdoc/>
        public Task ReleaseAsync(Lease lease)
        {
            if (lease is null)
            {
                throw new ArgumentNullException(nameof(lease));
            }

            Leases.TryRemove(lease.Id, out Lease _);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public string ToLeaseToken(Lease lease)
        {
            if (lease is null)
            {
                throw new ArgumentNullException(nameof(lease));
            }

            if (lease is InMemoryLease iml)
            {
                return iml.GetToken();
            }
            else
            {
                throw new TokenizationException();
            }
        }
    }
}