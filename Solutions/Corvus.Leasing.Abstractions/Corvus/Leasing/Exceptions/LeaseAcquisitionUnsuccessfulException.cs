// <copyright file="LeaseAcquisitionUnsuccessfulException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Exceptions
{
    using System;

    /// <summary>
    /// Exception that represents that a Lease could not be acquired successfully.
    /// </summary>
#pragma warning disable RCS1194 // Implement exception constructors.
    public class LeaseAcquisitionUnsuccessfulException : Exception
#pragma warning restore RCS1194 // Implement exception constructors.
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="leasePolicy">The lease policy used during the attempt to acquire the lease.</param>
        /// <param name="innerException">Exception that cause the lease to be unsuccessfully acquired.</param>
        public LeaseAcquisitionUnsuccessfulException(LeasePolicy leasePolicy, Exception? innerException)
            : base(string.Empty, innerException)
        {
            this.LeasePolicy = leasePolicy ?? throw new ArgumentNullException(nameof(leasePolicy));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        protected LeaseAcquisitionUnsuccessfulException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the lease policy used during the attempt to acquire the lease.
        /// </summary>
        public LeasePolicy LeasePolicy { get; }
    }
}