// <copyright file="AzureLeaseProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using Corvus.Configuration;
    using Corvus.Leasing;
    using Corvus.Leasing.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides extension methods to use azure leasing.
    /// </summary>
    public static class AzureLeaseProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Azure implementation of leasing to the service collection.
        /// </summary>
        /// <param name="services">The service collection to which to add azure leasing.</param>
        /// <param name="options">The configuration options.</param>
        /// <param name="configureLeasing">An optional configuration function for leasing.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddAzureLeasing(this IServiceCollection services, AzureLeaseProviderOptions options, Action<AzureLeaseProvider> configureLeasing = null)
        {
            if (services.Any(s => typeof(ILeaseProvider).IsAssignableFrom(s.ServiceType)))
            {
                // Already configured
                return services;
            }

            // You can override by adding test name provider before leasing
            // if you need test services.
            services.AddNameProvider();

            // You can override by adding logging before adding the lease provider.
            services.AddLogging();

            services.AddSingleton<ILeaseProvider>((sp) =>
            {
                var leaseProvider = new AzureLeaseProvider(
                    sp.GetRequiredService<ILogger<AzureLeaseProvider>>(),
                    options,
                    sp.GetRequiredService<INameProvider>());

                configureLeasing?.Invoke(leaseProvider);

                return leaseProvider;
            });
            return services;
        }
    }
}
