// <copyright file="LeasingContainerBindings.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

namespace Endjin.Leasing.Azure.Specs.Helpers
{
    using System.Collections.Generic;
    using Endjin.Configuration;
    using Endjin.SpecFlow.Bindings;
    using Endjin.Test;

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
            TestConfigurationService.EnableTestSession();

            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    var fallbackSettings = new Dictionary<string, string>
                        {
                            { "StorageAccountConnectionString", "UseDevelopmentStorage=true" },
                        };
                    serviceCollection.AddTestConfiguration(fallbackSettings);

                    serviceCollection.AddEndjinJsonConverters();
                    serviceCollection.AddLogging();
                    serviceCollection.AddAzureLeasing();
                });
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
                () =>
                {
                    TestConfigurationService.CompleteTestSession();
                });
        }
    }
}
