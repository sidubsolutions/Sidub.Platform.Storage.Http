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
using Sidub.Platform.Filter.Parsers.OData;
using Sidub.Platform.Filter.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Commands.Responses;
using Sidub.Platform.Storage.Handlers;
using Sidub.Platform.Storage.Handlers.Http;


#endregion

namespace Sidub.Platform.Storage.Services.Http
{

    /// <summary>
    /// Service for handling data operations using OData.
    /// </summary>
    public class BlobDataHandlerService : DataHandlerServiceBase<BlobStorageConnector>
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDataHandlerService"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="provider">The OData data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        /// <param name="commandHandlers">The command handlers.</param>
        /// <param name="queryHandlers">The query handlers.</param>
        public BlobDataHandlerService(
            IServiceRegistry metadataService,
            IFilterService<ODataFilterConfiguration> filterService,
            ODataDataProviderService provider,
            IEntitySerializerService entitySerializerService,
            IEntityPartitionService entityPartitionService,
            IEnumerable<ICommandHandlerFactory<BlobStorageConnector>> commandHandlers,
            IEnumerable<IQueryHandlerFactory<BlobStorageConnector>> queryHandlers)
            : base(metadataService, filterService, provider, entitySerializerService, entityPartitionService, commandHandlers, queryHandlers)
        {

        }

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the save command handler for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The save command handler.</returns>
        public override ICommandHandler<SaveEntityCommand<TEntity>, SaveEntityCommandResponse<TEntity>> GetSaveCommandHandler<TEntity>(IQueryService queryService)
        {
            return new SaveBlobCommandHandler<TEntity>(_metadataService, _dataProviderService, _entitySerializerService, _filterService, _entityPartitionService);
        }

        #endregion

    }

}
