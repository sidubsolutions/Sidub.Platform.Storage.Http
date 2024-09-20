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
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace Sidub.Platform.Storage.Handlers.Http.Responses
{

    /// <summary>
    /// Converts an enumerable OData response to JSON and vice versa.
    /// </summary>
    internal class ODataEnumerableResponseConverter : JsonConverterFactory
    {

        #region Public methods

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ODataEnumerableResponse<>);
        }

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type TEntity = typeToConvert.GetGenericArguments()[0];
            Type converterType = typeof(Converter<>).MakeGenericType(new[] { TEntity });
            var converter = Activator.CreateInstance(converterType);

            return (JsonConverter?)converter;
        }

        #endregion

        private class Converter<TEntity> : JsonConverter<ODataEnumerableResponse<TEntity>> where TEntity : class, IEntity, new()
        {

            #region Public methods

            /// <inheritdoc/>
            public override ODataEnumerableResponse<TEntity>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                ODataEnumerableResponse<TEntity>? result = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return result;
                    else if (result is null)
                        result = new ODataEnumerableResponse<TEntity>();

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "@odata.context":
                                result.ODataContext = reader.GetString();
                                break;
                            case "@odata.nextLink":
                                result.ODataContext = reader.GetString();
                                break;
                            case "value":
                                result.Value = JsonSerializer.Deserialize<IEnumerable<TEntity>>(ref reader, options) ?? Enumerable.Empty<TEntity>();
                                break;
                        }
                    }
                }

                return result;
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, ODataEnumerableResponse<TEntity> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("@odata.context", value.ODataContext);
                writer.WriteString("@odata.nextLink", value.ODataNextLink);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, value.Value, options);

                writer.WriteEndObject();
            }

            #endregion

        }

    }

}
