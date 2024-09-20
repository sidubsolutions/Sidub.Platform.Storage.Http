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
using Sidub.Platform.Storage.Services.Http;


#endregion

namespace Sidub.Platform.Storage.Handlers.Http.Factories
{

    /// <summary>
    /// Factory class for creating instances of <see cref="SaveQueueMessageCommandHandler{TEntity}"/>.
    /// </summary>
    public class SaveQueueMessageCommandHandlerFactory : ICommandHandlerFactory<QueueStorageConnector>
    {

        #region Member variables

        private readonly IServiceRegistry _metadataService;
        private readonly ODataDataProviderService _dataProviderService;
        private readonly IEntitySerializerService _entitySerializerService;
        private readonly IFilterService<ODataFilterConfiguration> _filterService;
        private readonly IEntityPartitionService _entityPartitionService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveQueueMessageCommandHandlerFactory"/> class.
        /// </summary>
        /// <param name="metadataService">The metadata service.</param>
        /// <param name="dataProviderService">The data provider service.</param>
        /// <param name="entitySerializerService">The entity serializer service.</param>
        /// <param name="filterService">The filter service.</param>
        /// <param name="entityPartitionService">The entity partition service.</param>
        public SaveQueueMessageCommandHandlerFactory(IServiceRegistry metadataService, ODataDataProviderService dataProviderService, IEntitySerializerService entitySerializerService, IFilterService<ODataFilterConfiguration> filterService, IEntityPartitionService entityPartitionService)
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
        /// Determines whether the specified command and result types are handled by this factory.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <returns><c>true</c> if the specified command and result types are handled; otherwise, <c>false</c>.</returns>
        public bool IsHandled<TCommand, TResult>()
            where TCommand : ICommand<TResult>
            where TResult : ICommandResponse
        {
            var T = typeof(TCommand);

            // ensure command conforms to AzureSaveBlobCommand<TEntity> where TEntity : IEntity...
            if (T.IsGenericType && T.GetGenericTypeDefinition() == typeof(SaveEntityCommand<>) && typeof(IEntity).IsAssignableFrom(T.GenericTypeArguments.Single()))
                return true;

            return false;
        }

        /// <summary>
        /// Creates an instance of the command handler for the specified command and result types.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <returns>An instance of the command handler.</returns>
        /// <exception cref="Exception">Thrown when the specified command and result types are not handled.</exception>
        public ICommandHandler<TCommand, TResult> Create<TCommand, TResult>()
            where TCommand : ICommand<TResult>
            where TResult : ICommandResponse
        {
            if (!IsHandled<TCommand, TResult>())
                throw new Exception("Unhandled type.");

            // retrieve TEntity from AzureSaveBlobCommand<TEntity>... note prior validation has ensured this is of IEntity type...
            var entityType = typeof(TCommand).GenericTypeArguments.Single();

            var handlerType = typeof(SaveQueueMessageCommandHandler<>).MakeGenericType(new[] { entityType });
            var handlerParameters = new object[] { _metadataService, _dataProviderService, _entitySerializerService, _filterService, _entityPartitionService };
            var handler = Activator.CreateInstance(handlerType, handlerParameters);

            if (handler is not ICommandHandler<TCommand, TResult> handlerCast)
                throw new Exception("Failed to cast handler.");

            return handlerCast;
        }

        #endregion

    }

}
