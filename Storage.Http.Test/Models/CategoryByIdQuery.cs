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

namespace Sidub.Platform.Storage.Http.Test.Models
{
    internal class CategoryByIdQuery : IRecordQuery<Category>, IBlobQuery<Category>
    {

        public string Id { get; }

        public CategoryByIdQuery(string id)
        {
            Id = id;
        }

        public IFilter GetFilter()
        {
            var filterBuilder = new FilterBuilder();
            filterBuilder.Add("RowKey", ComparisonOperator.Equals, Id);

            return filterBuilder.Build()
                ?? throw new Exception("Filter builder produced undefined filter.");
        }

        public IEnumerable<object> GetPathFilterSegments()
        {
            return new object[] { Id };
        }

        public IBlobQuery<Category> AsBlobQuery()
        {
            return this;
        }
    }
}
