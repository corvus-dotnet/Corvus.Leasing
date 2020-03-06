// <copyright file="LeasePolicy.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;

    /// <summary>
    /// Defines various options used in the creation and acquisition of a lease.
    /// </summary>
    public class LeasePolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeasePolicy"/> class.
        /// </summary>
        /// <param name="name">The <see cref="Name"/>. If null, a generated value will be used.</param>
        public LeasePolicy(string? name)
        {
            this.Name = name ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets or sets the name for the actor requesting the lease.
        /// </summary>
        public string? ActorName { get; set; }

        /// <summary>
        /// Gets or sets the duration of the lease.
        /// </summary>
        /// <remarks>Different lease implementation will have different validity rules about the lease duration.</remarks>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the name of the lease.
        /// </summary>
        /// <remarks>Different lease implementation will have different validity rules about the lease name.</remarks>
        public string Name { get; set; }
    }
}