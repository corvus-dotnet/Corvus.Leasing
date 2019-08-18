// <copyright file="InMemoryLeaseProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Leasing;
    using Corvus.Leasing.Internal;

    /// <summary>
    /// Provides extension methods to use in memory leasing.
    /// </summary>
    public static class InMemoryLeaseProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Add the in memory implementation of leasing to the service collection.
        /// </summary>
        /// <param name="services">The service collection to which to add azure leasing.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddInMemoryLeasing(this IServiceCollection services)
        {
            services.AddSingleton<ILeaseProvider>(_ => new InMemoryLeaseProvider());
            return services;
        }
    }
}