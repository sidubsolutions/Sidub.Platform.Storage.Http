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

using Sidub.Platform.Core.Entity;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Filter.Parsers.OData;
using Sidub.Platform.Filter.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Commands.Responses;
using Sidub.Platform.Storage.Connectors;
using Sidub.Platform.Storage.Handlers;
using Sidub.Platform.Storage.Handlers.Http;
using Sidub.Platform.Storage.Queries;

#endregion

namespace Sidub.Platform.Storage.Services.Http
{

    /// <summary>
    /// Service for handling data operations using OData.
    /// </summary>
    public abstract class DataHandlerServiceBase<TStorageConnector> : IDataHandlerService
        where TStorageConnector : IHttpStorageConnector
    {

        #region Member variables

        protected readonly IServiceRegistry _metadataService;
        protected readonly ODataDataProviderService _dataProviderService;
        protected readonly IFilterService<ODataFilterConfiguration> _filterService;
        protected readonly IEntitySerializerService _entitySerializerService;
        protected readonly IEntityPartitionService _entityPartitionService;
        protected readonly IReadOnlyList<ICommandHandlerFactory<TStorageConnector>> _commandHandlers;
        protected readonly IReadOnlyList<IQueryHandlerFactory<TStorageConnector>> _queryHandlers;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataDataHandlerService"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="provider">The OData data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        /// <param name="commandHandlers">The command handlers.</param>
        /// <param name="queryHandlers">The query handlers.</param>
        public DataHandlerServiceBase(
            IServiceRegistry metadataService,
            IFilterService<ODataFilterConfiguration> filterService,
            ODataDataProviderService provider,
            IEntitySerializerService entitySerializerService,
            IEntityPartitionService entityPartitionService,
            IEnumerable<ICommandHandlerFactory<TStorageConnector>> commandHandlers,
            IEnumerable<IQueryHandlerFactory<TStorageConnector>> queryHandlers)
        {
            _metadataService = metadataService;
            _filterService = filterService;
            _dataProviderService = provider;
            _entitySerializerService = entitySerializerService;
            _entityPartitionService = entityPartitionService;
            _commandHandlers = commandHandlers.ToList().AsReadOnly();
            _queryHandlers = queryHandlers.ToList().AsReadOnly();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the save command handler for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The save command handler.</returns>
        public virtual ICommandHandler<SaveEntityCommand<TEntity>, SaveEntityCommandResponse<TEntity>> GetSaveCommandHandler<TEntity>(IQueryService queryService)
            where TEntity : IEntity
        {
            var handler = new EntitySaveCommandHandler<TEntity, TStorageConnector>(_metadataService, _dataProviderService, _entitySerializerService, _filterService, _entityPartitionService);
            return handler;
        }

        /// <summary>
        /// Gets the entity query handler for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The entity query handler.</returns>
        public virtual IRecordQueryHandler<IRecordQuery<TEntity>, TEntity> GetEntityQueryHandler<TEntity>(IQueryService queryService)
            where TEntity : IEntity
        {
            var handler = new EntityQueryHandler<TEntity>(_metadataService, _dataProviderService, _entitySerializerService, _filterService);
            return handler;
        }

        /// <summary>
        /// Gets the entities query handler for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The entities query handler.</returns>
        public virtual IEnumerableQueryHandler<IEnumerableQuery<TEntity>, TEntity> GetEntitiesQueryHandler<TEntity>(IQueryService queryService)
            where TEntity : class, IEntity
        {
            var handler = new EntitiesQueryHandler<TEntity>(_metadataService, _dataProviderService, _entitySerializerService, _filterService);
            return handler;
        }

        /// <summary>
        /// Gets the query handler for the specified query type and response type.
        /// </summary>
        /// <typeparam name="TQuery">The type of the query.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The query handler.</returns>
        public virtual IQueryHandler<TQuery, TResponse> GetQueryHandler<TQuery, TResponse>(IQueryService queryService)
            where TQuery : IQuery<TResponse>
        {
            IQueryHandler<TQuery, TResponse>? result;

            var factory = _queryHandlers.SingleOrDefault(x => x.IsHandled<TQuery, TResponse>());
            result = factory?.Create<TQuery, TResponse>()
                ?? throw new Exception("Failed to create query handler from factory.");

            return result;
        }

        /// <summary>
        /// Gets the command handler for the specified command type and response type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="queryService">The query service.</param>
        /// <returns>The command handler.</returns>
        public virtual ICommandHandler<TCommand, TResponse>? GetCommandHandler<TCommand, TResponse>(IQueryService queryService)
            where TCommand : ICommand<TResponse>
            where TResponse : ICommandResponse
        {
            ICommandHandler<TCommand, TResponse>? result;

            var factory = _commandHandlers.SingleOrDefault(x => x.IsHandled<TCommand, TResponse>());
            result = factory?.Create<TCommand, TResponse>();

            return result;
        }

        public virtual bool IsHandled(IStorageConnector connector)
        {
            return connector is TStorageConnector;
        }

        #endregion

    }

}
