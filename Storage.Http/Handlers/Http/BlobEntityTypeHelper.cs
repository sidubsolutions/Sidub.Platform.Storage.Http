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

using Sidub.Platform.Core;
using Sidub.Platform.Core.Entity;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http
{

    /// <summary>
    /// Helper class for working with blob entity types.
    /// </summary>
    internal static class BlobEntityTypeHelper
    {

        #region Internal methods

        /// <summary>
        /// Parses a key value to a key path value.
        /// </summary>
        /// <param name="value">The key value to parse.</param>
        /// <returns>The parsed key path value.</returns>
        internal static string ParseKeyValueToKeyPathValue(object value)
        {
            if (value is null)
                throw new ArgumentException("Unexpected null value provided.", nameof(value));

            if (value is string valueString)
                return valueString;
            else if (value is Guid valueGuid)
                return valueGuid.ToString("D");
            else
                return value.ToString()!;

        }

        /// <summary>
        /// Parses a key path value to a key value.
        /// </summary>
        /// <param name="type">The type of the key.</param>
        /// <param name="value">The key path value to parse.</param>
        /// <returns>The parsed key value.</returns>
        internal static object ParseKeyPathValueToKeyValue(Type type, string value)
        {
            if (type == typeof(string))
                return value;
            else if (type == typeof(Guid))
                return Guid.Parse(value);
            else
                return value;
        }

        /// <summary>
        /// Gets the key path from the entity keys.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entityKeys">The entity keys.</param>
        /// <returns>The key path.</returns>
        internal static IEnumerable<string> GetKeyPathFromEntityKeys<TEntity>(IDictionary<IEntityField, object> entityKeys) where TEntity : IEntity
        {
            var entityLabel = EntityTypeHelper.GetEntityName<TEntity>()
                ?? throw new Exception("Failed to get entity name.");

            var result = new List<string>() {
                    entityLabel
                };

            foreach (var i in entityKeys.OrderBy(x => x.Key.OrdinalPosition))
            {
                result.Add(ParseKeyValueToKeyPathValue(i.Value));
            }

            return result;
        }

        /// <summary>
        /// Gets the entity keys from the key path.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="keyPath">The key path.</param>
        /// <returns>The entity keys.</returns>
        internal static IDictionary<IEntityField, object> GetEntityKeysFromKeyPath<TEntity>(IEnumerable<string> keyPath) where TEntity : IEntity
        {
            var position = 1;
            var result = new Dictionary<IEntityField, object>();

            foreach (var keySegment in keyPath)
            {
                var key = EntityTypeHelper.GetEntityField<TEntity>(position)
                    ?? throw new Exception($"Failed to retrieve key at ordinal position '{position}' on entity type '{typeof(TEntity).FullName}'.");

                result.Add(key, ParseKeyPathValueToKeyValue(key.FieldType, keySegment));

                position++;
            }

            return result;
        }

        #endregion

    }

}
