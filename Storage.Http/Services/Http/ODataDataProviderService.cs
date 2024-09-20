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

using Flurl.Http;
using Flurl.Http.Configuration;
using Sidub.Platform.Authentication.Services;
using Sidub.Platform.Core;
using Sidub.Platform.Core.Services;

#endregion

namespace Sidub.Platform.Storage.Services.Http
{

    /// <summary>
    /// Represents a service for providing data access to OData endpoints.
    /// </summary>
    public class ODataDataProviderService : IDataProviderService<IFlurlClient>
    {

        #region Services

        private readonly IFlurlClientCache _clientCache;
        private readonly IServiceRegistry _metadataService;
        private readonly IAuthenticationService _authenticationService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataDataProviderService"/> class.
        /// </summary>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="clientCache">The client cache.</param>
        public ODataDataProviderService(IAuthenticationService authenticationService, IServiceRegistry metadataService, IFlurlClientCache clientCache)
        {
            _authenticationService = authenticationService;
            _metadataService = metadataService;
            _clientCache = clientCache;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the data client for the specified service reference.
        /// </summary>
        /// <param name="ServiceReferenceContext">The service reference context.</param>
        /// <returns>The data client.</returns>
        public IFlurlClient GetDataClient(ServiceReference ServiceReferenceContext)
        {
            IFlurlClient result;

            // retrieve connector...
            var connectors = _metadataService.GetMetadata<IHttpStorageConnector>(ServiceReferenceContext);

            // the ServiceReference context provided should only derive to a single storage connector... throw an
            //  exception if the context was too broad and includes multiple connectors; multiple connectors
            //  should be handled at a higher level...

            if (connectors is null || connectors.Count() != 1)
                throw new Exception($"Invalid connector count '{connectors?.Count()}' discovered with ServiceReference context '{ServiceReferenceContext.Name}' of type '{ServiceReferenceContext.GetType().Name}'.");

            var connector = connectors.Single();

            // build the connection string...
            var uri = connector.ServiceUri;

            if (!uri.Contains(@"://"))
                uri = $"https://{uri}";

            var serviceUrl = new Flurl.Url(uri);

            result = _clientCache.GetOrAdd(serviceUrl, serviceUrl);
            result = _authenticationService.AuthenticateClient(ServiceReferenceContext, result);

            foreach (var i in connector.RequestHeaders.Keys)
            {
                result.Headers.AddOrReplace(i, connector.RequestHeaders[i]);
            }

            return result;
        }

        #endregion

    }

}