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

using Microsoft.Extensions.DependencyInjection;
using Sidub.Platform.Authentication;
using Sidub.Platform.Core.Services;
using Sidub.Platform.Storage.Http.Test.Models.TripPin;
using Sidub.Platform.Storage.Services;

namespace Sidub.Platform.Storage.Http.Test
{
    [TestClass]
    public class ODataStorageTest
    {

        private readonly IServiceRegistry _entityMetadataService;
        private readonly IQueryService _queryService;

        private readonly StorageServiceReference _tripPinApi = new("TripPinApi");

        public ODataStorageTest()
        {
            // initialize dependency injection environment...
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSidubAuthenticationForHttp();
            serviceCollection.AddSidubStorageForHttp();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _entityMetadataService = serviceProvider.GetService<IServiceRegistry>()
                ?? throw new Exception("Entity serviceConnector service not initialized.");
            _queryService = serviceProvider.GetService<IQueryService>()
                ?? throw new Exception("Query service not initialized.");

            var session = Guid.NewGuid().ToString("N");
            var serviceConnector = new ODataStorageConnector() { ServiceUri = $"https://services.odata.org/TripPinRESTierService/(S({session}))/" };

            _entityMetadataService.RegisterServiceReference(_tripPinApi, serviceConnector);
        }

        [TestMethod]
        public async Task GetPersonTest()
        {
            var query = new PersonByUsernameQuery("laurelosborn");
            var result = await _queryService.Execute(_tripPinApi, query);

            Assert.IsNotNull(result);
            AssertPerson(result);
        }

        [TestMethod]
        public async Task GetPeopleTest()
        {
            var query = new PeopleByGenderQuery(Gender.Female);
            var result = _queryService.Execute(_tripPinApi, query);
            var results = new List<Person>();
            Person? match = null;

            await foreach (var person in result)
            {
                Assert.IsNotNull(person);
                results.Add(person);

                if (person.Username == "laurelosborn")
                {
                    match = person;
                }
            }

            Assert.IsNotNull(match);
            AssertPerson(match);
        }

        [TestMethod]
        public async Task GetPeoplePaginatedTest()
        {
            var parameters = new QueryParameters()
            {
                Top = 2
            };
            var query = new PeopleByGenderQuery(Gender.Female);
            var result = _queryService.Execute(_tripPinApi, query, parameters);
            var results = new List<Person>();
            Person? match = null;

            await foreach (var person in result)
            {
                Assert.IsNotNull(person);
                results.Add(person);

                if (person.Username == "laurelosborn")
                {
                    match = person;
                }
            }

            Assert.IsNotNull(match);
            AssertPerson(match);
        }

        private void AssertPerson(Person person)
        {
            switch (person.Username)
            {
                case "laurelosborn":
                    Assert.AreEqual("laurelosborn", person.Username);
                    Assert.AreEqual("Laurel", person.FirstName);
                    Assert.AreEqual(null, person.MiddleName);
                    Assert.AreEqual("Osborn", person.LastName);
                    Assert.AreEqual(Gender.Female, person.Gender);
                    break;
            }
        }

    }

}
