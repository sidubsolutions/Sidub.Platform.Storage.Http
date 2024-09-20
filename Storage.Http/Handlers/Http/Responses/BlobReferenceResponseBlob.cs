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

using Sidub.Platform.Core.Attributes;
using Sidub.Platform.Core.Entity;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http.Responses
{

    /// <summary>
    /// Represents a response blob for a blob reference.
    /// </summary>
    [Entity("Blob")]
    public class BlobReferenceResponseBlob : IEntity
    {

        #region Public properties

        /// <summary>
        /// Gets or sets the path of the blob.
        /// </summary>
        [EntityKey<string>("Name")]
        public string Path { get; set; } = string.Empty;

        #endregion

        #region IEntity implementation

        /// <summary>
        /// Gets or sets a value indicating whether the entity is retrieved from storage.
        /// </summary>
        bool IEntity.IsRetrievedFromStorage { get; set; }

        #endregion

    }

}
