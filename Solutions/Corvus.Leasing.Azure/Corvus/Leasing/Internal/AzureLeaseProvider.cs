// <copyright file="AzureLeaseProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Configuration;
    using Corvus.Leasing.Exceptions;
    using Corvus.Retry;
    using Corvus.Retry.Policies;
    using Corvus.Retry.Strategies;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// The platform specific implementation used for lease operations.
    /// </summary>
    public class AzureLeaseProvider : ILeaseProvider
    {
        private readonly ILogger<ILeaseProvider> logger;
        private readonly AzureLeaseProviderOptions options;
        private readonly INameProvider nameProvider;
        private bool initialised;
        private CloudStorageAccount? storageAccount;
        private CloudBlobClient? client;
        private CloudBlobContainer? container;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureLeaseProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The configuration options.</param>
        /// <param name="nameProvider">The name provider service.</param>
        public AzureLeaseProvider(ILogger<ILeaseProvider> logger, AzureLeaseProviderOptions options, INameProvider nameProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.nameProvider = nameProvider ?? throw new ArgumentNullException(nameof(nameProvider));
        }

        /// <summary>
        /// Gets or sets the blob container name for leases.
        /// </summary>
        /// <remarks>If this name is not set, it will default to a container called "genericleases".</remarks>
        public string? ContainerName { get; set; }

        /// <summary>
        /// Gets the default lease duration for the specific platform implementation of the lease.
        /// </summary>
        /// <remarks>This is currently 59 seconds.</remarks>
        public TimeSpan DefaultLeaseDuration
        {
            get { return TimeSpan.FromSeconds(59); }
        }

        /// <inheritdoc/>
        public async Task<Lease> AcquireAsync(LeasePolicy leasePolicy, string? proposedLeaseId = null)
        {
            ArgumentNullException.ThrowIfNull(leasePolicy);

            this.logger.LogDebug($"Acquiring lease for '{leasePolicy.ActorName}' with name '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and proposed id '{proposedLeaseId}'");

            try
            {
                await this.InitialiseAsync().ConfigureAwait(false);

                // Once Initialise has been called, we know this.container is set.
                CloudBlockBlob blob = this.container!.GetBlockBlobReference((leasePolicy.Name ?? Guid.NewGuid().ToString()).ToLowerInvariant());

                await Retriable.RetryAsync(
                    async () =>
                    {
                        try
                        {
                            if (!await blob.ExistsAsync().ConfigureAwait(false))
                            {
                                using var ms = new MemoryStream();
                                await blob.UploadFromStreamAsync(ms).ConfigureAwait(false);
                            }
                        }
                        catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 400)
                        {
                            // Turn that into an invalid operation exception for standard "bad request" semantics (bad name)
                            throw new InvalidOperationException();
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Turn that into an invalid operation exception for standard "bad request" semantics (bad duration)
                            throw new InvalidOperationException();
                        }
                    },
                    CancellationToken.None,
                    new Count(10),
                    new AggregatePolicy { Policies = { new DoNotRetryOnInvalidOperationPolicy(), new DoNotRetryOnConflictPolicy(), new DoNotRetryOnInitializationFailurePolicy() } }).ConfigureAwait(false);

                string id = await Retriable.RetryAsync(
                        async () =>
                        {
                            try
                            {
                                return await blob.AcquireLeaseAsync(leasePolicy.Duration, proposedLeaseId);
                            }
                            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 400)
                            {
                                // Turn that into an invalid operation exception for standard "bad request" semantics (bad name)
                                throw new InvalidOperationException();
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                // Turn that into an invalid operation exception for standard "bad request" semantics (bad duration)
                                throw new InvalidOperationException();
                            }
                        },
                        CancellationToken.None,
                        new Count(10),
                        new AggregatePolicy { Policies = { new DoNotRetryOnInvalidOperationPolicy(), new DoNotRetryOnConflictPolicy(), new DoNotRetryOnInitializationFailurePolicy() } }).ConfigureAwait(false);

                var lease = new AzureLease(this, leasePolicy, id);
                lease.SetLastAcquired(DateTimeOffset.Now);

                this.logger.LogDebug($"Acquired lease for '{leasePolicy.ActorName}' with name '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and actual id '{id}'");

                return lease;
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode == 409)
                {
                    this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}'. The lease was held by another party. The lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and proposed id '{proposedLeaseId}'");
                    throw new LeaseAcquisitionUnsuccessfulException(leasePolicy, exception);
                }
                else
                {
                    this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}' due to storage failure. The lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and proposed id '{proposedLeaseId}'");
                    throw;
                }
            }
            catch (InitializationFailureException)
            {
                this.logger.LogError($"Failed to acquire lease for '{leasePolicy.ActorName}' due to storage intiialization failure. The lease name was '{leasePolicy.Name}', duration '{leasePolicy.Duration}', and proposed id '{proposedLeaseId}'");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ExtendAsync(Lease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);

            this.logger.LogDebug($"Extending lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
            await this.InitialiseAsync().ConfigureAwait(false);
            CloudBlockBlob blob = this.container!.GetBlockBlobReference(lease.LeasePolicy.Name.ToLowerInvariant());
            await Retriable.RetryAsync(() => blob.RenewLeaseAsync(new AccessCondition { LeaseId = lease.Id })).ConfigureAwait(false);
            (lease as AzureLease)?.SetLastAcquired(DateTimeOffset.Now);
            this.logger.LogDebug($"Extended lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
        }

        /// <inheritdoc/>
        public Lease FromLeaseToken(string leaseToken)
        {
            ArgumentNullException.ThrowIfNull(leaseToken);

            return AzureLease.FromToken(this, leaseToken);
        }

        /// <inheritdoc/>
        public async Task ReleaseAsync(Lease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);

            if (lease is not AzureLease al)
            {
                throw new ArgumentException("Only Leases of type 'AzureLease' can be released by the AzureLeaseProvider.");
            }

            this.logger.LogDebug($"Releasing lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
            await this.InitialiseAsync().ConfigureAwait(false);
            CloudBlockBlob blob = this.container!.GetBlockBlobReference(lease.LeasePolicy.Name.ToLowerInvariant());

            await Retriable.RetryAsync(() => blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = lease.Id })).ConfigureAwait(false);

            al.SetLastAcquired(null);
            this.logger.LogDebug($"Released lease for '{lease.LeasePolicy.ActorName}' with name '{lease.LeasePolicy.Name}', duration '{lease.LeasePolicy.Duration}', and actual id '{lease.Id}'");
        }

        /// <inheritdoc/>
        public string ToLeaseToken(Lease lease)
        {
            if (lease is AzureLease al)
            {
                return al.GetToken();
            }
            else
            {
                throw new TokenizationException();
            }
        }

        /// <summary>
        /// Gets the cloud storage account for the configured connection string.
        /// </summary>
        /// <returns>An instance of the cloud storage account for blob-based leasing.</returns>
        protected virtual CloudStorageAccount GetStorageAccount()
        {
            return CloudStorageAccount.Parse(this.options.StorageAccountConnectionString);
        }

        private string GetContainerName()
        {
            string cn = this.ContainerName ?? "genericleases";
            return this.nameProvider.ProvideName(cn, 63, NameCase.LowerInvariant);
        }

        private async Task InitialiseAsync()
        {
            if (!this.initialised)
            {
                try
                {
                    this.storageAccount = this.GetStorageAccount();

                    this.client = Retriable.Retry(() => this.storageAccount.CreateCloudBlobClient());

                    string containerName = this.GetContainerName();
                    this.container = Retriable.Retry(() => this.client.GetContainerReference(containerName));

                    if (await this.container.CreateIfNotExistsAsync().ConfigureAwait(false))
                    {
                        var containerPermissions = new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off };

                        await Retriable.RetryAsync(() => this.container.SetPermissionsAsync(containerPermissions)).ConfigureAwait(false);
                    }

                    this.initialised = true;
                }
                catch (Exception ex)
                {
                    throw new InitializationFailureException("Initialization failed.", ex);
                }
            }
        }
    }
}