// <copyright file="AzureLeaseProviderOptions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using Corvus.Leasing.Internal;

    /// <summary>
    /// Options for <see cref="AzureLeaseProvider"/>.
    /// </summary>
    public class AzureLeaseProviderOptions
    {
        /// <summary>
        /// Gets or sets the storage account connection string.
        /// </summary>
        public string StorageAccountConnectionString { get; set; }
    }
}
