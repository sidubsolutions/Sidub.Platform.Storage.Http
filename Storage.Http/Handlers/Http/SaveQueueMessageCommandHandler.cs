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
using Sidub.Platform.Core.Services;
using Sidub.Platform.Filter.Parsers.OData;
using Sidub.Platform.Filter.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Commands.Responses;
using Sidub.Platform.Storage.Entities;
using Sidub.Platform.Storage.Services;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles the command to save a queue message for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to save.</typeparam>
    public class SaveQueueMessageCommandHandler<TEntity> : ICommandHandler<SaveEntityCommand<TEntity>, SaveEntityCommandResponse<TEntity>>
        where TEntity : IEntity
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly IDataProviderService<IFlurlClient> _dataProviderService;
        private readonly IEntityPartitionService _entityPartitionService;
        private readonly IEntitySerializerService _entitySerializerService;
        private readonly IFilterService<ODataFilterConfiguration> _filterService;

        private SerializationLanguageType? _serializationLanuguage;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the serialization language type.
        /// </summary>
        public SerializationLanguageType SerializationLanguage { set => _serializationLanuguage = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveQueueMessageCommandHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public SaveQueueMessageCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
        {
            _metadataService = metadataService;
            _dataProviderService = dataProviderService;
            _entitySerializerService = entitySerializerService;
            _filterService = filterService;
            _entityPartitionService = entityPartitionService;
        }

        #endregion

        #region Public async methods

        /// <summary>
        /// Executes the save queue message command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The save queue message command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The response of the save queue message command.</returns>
        public async Task<SaveEntityCommandResponse<TEntity>> Execute(StorageServiceReference ServiceReference, SaveEntityCommand<TEntity> command, IQueryService queryService)
        {

            var entity = command.Entity;
            var entityLabel = EntityTypeHelper.GetEntityName(entity);

            // generate an IDictionary containing the data to submit...
            byte[] payload;

            var partitionValue = _entityPartitionService.GetPartitionValue(entity);

            QueueStorageConnector storageConnector = _metadataService.GetMetadata<QueueStorageConnector>(ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            byte[] responseData;

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);
            //using (var client = _dataProviderService.GetDataClient(ServiceReference))
            //{
            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            var request = client.Request("messages"); // TODO - not the best...

            IFlurlResponse response;

            // check if the entity has been retrieved from storage, else it is a new entity...
            if (entity.IsRetrievedFromStorage)
            {
                throw new Exception("Resubmitting entity to messaging service is not supported; rather create a new instance of the entity and submit it.");
            }
            else
            {
                // if TEntity is QueueMessage, then we just broadcast the queue message data... if TEntity is another type of entity, then we serialize the
                //  the entity and assign it as the payload of a QueueMessage
                payload = _entitySerializerService.Serialize(entity, serializerOptions);

                if (entity is not QueueMessage)
                {
                    var messageData = _entitySerializerService.Serialize(entity, serializerOptions);
                    var message = new QueueMessage(messageData);
                    payload = _entitySerializerService.Serialize(message, serializerOptions);
                }

                if (partitionValue is not null && storageConnector.PartitionKeyFieldName is not null)
                {
                    throw new Exception("TODO - partitions on messages?");
                }

                var content = new ByteArrayContent(payload);

                // perform a POST request...
                response = await request.PostAsync(content);
            }

            responseData = await response.GetBytesAsync();
            var deserialized = _entitySerializerService.DeserializeEnumerable<QueueMessage>(responseData, serializerOptions).SingleOrDefault()
                ?? throw new Exception("Null response encountered on message save.");

            if (entity is QueueMessage messageEntity)
            {
                if (deserialized is not TEntity castEntity)
                    throw new Exception("The deserialized entity is not of the expected type.");

                // note, message queue does not return data in response, so copy it over...
                var messageData = messageEntity.MessageData;
                deserialized.MessageData = messageData;
                entity = castEntity;
            }

            var saveResponse = new SaveEntityCommandResponse<TEntity>(true, entity);

            return saveResponse;
        }

        #endregion

    }

}
