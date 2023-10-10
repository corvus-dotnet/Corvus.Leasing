// <copyright file="LeasingContainerBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.CosmosDb.Specs.Helpers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Corvus.Configuration;
    using Corvus.Leasing.Internal;
    using Corvus.Testing.CosmosDb.Extensions;
    using Corvus.Testing.CosmosDb.SpecFlow;
    using Corvus.Testing.SpecFlow;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides SpecFlow bindings for Corvus.Leasing.CosmosDb.
    /// </summary>
    [Binding]
    public static class LeasingContainerBindings
    {
        public const string RootPkValue = "608aa751-e90f-4b8b-9be7-64fadc86de49"; // An imaginary tenant ID
        public const string LeaseContainerKey = "CosmosDbLeaseContainer";
        private const string UserHierarchicalPKTag = "useHierarchicalPK";

        /// <summary>
        /// Setup the endjin container for a feature.
        /// </summary>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@perFeatureContainer", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void SetupFeature(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    if (featureContext.FeatureInfo.Tags.Any(t => t == UserHierarchicalPKTag))
                    {
                        serviceCollection.AddSharedThroughputCosmosDbTestServices($"{CosmosDbLeaseProvider.RootPartitionKeyPath};/id");
                    }
                    else
                    {
                        serviceCollection.AddSharedThroughputCosmosDbTestServices("/id");
                    }

                    serviceCollection.AddSingleton(
                        s => BuildLeaseProvider(featureContext));
                });
        }

        /// <summary>
        /// Setup the lease container and add it to the feature context as <see cref="LeaseContainerKey"/>.
        /// </summary>
        /// <param name="featureContext">The SpecFlow test context.</param>
        /// <returns>A <see cref="Task"/> which completes when the lease container is configured.</returns>
        [BeforeFeature("@perFeatureContainer", Order = CosmosDbBeforeFeatureOrder.CreateContainer)]
        public static async Task SetupLeaseContainer(FeatureContext featureContext)
        {
            Database db = featureContext.Get<Database>(CosmosDbContextKeys.CosmosDbDatabase);

            Container leaseContainer = await db.CreateContainerIfNotExistsAsync(
                               GetContainerProperties(featureContext)).ConfigureAwait(false);
            CosmosDbContextBindings.AddFeatureLevelCosmosDbContainerForCleanup(featureContext, leaseContainer);
            featureContext.Set(leaseContainer, LeaseContainerKey);
        }

        /// <summary>
        /// Setup the endjin container for a feature.
        /// </summary>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@perFeatureContainer", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static void SetupTest(FeatureContext featureContext)
        {
            ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITestNameProvider>().BeginTestSesion();
        }

        /// <summary>
        /// Tear down the endjin container for a feature.
        /// </summary>
        /// <param name="featureContext">The feature context for the current feature.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [AfterFeature("@perFeatureContainer", Order = 1)]
        public static void TeardownContainer(FeatureContext featureContext)
        {
            featureContext.RunAndStoreExceptions(
                () => ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITestNameProvider>().CompleteTestSession());
        }

        private static ILeaseProvider BuildLeaseProvider(FeatureContext featureContext)
        {
            Container leaseContainer = featureContext.Get<Container>(LeaseContainerKey);
            if (featureContext.FeatureInfo.Tags.Any(t => t == UserHierarchicalPKTag))
            {
                return new CosmosDbLeaseProvider(leaseContainer, new CosmosDbLeaseProviderOptions { RootPartitionKeyValue = RootPkValue }, NullLogger<ILeaseProvider>.Instance);
            }
            else
            {
                return new CosmosDbLeaseProvider(leaseContainer, new CosmosDbLeaseProviderOptions { RootPartitionKeyValue = null }, NullLogger<ILeaseProvider>.Instance);
            }
        }

        private static ContainerProperties GetContainerProperties(FeatureContext featureContext)
        {
            bool useRootPartitionKey = featureContext.FeatureInfo.Tags.Any(t => t == UserHierarchicalPKTag);
            return CosmosDbLeaseProvider.GetContainerProperties($"leasecontainer_{Guid.NewGuid()}", useRootPartitionKey);
        }
    }
}
