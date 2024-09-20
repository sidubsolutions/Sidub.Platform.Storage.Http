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
    /// Handles querying entities using OData.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being queried.</typeparam>
    public class EntityQueryHandler<TEntity> : IRecordQueryHandler<IRecordQuery<TEntity>, TEntity> where TEntity : IEntity
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly IDataProviderService<IFlurlClient> _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;
        private readonly IFilterService<ODataFilterConfiguration> _filterService;

        private StorageServiceReference? _ServiceReference;
        private IRecordQuery<TEntity>? _query;

        #endregion

        #region Public properties

        /// <summary>
        /// Sets the storage service reference.
        /// </summary>
        public StorageServiceReference ServiceReference { set => _ServiceReference = value; }

        /// <summary>
        /// Sets the query.
        /// </summary>
        public IRecordQuery<TEntity> Query { set => _query = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityQueryHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        public EntityQueryHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService)
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
        /// Gets the entity based on the query.
        /// </summary>
        /// <param name="queryService">The query service.</param>
        /// <returns>The retrieved entity.</returns>
        public async Task<TEntity?> Get(IQueryService queryService)
        {
            if (_ServiceReference is null)
                throw new Exception("Undefined storage ServiceReference on query handler.");

            if (_query is null)
                throw new Exception("Undefined query on query handler.");

            ODataStorageConnector storageConnector = _metadataService.GetMetadata<ODataStorageConnector>(_ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            var entityLabel = EntityTypeHelper.GetEntityName<TEntity>();
            var relations = EntityTypeHelper.GetEntityRelations<TEntity>();
            var fields = EntityTypeHelper.GetEntityFields<TEntity>().ToList();

            if (EntityTypeHelper.IsEntityAbstract<TEntity>())
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

            TEntity? entity;

            var client = _dataProviderService.GetDataClient(_ServiceReference);

            var expandList = new List<string>();
            foreach (var i in relations)
            {
                var relatedKeys = EntityTypeHelper.GetEntityFields(i, EntityFieldType.Keys);

                if (!relatedKeys.Any())
                    throw new Exception("No keys on relation!?");

                var result = i.Name + "($select=";
                result += relatedKeys.Select(x => x.FieldName).Aggregate((x, y) => string.Join(",", x, y));
                result += ")";

                expandList.Add(result);
            }

            var request = client.Request(entityLabel)
                .SetQueryParam("$filter", filterString);

            if (fields is not null)
                request.SetQueryParam("$select", fields.Select(x => x.FieldName).Aggregate((x, y) => string.Join(",", x, y)));

            if (expandList.Any())
                request.SetQueryParam("$expand", expandList.Aggregate((x, y) => string.Join(",", x, y)));

            var response = await request.GetAsync();
            var responseData = await response.GetBytesAsync();

            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            serializerOptions.SerializeRelationships = true;

            // OData response objects are framed by the OData response object - the following converter is cognisant of how to
            //  deserialize the OData record response frame...
            if (serializerOptions is JsonEntitySerializerOptions jsonOptions)
            {
                jsonOptions.Converters.Add(new ODataRecordResponseConverter());
            }

            entity = _entitySerializerService.Deserialize<ODataRecordResponse<TEntity>>(responseData, serializerOptions).Value;

            if (entity is null)
                return entity;

            entity.IsRetrievedFromStorage = true;

            entity = await QueryHandlerHelper.AssignEntityReferenceProviders(queryService, _ServiceReference, entity);

            return entity;
        }

        #endregion

        #region IQueryHandler implementation

        /// <summary>
        /// Gets the entities based on the query.
        /// </summary>
        /// <param name="queryService">The query service.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>The retrieved entities.</returns>
        async IAsyncEnumerable<TEntity> IQueryHandler<IRecordQuery<TEntity>, TEntity>.Get(IQueryService queryService, QueryParameters? queryParameters)
        {
            var result = await Get(queryService);

            if (result is not null)
                yield return result;

            yield break;
        }

        #endregion

    }

}
