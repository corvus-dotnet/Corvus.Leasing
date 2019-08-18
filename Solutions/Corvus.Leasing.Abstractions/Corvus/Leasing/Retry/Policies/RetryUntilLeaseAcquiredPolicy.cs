// <copyright file="RetryUntilLeaseAcquiredPolicy.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Retry.Policies
{
    using System;
    using Corvus.Leasing.Exceptions;
    using Corvus.Retry.Policies;

    /// <summary>
    /// Retry Policy that will retry until the lease has been successfully acquired.
    /// </summary>
    public class RetryUntilLeaseAcquiredPolicy : IRetryPolicy
    {
        /// <summary>
        /// Checks to see if the exception thrown is expected and whether a retry attempt should be made.
        /// </summary>
        /// <param name="exception">Exception generated inside the retry scope.</param>
        /// <returns>Whether a retry attempt should be made.</returns>
        public bool CanRetry(Exception exception)
        {
            return exception is LeaseAcquisitionUnsuccessfulException;
        }
    }
}