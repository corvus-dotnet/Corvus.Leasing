// <copyright file="LeasingContainerBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Azure.Specs.Helpers
{
    using System.Collections.Generic;
    using Corvus.Configuration;
    using Corvus.SpecFlow.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides Specflow bindings for Endjin Composition.
    /// </summary>
    [Binding]
    public static class LeasingContainerBindings
    {
        /// <summary>
        /// Setup the endjin container for a feature.
        /// </summary>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@setupContainer", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void SetupFeature(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    var fallbackSettings = new Dictionary<string, string>
                        {
                            { "STORAGEACCOUNTCONNECTIONSTRING", "UseDevelopmentStorage=true" },
                        };

                    var configurationBuilder = new ConfigurationBuilder();
                    
                    // Adding test configuration expects the name of the local settings file for the specs project. In this
                    // case we don't actually have one, so we're passing a dummy string for the file name. The configuration
                    // method doesn't need this file to actually exist.
                    configurationBuilder.AddTestConfiguration("settings.json", fallbackSettings);
                    IConfigurationRoot config = configurationBuilder.Build();
                    serviceCollection.AddSingleton(config);

                    var options = new AzureLeaseProviderOptions
                    {
                        StorageAccountConnectionString = config["STORAGEACCOUNTCONNECTIONSTRING"]
                    };

                    serviceCollection.AddTestNameProvider();
                    serviceCollection.AddAzureLeasing(options);
                });
        }

        /// <summary>
        /// Setup the endjin container for a feature.
        /// </summary>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@setupContainer", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static void SetupTest(FeatureContext featureContext)
        {
            ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITestNameProvider>().BeginTestSesion();
        }

        /// <summary>
        /// Tear down the endjin container for a feature.
        /// </summary>
        /// <param name="featureContext">The feature context for the current feature.</param>
        /// <remarks>We expect features run in parallel to be executing in separate app domains.</remarks>
        [AfterFeature("@setupContainer", Order = 1)]
        public static void TeardownContainer(FeatureContext featureContext)
        {
            featureContext.RunAndStoreExceptions(
                () => ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITestNameProvider>().CompleteTestSession());
        }
    }
}
