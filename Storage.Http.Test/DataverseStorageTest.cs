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
using Sidub.Platform.Core.Services;
using Sidub.Platform.Storage.Commands;
using Sidub.Platform.Storage.Commands.Responses;
using Sidub.Platform.Storage.Http.Test.Models.Dataverse;
using Sidub.Platform.Storage.Services;

namespace Sidub.Platform.Storage.Http.Test
{
    [TestClass]
    public class DataverseStorageTest
    {

        private readonly IServiceRegistry _entityMetadataService;
        private readonly IQueryService _queryService;

        private StorageServiceReference StorageReference { get; }

        public DataverseStorageTest()
        {
            // initialize dependency injection environment...
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSidubAuthenticationForHttp();
            serviceCollection.AddSidubStorageForHttp();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var crmUrl = Environment.GetEnvironmentVariable("CRM_URL");

            if (string.IsNullOrEmpty(crmUrl))
                throw new Exception("CRM URL not provided.");

            _entityMetadataService = serviceProvider.GetService<IServiceRegistry>() ?? throw new Exception("Entity metadata service not initialized.");
            _queryService = serviceProvider.GetService<IQueryService>() ?? throw new Exception("Query service not initialized.");

            StorageReference = new StorageServiceReference("UnitTests");
            var metadata = new ODataStorageConnector() { ServiceUri = $"{crmUrl}/api/data/v9.2/" };

            var authModule = new AuthenticationServiceReference("UnitTestsAuth");
            var authMetadata = new ServiceTokenCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions() { ExcludeSharedTokenCacheCredential = true }), "https://sidubsandbox.crm3.dynamics.com/.default");
            _entityMetadataService.RegisterServiceReference(StorageReference, metadata);
            _entityMetadataService.RegisterServiceReference(authModule, authMetadata, StorageReference);

        }

        private async Task<Account> AddContactsToAccount(Account account)
        {
            if (account.Contacts.Count == 0)
            {
                var contact1Query = new ContactByNameQuery("Jim", "Glynn (sample)");
                var contact2Query = new ContactByNameQuery("Nancy", "Anderson (sample)");

                var contact1 = await _queryService.Execute(StorageReference, contact1Query);
                var contact2 = await _queryService.Execute(StorageReference, contact2Query);

                Assert.IsNotNull(contact1);
                Assert.IsNotNull(contact2);

                account.Contacts.Add(contact1);
                account.Contacts.Add(contact2);

                var saveCommand = new SaveEntityCommand<Account>(account);
                var saveResult = await _queryService.Execute(StorageReference, saveCommand);

                Assert.IsNotNull(saveResult);
                Assert.IsTrue(saveResult.IsSuccessful);

                var accountQuery = new AccountByNameQuery(account.Name);
                account = await _queryService.Execute(StorageReference, accountQuery)
                    ?? throw new Exception("Null account retrieved - unexpeceted error.");
            }
            else if (account.Contacts.Count == 1)
            {
                var existingContact = await account.Contacts[0].Get();

                ContactByNameQuery contactQuery;

                if (existingContact.FirstName == "Jim")
                {
                    contactQuery = new ContactByNameQuery("Nancy", "Anderson (sample)");
                }
                else
                {
                    contactQuery = new ContactByNameQuery("Jim", "Glynn (sample)");
                }

                var contact = await _queryService.Execute(StorageReference, contactQuery);

                Assert.IsNotNull(contact);

                account.Contacts.Add(contact);

                var saveCommand = new SaveEntityCommand<Account>(account);
                var saveResult = await _queryService.Execute(StorageReference, saveCommand);

                Assert.IsNotNull(saveResult);
                Assert.IsTrue(saveResult.IsSuccessful);

                var accountQuery = new AccountByNameQuery(account.Name);
                account = await _queryService.Execute(StorageReference, accountQuery)
                    ?? throw new Exception("Null account retrieved - unexpeceted error.");
            }

            return account;
        }

        private async Task<Account> AddParentToAccount(Account account, string parentName)
        {
            if (!account.ParentAccount.HasValue())
            {
                var parentQuery = new AccountByNameQuery(parentName);
                var parent = await _queryService.Execute(StorageReference, parentQuery);

                account.ParentAccount.Set(parent);

                var saveAccount = new SaveEntityCommand<Account>(account);
                var saveResult = await _queryService.Execute(StorageReference, saveAccount);

                Assert.IsNotNull(saveResult);
                Assert.IsTrue(saveResult.IsSuccessful);

                var accountQuery = new AccountByNameQuery(account.Name);
                account = await _queryService.Execute(StorageReference, accountQuery)
                    ?? throw new Exception("Null account retrieved - unexpeceted error.");
            }

            return account;
        }

        [TestMethod]
        public async Task RecordQueryWithRelationsTest()
        {
            var query = new AccountByNameQuery("Blue Yonder Airlines (sample)");
            var account = await _queryService.Execute(StorageReference, query)
                ?? throw new Exception("No record returned by record query.");

            await AddContactsToAccount(account);
            await AddParentToAccount(account, "Litware, Inc. (sample)");

            Assert.AreEqual("Blue Yonder Airlines (sample)", account.Name);
            Assert.IsNotNull(account.Contacts);
            Assert.AreEqual(2, account.Contacts.Count);

            var contacts = account.Contacts;
            var contactOne = await contacts[0].Get();
            var contactTwo = await contacts[1].Get();

            Assert.IsNotNull(contactOne);
            Assert.IsNotNull(contactTwo);
            Assert.IsFalse(string.IsNullOrEmpty(contactOne.Name));
            Assert.IsFalse(string.IsNullOrEmpty(contactTwo.Name));

            Assert.IsNotNull(account.ParentAccount);
            var parentAccount = await account.ParentAccount.Get();

            Assert.IsNotNull(parentAccount);
            Assert.AreEqual("Litware, Inc. (sample)", parentAccount.Name);

            Assert.IsNotNull(parentAccount.Contacts);
            Assert.AreEqual(0, parentAccount.Contacts.Count);
        }

        [TestMethod]
        public async Task RecordQueryWithRelationsTest02()
        {
            var query = new AccountByNameQuery("Litware, Inc. (sample)");
            var account = await _queryService.Execute(StorageReference, query)
                ?? throw new Exception("No record returned by record query.");

            Assert.IsNotNull(account);
            Assert.AreEqual("Litware, Inc. (sample)", account.Name);

            Assert.IsNotNull(account.Contacts);
            Assert.AreEqual(0, account.Contacts.Count);
        }

        [TestMethod]
        public async Task RemoveEnumerableRelationTest()
        {
            var query = new AccountByNameQuery("Blue Yonder Airlines (sample)");
            var account = await _queryService.Execute(StorageReference, query)
                ?? throw new Exception("No record returned by record query.");

            await AddContactsToAccount(account);
            await AddParentToAccount(account, "Litware, Inc. (sample)");

            Assert.AreEqual("Blue Yonder Airlines (sample)", account.Name);
            Assert.IsNotNull(account.Contacts);
            Assert.AreEqual(2, account.Contacts.Count);

            var contacts = account.Contacts;
            var contactOne = await contacts[0].Get();
            var contactTwo = await contacts[1].Get();

            Assert.IsNotNull(contactOne);
            Assert.IsNotNull(contactTwo);
            Assert.IsFalse(string.IsNullOrEmpty(contactOne.Name));
            Assert.IsFalse(string.IsNullOrEmpty(contactTwo.Name));

            account.Contacts.Remove(account.Contacts[1]);
            var saveCommand = new SaveEntityCommand<Account>(account);
            var saveResult = await _queryService.Execute(StorageReference, saveCommand);

            Assert.IsTrue(saveResult.IsSuccessful);

            account = await _queryService.Execute(StorageReference, query)
                ?? throw new Exception("No record returned by record query.");

            Assert.AreEqual(1, account.Contacts.Count);

            account.Contacts.Remove(account.Contacts[0]);
            saveCommand = new SaveEntityCommand<Account>(account);
            saveResult = await _queryService.Execute(StorageReference, saveCommand);

            Assert.IsTrue(saveResult.IsSuccessful);

            account = await _queryService.Execute(StorageReference, query)
                ?? throw new Exception("No record returned by record query.");

            Assert.AreEqual(0, account.Contacts.Count);
        }

        [TestMethod]
        public async Task EnumerableQueryWithRelationsTest()
        {
            var query = new AccountsByNameQuery("Blue Yonder Airlines (sample)", "Alpine Ski House (sample)");
            var accounts = _queryService.Execute(StorageReference, query);

            await foreach (var account in accounts)
            {
                if (account.Name == "Blue Yonder Airlines (sample)")
                {
                    await AddContactsToAccount(account);
                    await AddParentToAccount(account, "Litware, Inc. (sample)");

                    Assert.AreEqual("Blue Yonder Airlines (sample)", account.Name);
                    Assert.IsNotNull(account.Contacts);
                    Assert.AreEqual(2, account.Contacts.Count);

                    var contacts = account.Contacts;
                    var contactOne = await contacts[0].Get();
                    var contactTwo = await contacts[1].Get();

                    Assert.IsNotNull(contactOne);
                    Assert.IsNotNull(contactTwo);
                    Assert.IsFalse(string.IsNullOrEmpty(contactOne.Name));
                    Assert.IsFalse(string.IsNullOrEmpty(contactTwo.Name));

                    Assert.IsNotNull(account.ParentAccount);
                    var parentAccount = await account.ParentAccount.Get();

                    Assert.IsNotNull(parentAccount);
                    Assert.AreEqual("Litware, Inc. (sample)", parentAccount.Name);

                    Assert.IsNotNull(parentAccount.Contacts);
                    Assert.AreEqual(0, parentAccount.Contacts.Count);
                }
                else if (account.Name == "Alpine Ski House (sample)")
                {
                    await AddParentToAccount(account, "Coho Winery (sample)");

                    Assert.AreEqual("Alpine Ski House (sample)", account.Name);
                    Assert.IsNotNull(account.Contacts);
                    Assert.AreEqual(0, account.Contacts.Count);

                    Assert.IsNotNull(account.ParentAccount);
                    var parentAccount = await account.ParentAccount.Get();

                    Assert.IsNotNull(parentAccount);
                    Assert.AreEqual("Coho Winery (sample)", parentAccount.Name);

                    Assert.IsNotNull(parentAccount.Contacts);
                    Assert.AreEqual(0, parentAccount.Contacts.Count);
                }
            }

        }

        [TestMethod]
        public async Task RemoveRecordRelationTest()
        {
            var parentQuery = new AccountByNameQuery("Litware, Inc. (sample)");
            var childQuery = new AccountByNameQuery("Adventure Works (sample)");

            var parentAccount = await _queryService.Execute(StorageReference, parentQuery)
                ?? throw new Exception("No record returned by record query.");
            var childAccount = await _queryService.Execute(StorageReference, childQuery)
                ?? throw new Exception("No record returned by record query.");

            Assert.AreEqual("Litware, Inc. (sample)", parentAccount.Name);
            Assert.AreEqual("Adventure Works (sample)", childAccount.Name);

            SaveEntityCommand<Account> save;
            SaveEntityCommandResponse<Account> result;

            if (childAccount.ParentAccount.HasValue())
            {
                childAccount.ParentAccount.Clear();

                save = new SaveEntityCommand<Account>(childAccount);
                result = await _queryService.Execute(StorageReference, save);

                Assert.IsTrue(result.IsSuccessful);

                childAccount = await _queryService.Execute(StorageReference, childQuery)
                    ?? throw new Exception("No record returned by record query.");

                Assert.IsFalse(childAccount.ParentAccount.HasValue());
            }

            childAccount.ParentAccount.Set(parentAccount);

            save = new SaveEntityCommand<Account>(childAccount);
            result = await _queryService.Execute(StorageReference, save);

            Assert.IsTrue(result.IsSuccessful);

            childAccount = await _queryService.Execute(StorageReference, childQuery)
                ?? throw new Exception("No record returned by record query.");

            Assert.IsTrue(childAccount.ParentAccount.HasValue());

            var parentAccountViaChild = await childAccount.ParentAccount.Get();

            Assert.IsNotNull(parentAccountViaChild);
            Assert.AreEqual("Litware, Inc. (sample)", parentAccountViaChild.Name);


        }

    }
}
