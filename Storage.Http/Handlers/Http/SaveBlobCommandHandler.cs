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
    /// Handles the command to save a blob for a specific entity using OData storage.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class SaveBlobCommandHandler<TEntity> : ICommandHandler<SaveEntityCommand<TEntity>, SaveEntityCommandResponse<TEntity>> where TEntity : IEntity
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
        /// Initializes a new instance of the <see cref="SaveBlobCommandHandler{TEntity}"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public SaveBlobCommandHandler(IServiceRegistry metadataService, IDataProviderService<IFlurlClient> dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
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
        /// Executes the save blob command.
        /// </summary>
        /// <param name="ServiceReference">The storage service reference.</param>
        /// <param name="command">The save blob command.</param>
        /// <param name="queryService">The query service.</param>
        /// <returns>The save blob command response.</returns>
        public async Task<SaveEntityCommandResponse<TEntity>> Execute(StorageServiceReference ServiceReference, SaveEntityCommand<TEntity> command, IQueryService queryService)
        {
            string responseString;

            // open a data client for the given ServiceReference...
            var client = _dataProviderService.GetDataClient(ServiceReference);

            var serializerOptions = SerializerOptions.Default(SerializationLanguageType.Json);
            var keyValues = EntityTypeHelper.GetEntityKeyValues(command.Entity);
            var keyPath = BlobEntityTypeHelper.GetKeyPathFromEntityKeys<TEntity>(keyValues);
            var request = client.Request(keyPath.ToArray());

            IFlurlResponse response;

            // perform a PATCH request...
            var data = _entitySerializerService.Serialize(command.Entity, serializerOptions);
            var content = new ByteArrayContent(data);

            response = await request.PutAsync(content);
            responseString = await response.GetStringAsync();

            // TODO - response handling...
            var saveResponse = new SaveEntityCommandResponse<TEntity>(true, command.Entity);

            return saveResponse;
        }

        #endregion

    }

}
