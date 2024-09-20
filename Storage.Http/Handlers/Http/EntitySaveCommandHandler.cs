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
using Sidub.Platform.Storage.Services;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles the command to save an entity in the OData storage.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class EntitySaveCommandHandler<TEntity, TStorageConnector> : ICommandHandler<SaveEntityCommand<TEntity>, SaveEntityCommandResponse<TEntity>>
        where TEntity : IEntity
        where TStorageConnector : IHttpStorageConnector
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
        /// Initializes a new instance of the <see cref="EntitySaveCommandHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public EntitySaveCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
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
        /// Executes the save entity command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The save entity command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The response of the save entity command.</returns>
        public async Task<SaveEntityCommandResponse<TEntity>> Execute(StorageServiceReference ServiceReference, SaveEntityCommand<TEntity> command, IQueryService queryService)
        {
            var entity = command.Entity;
            var entityLabel = EntityTypeHelper.GetEntityName(entity);

            TStorageConnector storageConnector = _metadataService.GetMetadata<TStorageConnector>(ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);
            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            var request = client.Request(entityLabel);

            Dictionary<string, object?> additionalFields = new();

            // determine if partition is applicable...
            var partitionValue = _entityPartitionService.GetPartitionValue(entity);


            // check if the entity has been retrieved from storage, else it is a new entity...
            HttpMethod method;

            if (entity.IsRetrievedFromStorage)
            {
                // performing an update...
                method = new HttpMethod("PATCH");

                // update serializer options to only serialize fields...
                serializerOptions = serializerOptions.With(x => x.FieldSerialization = EntityFieldType.Fields);

                // build out a string of entity key filters...
                var entityKeyValues = EntityTypeHelper.GetEntityKeyValues(entity);



                var keyPredicateList = entityKeyValues.Select(x => $"{x.Key.FieldName} = {_filterService.GetFilterValueString(x.Value)}").ToList();


                // add the partition value into the keys... TODO - better approach?
                if (partitionValue is not null && storageConnector.PartitionKeyFieldName is not null)
                    keyPredicateList.Add($"{storageConnector.PartitionKeyFieldName} = {_filterService.GetFilterValueString(partitionValue)}");


                var keyPredicateString = string.Join(",", keyPredicateList);

                // append the key string identifier...
                request.Url += "(" + keyPredicateString + ")";
            }
            else
            {
                // perform an insert...
                method = HttpMethod.Post;

                // add the partition value to the payload for inserts...
                if (partitionValue is not null && storageConnector.PartitionKeyFieldName is not null)
                    additionalFields.Add(storageConnector.PartitionKeyFieldName, partitionValue);
            }

            // one-to-one / many-to-one relationships updated via. field@odata.bind, so inject these into serialization additional fields...
            var recordRelations = EntityTypeHelper.GetEntityRelations<TEntity>()
                .Where(relation => !relation.IsEnumerableRelation);

            foreach (var relation in recordRelations)
            {
                // one-to-one, so only one reference will be returned..
                var entityRef = EntityTypeHelper.GetEntityRelationRecord(entity, relation)
                    ?? throw new Exception("Failed to get entity relation record reference; if value is intended to be null, it should be assigned IEntityReference.Null value.");

                if (entityRef.Action == Core.Entity.Relations.EntityRelationActionType.None)
                    continue;

                var saveRelation = SaveEntityRelationCommand.Create(relation, entity, entityRef);
                var saveRelationResult = await queryService.Execute(ServiceReference, saveRelation);

                if (!saveRelationResult.IsSuccessful)
                    throw new Exception($"Error saving entity relation '{relation.Name}'.");

            }

            // serialize the payload...
            var payload = _entitySerializerService.Serialize(entity, serializerOptions, additionalFields);

            var content = new ByteArrayContent(payload);
            var response = await request.SendAsync(method, content);

            var enumerableRelations = EntityTypeHelper.GetEntityRelations<TEntity>()
                .Where(relation => relation.IsEnumerableRelation);

            foreach (var relation in enumerableRelations)
            {
                // one-to-many / many-to-many
                var entityRefs = EntityTypeHelper.GetEntityRelationEnumerable(entity, relation)
                    ?? throw new Exception("Failed to get entity relation list reference; if value is intended to be null, it should be assigned an empty IEntityReferenceList value.");

                foreach (var entityRef in entityRefs)
                {
                    if (entityRef.Action == Core.Entity.Relations.EntityRelationActionType.None)
                        continue;

                    var saveRelation = SaveEntityRelationCommand.Create(relation, entity, entityRef);
                    var saveRelationResult = await queryService.Execute(ServiceReference, saveRelation);

                    if (!saveRelationResult.IsSuccessful)
                        throw new Exception($"Error saving entity relation '{relation.Name}'.");
                }

                foreach (var removedEntityRef in entityRefs.RemovedReferences)
                {
                    var saveRelation = SaveEntityRelationCommand.Create(relation, entity, removedEntityRef);
                    var saveRelationResult = await queryService.Execute(ServiceReference, saveRelation);

                    if (!saveRelationResult.IsSuccessful)
                        throw new Exception($"Error saving entity relation '{relation.Name}'.");
                }

                entityRefs.Commit();
            }

            // TODO - response handling...
            //var entityDeserializer = new EntityDeserializer(responseString);
            //var entityResult = entityDeserializer.Deserialize<TEntity>();

            var saveResponse = new SaveEntityCommandResponse<TEntity>(response.ResponseMessage.IsSuccessStatusCode, entity);

            return saveResponse;
        }

        #endregion

    }

}
