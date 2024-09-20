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

using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sidub.Platform.Authentication;
using Sidub.Platform.Authentication.Credentials;
using Sidub.Platform.Core;
using Sidub.Platform.Core.Serializers;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Entities;
using Sidub.Platform.Storage.Http.Test.Models;
using Sidub.Platform.Storage.Services;

namespace Sidub.Platform.Storage.Http.Test
{
    [TestClass]
    public class AzureStorageTest
    {

        private readonly IServiceRegistry _entityMetadataService;
        private readonly IQueryService _queryService;
        private readonly IEntitySerializerService _serializerService;

        private StorageServiceReference TableService { get; } = new StorageServiceReference("TableApi");
        private StorageServiceReference BlobService { get; } = new StorageServiceReference("BlobApi");
        private StorageServiceReference QueueService { get; } = new StorageServiceReference("QueueApi");

        public AzureStorageTest()
        {
            // initialize dependency injection environment...
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSidubPlatform(services =>
            {
                var serviceRegistry = new InMemoryServiceRegistry();

                var storageTableUrl = Environment.GetEnvironmentVariable("STORAGETABLE_URL");
                var storageQueueUrl = Environment.GetEnvironmentVariable("STORAGEQUEUE_URL");
                var storageBlobUrl = Environment.GetEnvironmentVariable("STORAGEBLOB_URL");

                if (string.IsNullOrEmpty(storageTableUrl))
                    throw new Exception("Storage table URL not provided.");

                if (string.IsNullOrEmpty(storageQueueUrl))
                    throw new Exception("Storage queue URL not provided.");

                if (string.IsNullOrEmpty(storageBlobUrl))
                    throw new Exception("Storage blob URL not provided.");

                var serviceCredential = new ServiceTokenCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions() { ExcludeSharedTokenCacheCredential = true }), "https://storage.azure.com/");
                var tableApi = new TableStorageConnector() { ServiceUri = storageTableUrl, PartitionKeyFieldName = "PartitionKey" };
                var tableAuthService = new AuthenticationServiceReference("TableApi.Auth");
                var blobApi = new BlobStorageConnector() { ServiceUri = storageBlobUrl + "/testcontainer", SerializationLanguage = SerializationLanguageType.Xml };
                var blobAuthService = new AuthenticationServiceReference("BlobApi.Auth");
                var queueApi = new QueueStorageConnector() { ServiceUri = storageQueueUrl + "/testqueue", SerializationLanguage = SerializationLanguageType.Xml };
                var queueAuthService = new AuthenticationServiceReference("QueueApi.Auth");

                serviceRegistry.RegisterServiceReference(TableService, tableApi);
                serviceRegistry.RegisterServiceReference(tableAuthService, serviceCredential, TableService);

                serviceRegistry.RegisterServiceReference(BlobService, blobApi);
                serviceRegistry.RegisterServiceReference(blobAuthService, serviceCredential, BlobService);

                serviceRegistry.RegisterServiceReference(QueueService, queueApi);
                serviceRegistry.RegisterServiceReference(queueAuthService, serviceCredential, QueueService);

                return serviceRegistry;
            });

            serviceCollection.AddSidubAuthenticationForHttp();
            serviceCollection.AddSidubStorageForHttp();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _entityMetadataService = serviceProvider.GetService<IServiceRegistry>() ?? throw new Exception("Entity metadata service not initialized.");
            _queryService = serviceProvider.GetService<IQueryService>() ?? throw new Exception("Query service not initialized.");
            _serializerService = serviceProvider.GetService<IEntitySerializerService>() ?? throw new Exception("Serializer service not initialized.");

        }


        [TestMethod]
        public async Task SaveBlobTest()
        {
            var category = new Category()
            {
                Id = "mykey",
                Name = "categoryname",
                IsRetrievedFromStorage = true
            };

            var saveCommand = new SaveEntityCommand<Category>(category);
            var saveResult = await _queryService.Execute(BlobService, saveCommand);
            var a = "A";

            var query = new CategoryByIdQuery("mykey");
            var queryResult = await _queryService.Execute(BlobService, query.AsBlobQuery()).ToListAsync();

            Assert.AreEqual(1, queryResult.Count);
            var resultReference = queryResult.Single();

            Assert.IsNotNull(resultReference);
            var result = await resultReference.Get();

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveQueueMessageTest()
        {
            var category = new QueueMessage("test message");

            var saveCommand = new SaveEntityCommand<QueueMessage>(category);
            var saveResult = await _queryService.Execute(QueueService, saveCommand);

            Assert.IsTrue(saveResult.IsSuccessful);
        }

        [TestMethod]
        public async Task SaveQueueEntityTest()
        {
            var category = new Category()
            {
                Id = "mykey",
                Name = "categoryname"
            };

            var saveCommand = new SaveEntityCommand<Category>(category);
            var saveResult = await _queryService.Execute(QueueService, saveCommand);

            Assert.IsTrue(saveResult.IsSuccessful);
        }

        [TestMethod]
        public async Task SaveTableEntityTest()
        {
            var category = new Category()
            {
                Id = Guid.NewGuid().ToString("d"),
                Name = "categoryname"
            };

            var saveCommand = new SaveEntityCommand<Category>(category);
            var saveResult = await _queryService.Execute(TableService, saveCommand);

            Assert.IsTrue(saveResult.IsSuccessful);
        }

    }
}
