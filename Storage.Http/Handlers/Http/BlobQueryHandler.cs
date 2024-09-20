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
using Sidub.Platform.Core.Entity.Relations;
using Sidub.Platform.Core.Serializers;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Filter;
using Sidub.Platform.Storage.Handlers.Http.Responses;
using Sidub.Platform.Storage.Queries;
using Sidub.Platform.Storage.Services;
using Sidub.Platform.Storage.Services.Http;


#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles queries for blobs in the storage service.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity.</typeparam>
    public class BlobQueryHandler<TEntity> : IQueryHandler<IQuery<EntityReference<TEntity>>, EntityReference<TEntity>>,
        IRecordQueryHandler<IRecordQuery<TEntity>, TEntity>,
        IEnumerableQueryHandler<IEnumerableQuery<TEntity>, TEntity>
        where TEntity : class, IEntity
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly ODataDataProviderService _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;

        private StorageServiceReference? _ServiceReference;
        private IQuery<EntityReference<TEntity>>? _query;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobQueryHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        public BlobQueryHandler(IServiceRegistry metadataService, ODataDataProviderService dataProviderService, IEntitySerializerService entitySerializerService)
        {
            _metadataService = metadataService;
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
        /// Gets or sets the blob query.
        /// </summary>
        public IQuery<EntityReference<TEntity>> Query { set => _query = value; }
        IRecordQuery<TEntity> IQueryHandler<IRecordQuery<TEntity>, TEntity>.Query { set => throw new NotImplementedException(); }
        IEnumerableQuery<TEntity> IQueryHandler<IEnumerableQuery<TEntity>, TEntity>.Query { set => throw new NotImplementedException(); }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves the entities based on the specified query.
        /// </summary>
        /// <param name="queryService">The query service.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>An asynchronous enumerable of entity references.</returns>
        public async IAsyncEnumerable<EntityReference<TEntity>> Get(IQueryService queryService, QueryParameters? queryParameters = null)
        {
            if (_ServiceReference is null)
                throw new Exception("Undefined storage ServiceReference on query handler.");

            if (_query is null)
                throw new Exception("Undefined query on query handler.");

            BlobStorageConnector storageConnector = _metadataService.GetMetadata<BlobStorageConnector>(_ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            var fields = EntityTypeHelper.GetEntityFields<TEntity>();
            var entityLabel = EntityTypeHelper.GetEntityName<TEntity>();
            var relations = EntityTypeHelper.GetEntityRelations<TEntity>();

            // validate and convert query to path filter segments...
            // currently blob only supports prefix filters without wildcards; that means
            //  filters can only be applied against sequential ordinal keys... i.e., we
            //  cannot filter on keys 1+3, only 3, etc... once indexing service support
            // is added, this will be expanded to support more complex filters...

            var filter = _query.GetFilter();

            List<object> filterSegmentValues = new List<object>();

            // ensure that the filters, if present, begin with ordinal position 1, are
            //  contiguous, and joined with AND logical operators
            if (filter is not null)
            {
                var predicates = new List<FilterPredicate>();
                var logicalOperators = new List<FilterLogicalOperator>();

                if (filter is FilterPipeline filterPipeline)
                {
                    foreach (var segment in filterPipeline.Filters)
                    {
                        if (segment is FilterPipeline)
                            throw new Exception("Nested filter pipelines are currently not supported by the blob service.");
                        else if (segment is FilterPredicate innerFilterPredicate)
                            predicates.Add(innerFilterPredicate);
                        else if (segment is FilterLogicalOperator innerLogicalOperator)
                            logicalOperators.Add(innerLogicalOperator);
                        else
                            throw new Exception("Unknown filter segment type.");
                    }
                }

                if (filter is FilterPredicate filterPredicate)
                    predicates.Add(filterPredicate);

                // ensure there are only "AND" logical operators...
                if (logicalOperators.Any(x => x.Operator != LogicalOperator.And))
                    throw new Exception("Only 'AND' logical operators are currently supported by the blob service.");

                // ensure that the predicates are contiguous and begin with ordinal position 1...
                var predicateKeys = predicates.ToDictionary(
                    x => EntityTypeHelper.GetEntityField<TEntity>(x.Field) ?? throw new Exception($"Entity field '{x.Field}' not found for entity '{typeof(TEntity).FullName}'."),
                    y => y
                );

                if (predicateKeys.Any(x => !x.Key.IsKeyField))
                    throw new Exception("Blob queries must be filtered only on key fields.");

                var expectedOrdinal = 1;

                foreach (var predicateKey in predicateKeys.OrderBy(x => x.Key.OrdinalPosition))
                {
                    if (predicateKey.Key.OrdinalPosition != expectedOrdinal)
                        throw new Exception("Blob queries must be filtered on contiguous ordinal positions starting at 1.");

                    expectedOrdinal++;

                    filterSegmentValues.Add(predicateKey.Value.Value);
                }

            }


            var filterSegments = filterSegmentValues.Select(BlobEntityTypeHelper.ParseKeyValueToKeyPathValue);
            var filterString = entityLabel + "/" + filterSegments.Aggregate((x, y) => $"{x}/{y}");
            var client = _dataProviderService.GetDataClient(_ServiceReference);
            var request = client.Request();

            request.SetQueryParam("resType", "container");
            request.SetQueryParam("comp", "list");

            if (!string.IsNullOrEmpty(filterString))
                request.SetQueryParam("prefix", filterString);

            var response = await request.GetAsync();
            var responseData = await response.GetBytesAsync();

            var serializerOptions = SerializerOptions.Default(storageConnector.SerializationLanguage);

            var entities = _entitySerializerService.Deserialize<BlobReferenceResponse<TEntity>>(responseData, serializerOptions);

            var referenceProvider = (IDictionary<IEntityField, object> keys) =>
            {
                var pathKey = EntityTypeHelper.GetEntityField<BlobReferenceResponseBlob>("Name")
                    ?? throw new Exception("Failed to find path field on 'BlobReferenceResponseBlob' record.");

                var responseBlob = new BlobReferenceResponseBlob()
                {
                    Path = keys[pathKey] as string ?? throw new Exception("Failed to get blob path.")
                };

                return Task.FromResult(responseBlob);
            };

            // at this point we've retrieved the response in a "blob reference model" type approach (i.e., the response is a list of blob records
            //  that satisfied the query); we need to use this information to generate a list of actual entity references of the given blob type
            //  and apply a provider that will retrieve the actual blob data from storage...
            foreach (var iBlobReference in entities.Blobs)
            {
                if (iBlobReference is not EntityReference<BlobReferenceResponseBlob> blobReference)
                    throw new Exception("Failed to cast blob reference to blob reference of type TEntity.");

                blobReference.Provider = referenceProvider;

                var blob = await blobReference.Get()
                    ?? throw new Exception("Failed to get blob reference.");

                var provider = async (IDictionary<IEntityField, object> keys) =>
                {
                    var query = new BlobDataQuery<TEntity>(blob.Path);
                    TEntity? result = null;

                    await foreach (var i in queryService.ExecuteQuery<BlobDataQuery<TEntity>, TEntity>(_ServiceReference, query))
                    {
                        result = i;
                        break;
                    }

                    return result;
                };

                //var keys = EntityTypeHelper.GetEntityKeyValues(blob);
                var entityDataReference = new EntityReference<TEntity>(iBlobReference.EntityKeys, provider);

                yield return entityDataReference;
            }

        }

        Task<TEntity?> IRecordQueryHandler<IRecordQuery<TEntity>, TEntity>.Get(IQueryService queryService)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<TEntity> IQueryHandler<IRecordQuery<TEntity>, TEntity>.Get(IQueryService queryService, QueryParameters? queryParameters)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<TEntity> IEnumerableQueryHandler<IEnumerableQuery<TEntity>, TEntity>.Get(IQueryService queryService, QueryParameters? queryParameters)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<TEntity> IQueryHandler<IEnumerableQuery<TEntity>, TEntity>.Get(IQueryService queryService, QueryParameters? queryParameters)
        {
            throw new NotImplementedException();
        }

        #endregion

    }

}
