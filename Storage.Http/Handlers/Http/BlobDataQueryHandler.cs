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
using Sidub.Platform.Core.Entity;
using Sidub.Platform.Core.Serializers;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Storage.Queries;
using Sidub.Platform.Storage.Services;
using Sidub.Platform.Storage.Services.Http;


#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles the query for retrieving blob data.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class BlobDataQueryHandler<TEntity> : IQueryHandler<BlobDataQuery<TEntity>, TEntity> where TEntity : class, IEntity, new()
    {

        #region Member variables

        private readonly ODataDataProviderService _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;
        private StorageServiceReference? _ServiceReference;
        private BlobDataQuery<TEntity>? _query;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDataQueryHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        public BlobDataQueryHandler(ODataDataProviderService dataProviderService, IEntitySerializerService entitySerializerService)
        {
            _dataProviderService = dataProviderService;
            _entitySerializerService = entitySerializerService;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the storage service reference.
        /// </summary>
        public StorageServiceReference ServiceReference { set => _ServiceReference = value; }

        /// <summary>
        /// Gets or sets the blob data query.
        /// </summary>
        public BlobDataQuery<TEntity> Query { set => _query = value; }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves the blob data.
        /// </summary>
        /// <returns>The retrieved entity.</returns>
        public async Task<TEntity?> Get()
        {
            if (_ServiceReference is null)
                throw new Exception("Undefined storage ServiceReference on query handler.");

            if (_query is null)
                throw new Exception("Undefined query on query handler.");

            var client = _dataProviderService.GetDataClient(_ServiceReference);
            var dataRequest = client.Request(_query.BlobPath);
            var dataResponse = await dataRequest.GetBytesAsync();
            var serializerOptions = SerializerOptions.Default(SerializationLanguageType.Json);

            var result = _entitySerializerService.Deserialize<TEntity>(dataResponse, serializerOptions);

            return result;
        }

        #endregion

        #region IQueryHandler implementation

        /// <summary>
        /// Retrieves the blob data as an asynchronous enumerable.
        /// </summary>
        /// <param name="queryService">The query service.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>The asynchronous enumerable of retrieved entities.</returns>
        async IAsyncEnumerable<TEntity> IQueryHandler<BlobDataQuery<TEntity>, TEntity>.Get(IQueryService queryService, QueryParameters? queryParameters)
        {
            var result = await Get();

            if (result is null)
                yield break;

            yield return result;
        }

        #endregion

    }

}
