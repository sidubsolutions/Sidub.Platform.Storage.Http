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

#endregion

namespace Sidub.Platform.Storage.Handlers.Http.Responses
{

    /// <summary>
    /// Represents a response containing a single OData record.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    internal class ODataRecordResponse<TEntity> : IEntity where TEntity : IEntity
    {

        #region Internal properties

        /// <summary>
        /// Gets or sets the OData context.
        /// </summary>
        internal string? ODataContext { get; set; }

        /// <summary>
        /// Gets or sets the value of the OData record.
        /// </summary>
        internal TEntity? Value { get; set; }

        #endregion

        #region IEntity implementation

        /// <summary>
        /// Gets or sets a value indicating whether the entity is retrieved from storage.
        /// </summary>
        bool IEntity.IsRetrievedFromStorage { get => false; set => throw new Exception("Setting retrieved from storage flag not allowed."); }

        #endregion

    }

}
