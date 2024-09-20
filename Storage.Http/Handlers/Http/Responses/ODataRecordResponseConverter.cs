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
    /// Converts record OData responses to JSON and vice versa.
    /// </summary>
    internal class ODataRecordResponseConverter : JsonConverterFactory
    {

        #region Public methods

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            var result = typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ODataRecordResponse<>);

            return result;
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

        /// <summary>
        /// Converts ODataRecordResponse to JSON and vice versa.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        private class Converter<TEntity> : JsonConverter<ODataRecordResponse<TEntity>> where TEntity : IEntity
        {

            #region Public methods

            /// <inheritdoc/>
            public override ODataRecordResponse<TEntity>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                ODataRecordResponse<TEntity>? result = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return result;
                    else if (result is null)
                        result = new ODataRecordResponse<TEntity>();

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "@odata.context":
                                result.ODataContext = reader.GetString();
                                break;
                            case "value":
                                var enumerableResponse = false;
                                var emptyEnumerable = false;
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    enumerableResponse = true;
                                    reader.Read();

                                    if (reader.TokenType == JsonTokenType.EndArray)
                                        emptyEnumerable = true;
                                }

                                if (!(enumerableResponse && emptyEnumerable))
                                    result.Value = JsonSerializer.Deserialize<TEntity>(ref reader, options);

                                if (enumerableResponse)
                                {
                                    if (!emptyEnumerable)
                                        reader.Read();

                                    if (reader.TokenType != JsonTokenType.EndArray)
                                        throw new Exception("More than one result was returned by a record query.");
                                }

                                break;
                        }
                    }
                }

                return result;
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, ODataRecordResponse<TEntity> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("@odata.context", value.ODataContext);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, value.Value, options);

                writer.WriteEndObject();
            }

            #endregion

        }

    }

}
