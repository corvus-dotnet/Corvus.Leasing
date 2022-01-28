// <copyright file="DoNotRetryOnInitializationFailurePolicy.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using Corvus.Retry.Policies;

    /// <summary>
    /// Retry policy that will retry unless an InitializationFailureException occurs.
    /// </summary>
    /// <remarks>This indicates the lease is currently locked.</remarks>
    internal class DoNotRetryOnInitializationFailurePolicy : IRetryPolicy
    {
        /// <inheritdoc/>
        public bool CanRetry(Exception exception)
        {
            return exception is not InitializationFailureException;
        }
    }
}