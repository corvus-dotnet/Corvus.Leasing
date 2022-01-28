// <copyright file="DoNotRetryOnInvalidOperationPolicy.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using Corvus.Retry.Policies;

    /// <summary>
    /// Retry policy that will retry unless a HTTP 409 Conflict status code is detected.
    /// </summary>
    /// <remarks>This indicates the lease is currently locked.</remarks>
    internal class DoNotRetryOnInvalidOperationPolicy : IRetryPolicy
    {
        /// <summary>
        /// Checks to see if the exception thrown is expected and whether a retry attempt should be made.
        /// </summary>
        /// <param name="exception">Exception generated inside the retry scope.</param>
        /// <returns>Whether a retry attempt should be made.</returns>
        public bool CanRetry(Exception exception)
        {
            return exception is not ArgumentOutOfRangeException;
        }
    }
}