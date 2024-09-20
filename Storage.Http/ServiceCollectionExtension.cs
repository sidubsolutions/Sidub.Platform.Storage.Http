/*
 * Sidub Platform - Storage - HTTP
 * Copyright (C) 2024 Sidub Inc.
 * All rights reserved.
 *
 * This file is part of Sidub Platform - Storage - HTTP (the "Product").
 *
 * The Product is dual-licensed under:
 * 1. The GNU Affero General Public License version 3 (AGPLv3)
 * 2. Sidub Inc.'s Proprietary Software License Agreement (PSLA)
 *
 * You may choose to use, redistribute, and/or modify the Product under
 * the terms of either license.
 *
 * The Product is provided "AS IS" and "AS AVAILABLE," without any
 * warranties or conditions of any kind, either express or implied, including
 * but not limited to implied warranties or conditions of merchantability and
 * fitness for a particular purpose. See the applicable license for more
 * details.
 *
 * See the LICENSE.txt file for detailed license terms and conditions or
 * visit https://sidub.ca/licensing for a copy of the license texts.
 */

#region Imports

using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sidub.Platform.Authentication;
using Sidub.Platform.Filter;
using Sidub.Platform.Storage.Handlers;
using Sidub.Platform.Storage.Handlers.Http.Factories;
using Sidub.Platform.Storage.Services;
using Sidub.Platform.Storage.Services.Http;

#endregion

namespace Sidub.Platform.Storage
{

    /// <summary>
    /// Static helper class providing IServiceCollection extensions.
    /// </summary>
    public static class ServiceCollectionExtension
    {

        #region Extension methods

        /// <summary>
        /// Adds Sidub storage for HTTP to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection AddSidubStorageForHttp(
            this IServiceCollection services)
        {
            services.AddSidubAuthentication();
            services.AddSidubStorage();

            // OData service registration...
            services.AddSidubFilter(FilterParserType.OData)
                .AddTransient<IFlurlClientCache, FlurlClientCache>()
                .AddTransient<ODataDataProviderService>()
                .AddTransient<IDataHandlerService, ODataDataHandlerService>()
                .AddTransient<IDataHandlerService, BlobDataHandlerService>()
                .AddTransient<IDataHandlerService, QueueDataHandlerService>();

            services.TryAddEnumerable(ServiceDescriptor.Transient<ICommandHandlerFactory<ODataStorageConnector>, EntitySaveRelationCommandHandlerFactory>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<ICommandHandlerFactory<ODataStorageConnector>, ActionCommandHandlerFactory>());

            services.TryAddEnumerable(ServiceDescriptor.Transient<IQueryHandlerFactory<BlobStorageConnector>, BlobDataQueryHandlerFactory>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IQueryHandlerFactory<BlobStorageConnector>, BlobQueryHandlerFactory>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<ICommandHandlerFactory<BlobStorageConnector>, SaveBlobCommandHandlerFactory>());

            services.TryAddEnumerable(ServiceDescriptor.Transient<ICommandHandlerFactory<QueueStorageConnector>, SaveQueueMessageCommandHandlerFactory>());

            return services;
        }

        #endregion

    }

}
