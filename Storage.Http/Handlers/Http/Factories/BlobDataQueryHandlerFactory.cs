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

using Sidub.Platform.Core.Services;
using Sidub.Platform.Storage.Queries;
using Sidub.Platform.Storage.Services.Http;


#endregion

namespace Sidub.Platform.Storage.Handlers.Http.Factories
{

    /// <summary>
    /// Factory class for creating BlobDataQueryHandler instances.
    /// </summary>
    public class BlobDataQueryHandlerFactory : IQueryHandlerFactory<BlobStorageConnector>
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly ODataDataProviderService _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDataQueryHandlerFactory"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        public BlobDataQueryHandlerFactory(IServiceRegistry metadataService, ODataDataProviderService dataProviderService, IEntitySerializerService entitySerializerService)
        {
            _metadataService = metadataService;
            _dataProviderService = dataProviderService;
            _entitySerializerService = entitySerializerService;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Determines whether the specified query and response types are handled by this factory.
        /// </summary>
        /// <typeparam name="TQuery">The type of the query.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns><c>true</c> if the specified query and response types are handled; otherwise, <c>false</c>.</returns>
        public bool IsHandled<TQuery, TResponse>()
            where TQuery : IQuery<TResponse>
        {
            // ensure command conforms to IBlobQuery<TEntity> where TEntity : IEntity...
            if (typeof(TQuery).GetGenericTypeDefinition() == typeof(BlobDataQuery<>))
                return true;

            return false;
        }

        /// <summary>
        /// Creates a new instance of the query handler for the specified query and response types.
        /// </summary>
        /// <typeparam name="TQuery">The type of the query.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <returns>An instance of the query handler.</returns>
        /// <exception cref="Exception">Thrown when the specified query and response types are not handled.</exception>
        public IQueryHandler<TQuery, TResponse> Create<TQuery, TResponse>()
            where TQuery : IQuery<TResponse>
        {
            if (!IsHandled<TQuery, TResponse>())
                throw new Exception("Unhandled type.");

            // retrieve TEntity from AzureSaveBlobCommand<TEntity>... note prior validation has ensured this is of IEntity type...
            var entityType = typeof(TQuery).GenericTypeArguments.Single();

            var handlerType = typeof(BlobDataQueryHandler<>).MakeGenericType([entityType]);
            var handlerParameters = new object[] { _dataProviderService, _entitySerializerService };
            var handler = Activator.CreateInstance(handlerType, handlerParameters);

            if (handler is not IQueryHandler<TQuery, TResponse> handlerCast)
                throw new Exception("Failed to cast handler.");

            return handlerCast;
        }

        #endregion

    }

}
