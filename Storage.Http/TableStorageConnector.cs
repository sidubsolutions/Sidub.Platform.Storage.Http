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

using Sidub.Platform.Core.Serializers;

#endregion

namespace Sidub.Platform.Storage
{

    /// <summary>
    /// Represents a connector for queue storage.
    /// </summary>
    public class TableStorageConnector : ODataStorageConnector
    {

        #region Private methods

        /// <summary>
        /// Initializes the storage connector with default values.
        /// </summary>
        protected override void Initialize()
        {
            SerializationLanguage = SerializationLanguageType.Json;
            RequestHeaders.Add("Content-Type", "application/json");
            RequestHeaders.Add("x-ms-version", "2017-11-09");
            RequestHeaders.Add("Accept", "application/json");
        }

        #endregion

    }

}
