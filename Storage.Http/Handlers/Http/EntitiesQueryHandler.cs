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
using Sidub.Platform.Core;
using Sidub.Platform.Core.Entity;
using Sidub.Platform.Core.Extensions;
using Sidub.Platform.Core.Serializers;
using Sidub.Platform.Core.Serializers.Json;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Filter.Parsers.OData;
using Sidub.Platform.Filter.Services;
using Sidub.Platform.Storage.Handlers.Http.Responses;
using Sidub.Platform.Storage.Queries;
using Sidub.Platform.Storage.Services;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{
    /// <summary>
    /// Handles the query for retrieving entities using OData.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class EntitiesQueryHandler<TEntity> : IEnumerableQueryHandler<IEnumerableQuery<TEntity>, TEntity> where TEntity : IEntity
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly IDataProviderService<IFlurlClient> _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;
        private readonly IFilterService<ODataFilterConfiguration> _filterService;
        private StorageServiceReference? _ServiceReference;
        private IEnumerableQuery<TEntity>? _query;

        #endregion

        #region Public properties

        /// <summary>
        /// Sets the storage service reference.
        /// </summary>
        public StorageServiceReference ServiceReference { set => _ServiceReference = value; }

        /// <summary>
        /// Sets the query.
        /// </summary>
        public IEnumerableQuery<TEntity> Query { set => _query = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesQueryHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        public EntitiesQueryHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService)
        {
            _metadataService = metadataService;
            _dataProviderService = dataProviderService;
            _entitySerializerService = entitySerializerService;
            _filterService = filterService;

            _ServiceReference = null;
            _query = null;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves the entities based on the query parameters.
        /// </summary>
        /// <param name="queryService">The query service.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>The entities.</returns>
        public async IAsyncEnumerable<TEntity> Get(IQueryService queryService, QueryParameters? queryParameters = null)
        {
            if (_ServiceReference is null)
                throw new Exception("Undefined storage ServiceReference on query handler.");

            if (_query is null)
                throw new Exception("Undefined query on query handler.");

            ODataStorageConnector storageConnector = _metadataService.GetMetadata<ODataStorageConnector>(_ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            var fields = EntityTypeHelper.GetEntityFields<TEntity>().ToList();
            var entityLabel = EntityTypeHelper.GetEntityName<TEntity>();
            var relations = EntityTypeHelper.GetEntityRelations<TEntity>();

            if (typeof(TEntity).IsInterface || typeof(TEntity).IsAbstract)
            {
                // temporary workaround - if we're querying by an abstract type, we don't technically know all the columns we need to query... therefore,
                //  we'll omit the select case so that we retrieve all fields... work item #9...
                fields = null;

                //// add entity type discriminator in selection...
                //var field = TypeDiscriminatorEntityField.Instance;
                //fields.Add(field);
            }

            var filter = _query.GetFilter();
            var filterString = _filterService.GetFilterString(filter);

            var client = _dataProviderService.GetDataClient(_ServiceReference);

            var expandList = new List<string>();
            foreach (var i in relations)
            {
                var relatedKeys = EntityTypeHelper.GetEntityFields(i, EntityFieldType.Keys);

                if (relatedKeys.Count() == 0)
                    throw new Exception("No keys on relation!?");

                var result = i.Name + "($select=";
                result += relatedKeys.Select(x => x.FieldName).Aggregate((x, y) => string.Join(",", x, y));
                result += ")";

                expandList.Add(result);
            }

            var request = client.Request(entityLabel);

            if (fields is not null)
                request.SetQueryParam("$select", fields.Select(x => x.FieldName).Aggregate((x, y) => string.Join(",", x, y)));

            if (!string.IsNullOrEmpty(filterString))
                request.SetQueryParam("$filter", filterString);

            if (queryParameters?.Top is not null)
                request.SetQueryParam("$top", queryParameters.Top);

            if (queryParameters?.Skip is not null)
                request.SetQueryParam("$skip", queryParameters.Skip);

            if (expandList.Any())
                request.SetQueryParam("$expand", expandList.Aggregate((x, y) => string.Join(",", x, y)));

            var response = await request.GetAsync();
            var responseData = await response.GetBytesAsync();

            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);

            if (serializerOptions is JsonEntitySerializerOptions jsonOptions)
            {
                jsonOptions.Converters.Add(new ODataEnumerableResponseConverter());
            }

            IEnumerable<TEntity> entities = _entitySerializerService.Deserialize<ODataEnumerableResponse<TEntity>>(responseData, serializerOptions).Value;

            foreach (var i in entities)
            {
                var entity = await QueryHandlerHelper.AssignEntityReferenceProviders(queryService, _ServiceReference, i);
                yield return entity.With(x => x.IsRetrievedFromStorage = true);
            }

            if (!string.IsNullOrEmpty(null)) // TODO
            {
                throw new NotImplementedException("Next link support not implemented.");
            }
            // if top / skip approach is used, only continue pagination if entities have been retrieved from the last request...
            else if (queryParameters?.Top is not null && entities.Any())
            {
                // incrememnt the skip by the top and retrieve the next page...
                var newQueryParameters = queryParameters; // TODO - need to clone?

                if (newQueryParameters.Skip is null)
                    newQueryParameters.Skip = 0;

                newQueryParameters.Skip += newQueryParameters.Top;

                await foreach (var i in Get(queryService, newQueryParameters))
                {
                    yield return i;
                }
            }
        }

        #endregion

    }

}
