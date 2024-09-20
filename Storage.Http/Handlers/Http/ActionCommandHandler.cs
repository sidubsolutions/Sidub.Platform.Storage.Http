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
using Sidub.Platform.Storage.Services;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Handles action commands for a specific entity type with no response.
    /// </summary>
    /// <typeparam name="TRequestEntity">The type of the request entity.</typeparam>
    public class ActionCommandHandler<TRequestEntity> : ICommandHandler<IActionCommand<TRequestEntity>, ActionCommandResponse>
        where TRequestEntity : IEntity
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
        /// Initializes a new instance of the <see cref="ActionCommandHandler{TRequestEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public ActionCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
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
        /// Executes the action command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The action command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The action command response.</returns>
        public async Task<ActionCommandResponse> Execute(StorageServiceReference ServiceReference, IActionCommand<TRequestEntity> command, IQueryService queryService)
        {
            var actionData = command.Parameters;
            var actionLabel = EntityTypeHelper.GetEntityName(actionData)
                ?? throw new Exception("Null entity name encountered.");

            // retrieve entity keys and find / replace and key values that exist in the label...
            var keys = EntityTypeHelper.GetEntityKeyValues(actionData);
            var keyMatches = keys.Where(x => actionLabel.Contains(x.Key.FieldName));

            foreach (var keyMatch in keyMatches)
            {
                // TODO - need to correctly serialize the key values being put into the URI...
                actionLabel = actionLabel.Replace($"{{{keyMatch.Key.FieldName}}}", keyMatch.Value.ToString());
            }

            ODataStorageConnector storageConnector = _metadataService.GetMetadata<ODataStorageConnector>(ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);
            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            var request = client.Request(actionLabel);

            // check partition - we've not yet considered their implication here...
            var partitionValue = _entityPartitionService.GetPartitionValue(actionData);

            if (!string.IsNullOrEmpty(partitionValue))
                throw new Exception("Partition in action data has not been considered.");

            // check relationships - we've not yet considered their implications here...
            var relations = EntityTypeHelper.GetEntityRelations<TRequestEntity>();

            if (relations.Any())
                throw new Exception("Relationships in action data has not been considered.");

            // serialize the payload...
            var payload = _entitySerializerService.Serialize(actionData, serializerOptions);

            var method = command.ActionCommand switch
            {
                ActionCommandType.Create => HttpMethod.Post,
                ActionCommandType.Read => HttpMethod.Get,
                ActionCommandType.Update => new HttpMethod("PATCH"),
                ActionCommandType.Upsert => HttpMethod.Put,
                _ => throw new Exception($"Unhandled action command type '{command.ActionCommand}' encountered.")
            };

            var content = new ByteArrayContent(payload);
            var response = await request.SendAsync(method, content);
            var responseBytes = await response.GetBytesAsync();

            var actionResponse = new ActionCommandResponse
            {
                IsSuccessful = response.ResponseMessage.IsSuccessStatusCode
            };

            return actionResponse;
        }

        #endregion

    }

    /// <summary>
    /// Handles action commands for a specific request entity type and response entity type.
    /// </summary>
    /// <typeparam name="TRequestEntity">The type of the request entity.</typeparam>
    /// <typeparam name="TResponseEntity">The type of the response entity.</typeparam>
    public class ActionCommandHandler<TRequestEntity, TResponseEntity> : ICommandHandler<IActionCommand<TRequestEntity, TResponseEntity>, ActionCommandResponse<TResponseEntity>>
        where TRequestEntity : IEntity
        where TResponseEntity : IEntity
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
        /// Initializes a new instance of the <see cref="ActionCommandHandler{TRequestEntity, TResponseEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public ActionCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
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
        /// Executes the action command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The action command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The action command response.</returns>
        public async Task<ActionCommandResponse<TResponseEntity>> Execute(StorageServiceReference ServiceReference, IActionCommand<TRequestEntity, TResponseEntity> command, IQueryService queryService)
        {
            var actionData = command.Parameters;
            var actionLabel = EntityTypeHelper.GetEntityName(actionData)
                ?? throw new Exception("Null entity name encountered.");

            // retrieve entity keys and find / replace and key values that exist in the label...
            var keys = EntityTypeHelper.GetEntityKeyValues(actionData);
            var keyMatches = keys.Where(x => actionLabel.Contains(x.Key.FieldName));

            foreach (var keyMatch in keyMatches)
            {
                // TODO - need to correctly serialize the key values being put into the URI...
                actionLabel = actionLabel.Replace($"{{{keyMatch.Key.FieldName}}}", keyMatch.Value.ToString());
            }

            ODataStorageConnector storageConnector = _metadataService.GetMetadata<ODataStorageConnector>(ServiceReference).SingleOrDefault()
                ?? throw new Exception("Storage connector not initialized.");

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);
            var serializerOptions = SerializerOptions.New(storageConnector.SerializationLanguage);
            var request = client.Request(actionLabel);

            // check partition - we've not yet considered their implication here...
            var partitionValue = _entityPartitionService.GetPartitionValue(actionData);

            if (!string.IsNullOrEmpty(partitionValue))
                throw new Exception("Partition in action data has not been considered.");

            // check relationships - we've not yet considered their implications here...
            var relations = EntityTypeHelper.GetEntityRelations<TRequestEntity>();

            if (relations.Any())
                throw new Exception("Relationships in action data has not been considered.");

            // serialize the payload...
            var payload = _entitySerializerService.Serialize(actionData, serializerOptions);

            var method = command.ActionCommand switch
            {
                ActionCommandType.Create => HttpMethod.Post,
                ActionCommandType.Read => HttpMethod.Get,
                ActionCommandType.Update => new HttpMethod("PATCH"),
                ActionCommandType.Upsert => HttpMethod.Put,
                _ => throw new Exception($"Unhandled action command type '{command.ActionCommand}' encountered.")
            };

            var content = new ByteArrayContent(payload);
            var response = await request.SendAsync(method, content);
            var responseBytes = await response.GetBytesAsync();

            TResponseEntity? responseEntity;

            if (responseBytes.Any())
                responseEntity = _entitySerializerService.Deserialize<TResponseEntity>(responseBytes, serializerOptions);
            else
                responseEntity = default;

            var actionResponse = new ActionCommandResponse<TResponseEntity>
            {
                IsSuccessful = response.ResponseMessage.IsSuccessStatusCode,
                Result = responseEntity
            };

            return actionResponse;
        }

        #endregion

    }

}
