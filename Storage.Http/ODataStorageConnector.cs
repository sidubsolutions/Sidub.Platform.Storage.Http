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
    /// Represents a connector for OData storage.
    /// </summary>
    public class ODataStorageConnector : IHttpStorageConnector
    {

        #region Public properties

        /// <summary>
        /// Gets or sets the service URI of the OData storage.
        /// </summary>
        public string ServiceUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the partition key field name.
        /// </summary>
        public string? PartitionKeyFieldName { get; set; } = null;

        /// <summary>
        /// Gets the request headers for the OData storage.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether the data is retrieved from the storage.
        /// </summary>
        public bool IsRetrievedFromStorage { get; set; }

        /// <summary>
        /// Gets or sets the serialization language type.
        /// </summary>
        public SerializationLanguageType SerializationLanguage { get; set; } = SerializationLanguageType.Json;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataStorageConnector"/> class.
        /// </summary>
        public ODataStorageConnector()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataStorageConnector"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The service URI of the OData storage.</param>
        public ODataStorageConnector(string serviceUri)
        {
            Initialize();

            ServiceUri = serviceUri;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Initializes the OData storage connector with default values.
        /// </summary>
        protected virtual void Initialize()
        {
            SerializationLanguage = SerializationLanguageType.Json;
            RequestHeaders.Add("Content-Type", "application/json");
        }

        #endregion

    }

}
