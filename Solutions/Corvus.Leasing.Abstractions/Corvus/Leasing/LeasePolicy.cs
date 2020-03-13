// <copyright file="LeasePolicy.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Defines various options used in the creation and acquisition of a lease.
    /// </summary>
    public class LeasePolicy
    {
        private string? name;

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
        [AllowNull]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.name))
                {
                    this.name = Guid.NewGuid().ToString();
                }

                return this.name!;
            }

            set
            {
                this.name = string.IsNullOrEmpty(value) ? Guid.NewGuid().ToString() : value;
            }
        }
    }
}