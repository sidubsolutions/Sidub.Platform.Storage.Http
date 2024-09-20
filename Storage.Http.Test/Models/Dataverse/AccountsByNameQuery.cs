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

using Sidub.Platform.Filter;
using Sidub.Platform.Storage.Queries;

namespace Sidub.Platform.Storage.Http.Test.Models.Dataverse
{
    public class AccountsByNameQuery : IEnumerableQuery<Account>
    {

        public string[] Names { get; set; }

        public AccountsByNameQuery(params string[] names)
        {
            Names = names;
        }

        public IFilter GetFilter()
        {
            var filterBuilder = new FilterBuilder();
            var isFirst = true;

            foreach (var name in Names)
            {
                if (!isFirst)
                    filterBuilder.Add(LogicalOperator.Or);

                filterBuilder.Add("name", ComparisonOperator.Equals, name);

                isFirst = false;
            }

            return filterBuilder.Build()
                ?? throw new Exception("Filter builder produced undefined filter.");
        }
    }
}
