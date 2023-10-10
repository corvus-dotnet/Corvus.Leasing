// <copyright file="CosmosDbLeaseProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Corvus.Leasing.Exceptions;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The platform specific implementation used for lease operations.
    /// </summary>
    public class CosmosDbLeaseProvider : ILeaseProvider
    {
        /// <summary>
        /// Gets the internal name of the root partition key path, if
        /// a root partition is in use.
        /// </summary>
        public const string RootPartitionKeyPath = "/rpk";

        private readonly Container container;
        private readonly CosmosDbLeaseProviderOptions options;
        private readonly ILogger<ILeaseProvider> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbLeaseProvider"/> class.
        /// </summary>
        /// <param name="container">The container in which leases are to be created.</param>
        /// <param name="options">The lease provider options.</param>
        /// <param name="logger">The logger.</param>
        public CosmosDbLeaseProvider(Container container, CosmosDbLeaseProviderOptions options, ILogger<ILeaseProvider> logger)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.options = options;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public TimeSpan DefaultLeaseDuration => InternalDefaultLeaseDuration;

        /// <summary>
        /// Gets the default lease duration for the specific platform implementation of the lease.
        /// </summary>
        /// <remarks>This is currently 59 seconds.</remarks>
        private static TimeSpan InternalDefaultLeaseDuration => TimeSpan.FromSeconds(59);

        /// <summary>
        /// Gets the required container properties based on the desired container ID.
        /// </summary>
        /// <param name="containerId">The container ID.</param>
        /// <param name="useRootPartitionKey">A boolean which indicates whether to use a root partition key.</param>
        /// <returns>The container properties for a lease container.</returns>
        public static ContainerProperties GetContainerProperties(string containerId, bool useRootPartitionKey)
        {
            if (useRootPartitionKey)
            {
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
                return new ContainerProperties
                {
                    Id = containerId,
                    PartitionKeyPaths = ["/rpk", "/id"],
                    DefaultTimeToLive = -1, // Explicit default time to live
                };
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
            }
            else
            {
                return new ContainerProperties
                {
                    Id = containerId,
                    PartitionKeyPath = "/id",
                    DefaultTimeToLive = -1, // Explicit default time to live
                };
            }
        }

        /// <inheritdoc/>
        public async Task<Lease> AcquireAsync(LeasePolicy leasePolicy, string? proposedLeaseId = null)
        {
            ArgumentNullException.ThrowIfNull(leasePolicy);

            this.logger.LogDebug($"Acquiring lease for '{leasePolicy.ActorName}' with name '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and proposed id '{proposedLeaseId}'");
            string leaseObjectId = GetLeaseObjectId(leasePolicy);
            string leaseId = proposedLeaseId ?? Guid.NewGuid().ToString();
            PartitionKey pk = this.BuildPartitionKey(leaseObjectId);

            // First, just try to read the lease.
            ResponseMessage response = await this.container.ReadItemStreamAsync(leaseObjectId, pk).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                string leaseEntity;

                if (this.options.RootPartitionKeyValue is string rpk)
                {
                    leaseEntity =
                        $$"""
                        {
                            "rpk": "{{rpk}}",
                            "id": "{{leaseObjectId}}",
                            "leaseId": "{{leaseId}}",
                            "ttl": {{GetLeaseDuration(leasePolicy)}},
                            "acquisitionToken": 0
                        }
                        """;
                }
                else
                {
                    leaseEntity =
                        $$"""
                        {
                            "id": "{{leaseObjectId}}",
                            "leaseId": "{{leaseId}}",
                            "ttl": {{GetLeaseDuration(leasePolicy)}},
                            "acquisitionToken": 0
                        }
                        """;
                }

                // The lease does not exist, so we can create it, and assign it to ourselves
                using Stream stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                writer.Write(leaseEntity);
                writer.Flush();
                stream.Position = 0;
                response = await this.container.CreateItemStreamAsync(stream, pk).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // In this case, someone sneakily got in there before us!
                    this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}'. The lease was held by another party. The lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and lease id '{leaseId}'");
                    throw new LeaseAcquisitionUnsuccessfulException(leasePolicy, null);
                }
                else if (!response.IsSuccessStatusCode)
                {
                    // In this case, something blew up horribly
                    this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}'.\r\n{response.ErrorMessage}\r\nThe lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and lease id '{leaseId}'");
                    throw new InvalidOperationException();
                }
            }

            // Now, we parse the response to get the current Actor and ETag
            if (!OwnsLease(response.Content, leaseId))
            {
                // In this case, someone sneakily got in there before us!
                this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}'. The lease was held by another party. The lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and lease id '{leaseId}'");
                throw new LeaseAcquisitionUnsuccessfulException(leasePolicy, null);
            }

            var lease = new CosmosDbLease(this, leasePolicy, leaseId);
            lease.SetLastAcquired(DateTimeOffset.Now);
            this.logger.LogDebug($"Acquired lease for '{leasePolicy.ActorName}' with name '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and actual id '{leaseId}'");
            return lease;

            static int GetLeaseDuration(LeasePolicy leasePolicy)
            {
                return (int)Math.Round((leasePolicy.Duration ?? InternalDefaultLeaseDuration).TotalSeconds);
            }
        }

        /// <inheritdoc/>
        public async Task ExtendAsync(Lease lease)
        {
            if (lease is not CosmosDbLease al)
            {
                throw new ArgumentException($"Only Leases of type {nameof(CosmosDbLease)} can be extended by the {nameof(CosmosDbLeaseProvider)}.");
            }

            this.logger.LogDebug($"Extending lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");

            // First, just try to read the lease.
            string leaseObjectId = GetLeaseObjectId(lease.LeasePolicy);
            PartitionKey pk = this.BuildPartitionKey(leaseObjectId);

            ResponseMessage response = await this.container.ReadItemStreamAsync(leaseObjectId, pk).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // No lease found, so let's try to re-acquire the lease
                Lease updatedLease = await this.AcquireAsync(lease.LeasePolicy, lease.Id).ConfigureAwait(false);
                al.SetLastAcquired(updatedLease.LastAcquired);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError($"Failed to extend the lease for '{lease.LeasePolicy.ActorName}'. The lease name was '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and lease id '{lease.Id}'");
                throw new InvalidOperationException();
            }

            if (!OwnsLease(response.Content, al.Id, out string? eTag))
            {
                this.logger.LogError($"Failed to extend the lease for '{lease.LeasePolicy.ActorName}'. The lease was held by another party. The lease name was '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and lease id '{lease.Id}'");
                throw new LeaseAcquisitionUnsuccessfulException(lease.LeasePolicy, null);
            }

            // Increment the acquisition token so the doc changes.
            var patchOperations = new List<PatchOperation>
                {
                    PatchOperation.Increment("/acquisitionToken", 1),
                };

            // We own it, so now it is time to update it (if it has not been modified while we are using it).
            response = await this.container.PatchItemStreamAsync(leaseObjectId, pk, patchOperations, new PatchItemRequestOptions { IfMatchEtag = eTag }).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // It disappeared while we were building the patch, so we'll just re-acquire it.
                Lease updatedLease = await this.AcquireAsync(lease.LeasePolicy, lease.Id).ConfigureAwait(false);
                al.SetLastAcquired(updatedLease.LastAcquired);
                return;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                this.logger.LogError($"Failed to extend the lease for '{lease.LeasePolicy.ActorName}'. The lease was held by another party. The lease name was '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and lease id '{lease.Id}'");
                throw new LeaseAcquisitionUnsuccessfulException(lease.LeasePolicy, null);
            }

            al.SetLastAcquired(DateTimeOffset.Now);
            this.logger.LogDebug($"Extended lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
        }

        /// <inheritdoc/>
        public Lease FromLeaseToken(string leaseToken)
        {
            ArgumentNullException.ThrowIfNull(leaseToken);

            return CosmosDbLease.FromToken(this, leaseToken);
        }

        /// <inheritdoc/>
        public async Task ReleaseAsync(Lease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);

            if (lease is not CosmosDbLease al)
            {
                throw new ArgumentException($"Only Leases of type {nameof(CosmosDbLease)} can be released by the {nameof(CosmosDbLeaseProvider)}.");
            }

            this.logger.LogDebug($"Releasing lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");

            string leaseObjectId = GetLeaseObjectId(lease.LeasePolicy);
            PartitionKey pk = this.BuildPartitionKey(leaseObjectId);

            ResponseMessage response = await this.container.ReadItemStreamAsync(leaseObjectId, pk).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                al.SetLastAcquired(null);
                this.logger.LogDebug($"Lease already released lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError($"Failed to release lease for '{lease.LeasePolicy.ActorName}'. The lease was held by another party. The lease name was '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and lease id '{lease.Id}'");
                throw new InvalidOperationException();
            }

            if (!OwnsLease(response.Content, lease.Id, out string? eTag))
            {
                al.SetLastAcquired(null);
                this.logger.LogDebug($"Lease already released lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
                return;
            }

            // We own it, so now it is time to delete it (if it still exists).
            response = await this.container.DeleteItemStreamAsync(leaseObjectId, pk, new ItemRequestOptions { IfMatchEtag = eTag }).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                al.SetLastAcquired(null);
                this.logger.LogDebug($"Lease already released lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError($"Failed to release lease for '{lease.LeasePolicy.ActorName}'. The lease was held by another party. The lease name was '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and lease id '{lease.Id}'");
                throw new InvalidOperationException();
            }

            al.SetLastAcquired(null);
            this.logger.LogDebug($"Released lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
        }

        /// <inheritdoc/>
        public string ToLeaseToken(Lease lease)
        {
            if (lease is CosmosDbLease al)
            {
                return al.GetToken();
            }
            else
            {
                throw new TokenizationException();
            }
        }

        private static string GetLeaseObjectId(LeasePolicy leasePolicy)
        {
            return (leasePolicy.Name ?? Guid.NewGuid().ToString()).ToLowerInvariant();
        }

        /// <summary>
        /// This method verifies if the given ID owns the lease.
        /// </summary>
        private static bool OwnsLease(Stream content, string id)
        {
            using var jsonDocument = JsonDocument.Parse(content);
            return jsonDocument.RootElement.GetProperty("leaseId"u8).ValueEquals(id);
        }

        /// <summary>
        /// This method verifies if the given ID owns the lease, and if so, returns the ETag.
        /// </summary>
        private static bool OwnsLease(Stream content, string id, [NotNullWhen(true)] out string? eTag)
        {
            using var jsonDocument = JsonDocument.Parse(content);
            if (jsonDocument.RootElement.GetProperty("leaseId"u8).ValueEquals(id))
            {
                eTag = jsonDocument.RootElement.GetProperty("_etag"u8).GetString()!;
                return true;
            }

            eTag = null;
            return false;
        }

        private PartitionKey BuildPartitionKey(string leaseObjectId)
        {
            if (this.options.RootPartitionKeyValue is string rpk)
            {
                PartitionKeyBuilder builder = new();
                builder.Add(rpk);
                builder.Add(leaseObjectId);
                return builder.Build();
            }

            return new(leaseObjectId);
        }
    }
}