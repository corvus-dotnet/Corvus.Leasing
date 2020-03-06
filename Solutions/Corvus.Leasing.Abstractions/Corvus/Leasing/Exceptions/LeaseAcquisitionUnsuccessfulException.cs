// <copyright file="LeaseAcquisitionUnsuccessfulException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Exceptions
{
    using System;

    /// <summary>
    /// Exception that represents that a Lease could not be acquired successfully.
    /// </summary>
    public class LeaseAcquisitionUnsuccessfulException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="leasePolicy">The lease policy used during the attempt to acquire the lease.</param>
        /// <param name="innerException">Exception that cause the lease to be unsuccessfully acquired.</param>
        public LeaseAcquisitionUnsuccessfulException(LeasePolicy leasePolicy, Exception innerException)
            : base(string.Empty, innerException)
        {
            this.LeasePolicy = leasePolicy ?? throw new ArgumentNullException(nameof(leasePolicy));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        public LeaseAcquisitionUnsuccessfulException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public LeaseAcquisitionUnsuccessfulException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public LeaseAcquisitionUnsuccessfulException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaseAcquisitionUnsuccessfulException"/> class.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected LeaseAcquisitionUnsuccessfulException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the lease policy used during the attempt to acquire the lease.
        /// </summary>
        public LeasePolicy? LeasePolicy { get; }
    }
}