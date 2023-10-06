// <copyright file="CosmosDbLeaseProviderOptions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using Corvus.Leasing.Internal;

    /// <summary>
    /// Options for <see cref="CosmosDbLeaseProvider"/>.
    /// </summary>
    public class CosmosDbLeaseProviderOptions
    {
        /// <summary>
        /// Gets a value for the root partition key, or null if no root partition key is in use.
        /// </summary>
        public string? RootPartitionKeyValue { get; init; }

        /// <summary>
        /// Gets a value for the connection string to connect to the CosmosDb account.
        /// </summary>
        public string? CosmosDbConnectionString { get; init; }
    }
}