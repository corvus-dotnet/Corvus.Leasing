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
        private string name;

        /// <summary>
        /// Gets or sets the name for the actor requesting the lease.
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// Gets or sets the duration of the lease.
        /// </summary>
        /// <remarks>Different lease implementation will have different validity rules about the lease duration.</remarks>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the name of the lease.
        /// </summary>
        /// <remarks>Different lease implementation will have different validity rules about the lease name.</remarks>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = string.IsNullOrEmpty(value) ? Guid.NewGuid().ToString() : value;
            }
        }
    }
}