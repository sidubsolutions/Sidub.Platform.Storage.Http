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
using Sidub.Platform.Core.Services;
using Sidub.Platform.Filter.Parsers.OData;
using Sidub.Platform.Filter.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Commands.Responses;
using Sidub.Platform.Storage.Handlers.Http.Requests;
using Sidub.Platform.Storage.Services;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles the command to save a relation between two entities in OData storage.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent entity.</typeparam>
    /// <typeparam name="TRelated">The type of the related entity.</typeparam>
    public class EntitySaveRelationCommandHandler<TParent, TRelated> : ICommandHandler<SaveEntityRelationCommand<TParent, TRelated>, SaveEntityRelationCommandResponse>
        where TParent : class, IEntity
        where TRelated : class, IEntity
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly IDataProviderService<IFlurlClient> _dataProviderService;
        private readonly IEntityPartitionService _entityPartitionService;
        private readonly IEntitySerializerService _entitySerializerService;
        private readonly IFilterService<ODataFilterConfiguration> _filterService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitySaveRelationCommandHandler{TParent, TRelated}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public EntitySaveRelationCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
        {
            _metadataService = metadataService;
            _dataProviderService = dataProviderService;
            _entitySerializerService = entitySerializerService;
            _filterService = filterService;
            _entityPartitionService = entityPartitionService;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Executes the save entity relation command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The save entity relation command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The save entity relation command response.</returns>
        public async Task<SaveEntityRelationCommandResponse> Execute(StorageServiceReference ServiceReference, SaveEntityRelationCommand<TParent, TRelated> command, IQueryService queryService)
        {
            ODataStorageConnector storageConnector = _metadataService.GetMetadata<ODataStorageConnector>(ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            var parentEntity = command.ParentEntity;
            var relatedEntity = command.RelatedEntity;
            var relation = command.Relation;
            var parentEntityName = EntityTypeHelper.GetEntityName<TParent>();

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);
            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            var request = client.Request(parentEntityName);

            Dictionary<string, object?>? partitionKeyValue = null;

            // determine if partition is applicable...
            var partitionValue = _entityPartitionService.GetPartitionValue(parentEntity);

            if (partitionValue is not null && storageConnector.PartitionKeyFieldName is not null)
            {
                partitionKeyValue = new Dictionary<string, object?>()
                        {
                            { storageConnector.PartitionKeyFieldName, partitionValue }
                        };
            }

            // check if the entity has been retrieved from storage, else it is a new entity...
            HttpMethod method;

            // build out the identifier string for the related entity...
            var relatedEntityPredicateList = relatedEntity.EntityKeys.Select(x => $"{x.Key.FieldName} = {_filterService.GetFilterValueString(x.Value)}");
            var relatedEntityPredicateString = string.Join(",", relatedEntityPredicateList);

            if (parentEntity.IsRetrievedFromStorage)
            {
                // update serializer options to only serialize fields...
                serializerOptions = serializerOptions.With(x => x.FieldSerialization = EntityFieldType.Fields);

                // build out a string of entity key filters...
                var entityKeyValues = EntityTypeHelper.GetEntityKeyValues(parentEntity);
                var keyPredicateList = entityKeyValues.Select(x => $"{x.Key.FieldName} = {_filterService.GetFilterValueString(x.Value)}");
                var keyPredicateString = string.Join(",", keyPredicateList);

                // append the key string identifier...
                if (command.Relation.IsEnumerableRelation && command.IsDeleted)
                    request.Url += $"({keyPredicateString})/{relation.Name}({relatedEntityPredicateString})/$ref";
                else
                    request.Url += $"({keyPredicateString})/{relation.Name}/$ref";

                // perform a POST request...
                if (!relatedEntity.HasValue() || command.IsDeleted)
                    method = HttpMethod.Delete;
                else
                    method = HttpMethod.Post;
            }
            else
            {
                throw new Exception("Can't save relationship against record not retrieved from storage.");
            }


            var relationRequest = new ODataSaveEntityRelationRequest()
            {
                RelatedEntityId = command.Relation.IsEnumerableRelation && command.IsDeleted ? string.Empty : $"{client.BaseUrl}/{EntityTypeHelper.GetEntityName<TRelated>()}({relatedEntityPredicateString})"
            };

            // serialize the payload...
            var payload = _entitySerializerService.Serialize(relationRequest, serializerOptions, partitionKeyValue);

            var content = new ByteArrayContent(payload);
            var response = await request.SendAsync(method, content);

            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                // save relationships...
                var relations = EntityTypeHelper.GetEntityRelations<TParent>();
            }

            // TODO - response handling...
            //var entityDeserializer = new EntityDeserializer(responseString);
            //var entityResult = entityDeserializer.Deserialize<TEntity>();

            var saveResponse = new SaveEntityRelationCommandResponse
            {
                IsSuccessful = response.ResponseMessage.IsSuccessStatusCode
            };

            return saveResponse;
        }

        #endregion

    }

}
