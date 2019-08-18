// <copyright file="LeaseSet.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A set of leases.
    /// </summary>
    public class LeaseSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseSet"/> class.
        /// </summary>
        /// <param name="leases">The leases in the set.</param>
        public LeaseSet(IEnumerable<Lease> leases)
        {
            this.Leases = leases;
        }

        /// <summary>
        /// Gets the leases in the set.
        /// </summary>
        public IEnumerable<Lease> Leases { get; }

        /// <summary>
        /// Release all the leases in the set.
        /// </summary>
        /// <returns>A task which completes once the leases have been released.</returns>
        public Task ReleaseAllAsync()
        {
            IEnumerable<Task> tasks = this.Leases.Select(l => l.ReleaseAsync());
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Extend all the leases in the set.
        /// </summary>
        /// <returns>A task which completes once the leases have been extended.</returns>
        public Task ExtendAllAsync()
        {
            IEnumerable<Task> tasks = this.Leases.Select(l => l.ExtendAsync());
            return Task.WhenAll(tasks);
        }
    }
}
