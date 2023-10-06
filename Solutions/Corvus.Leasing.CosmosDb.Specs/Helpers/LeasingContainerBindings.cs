﻿// <copyright file="LeasingContainerBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.CosmosDb.Specs.Helpers
{
    using System.Collections.Generic;
    using Corvus.Configuration;
    using Corvus.Testing.SpecFlow;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides Specflow bindings for Corvus.Leasing.CosmosDb.
    /// </summary>
    [Binding]
    public static class LeasingContainerBindings
    {
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
                    var fallbackSettings = new Dictionary<string, string>
                        {
                            { "COSMOSDBCONNECTIONSTRING", "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" },
                        };

                    var configurationBuilder = new ConfigurationBuilder();
                    configurationBuilder.AddConfigurationForTest(null, fallbackSettings);
                    IConfigurationRoot config = configurationBuilder.Build();
                    serviceCollection.AddSingleton(config);

                    var options = new CosmosDbLeaseProviderOptions
                    {
                        RootPartitionKeyValue = null,
                        CosmosDbConnectionString = config["COSMOSDBCONNECTIONSTRING"],
                    };

                    serviceCollection.AddTestNameProvider();
                    //// TODO: Figure out how we're going to register this
                    //// serviceCollection.AddCosmosDbLeasing(options);
                });
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
    }
}
