#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace TinyJson
{
    using Encoder = Action<object, JsonBuilder>;
    using Decoder = Func<Type, object, object>;

    public static class JsonMapper
    {
        internal static Encoder genericEncoder;
        internal static Decoder genericDecoder;
        internal static IDictionary<Type, Encoder> encoders = new Dictionary<Type, Encoder>();
        internal static IDictionary<Type, Decoder> decoders = new Dictionary<Type, Decoder>();

        static JsonMapper()
        {
            // Register default encoder
            RegisterEncoder(typeof(object), DefaultEncoder.GenericEncoder());
            RegisterEncoder(typeof(IDictionary), DefaultEncoder.DictionaryEncoder());
            RegisterEncoder(typeof(IEnumerable), DefaultEncoder.EnumerableEncoder());
            RegisterEncoder(typeof(DateTime), DefaultEncoder.ZuluDateEncoder());

            // Register default decoder
            RegisterDecoder(typeof(object), DefaultDecoder.GenericDecoder());
            RegisterDecoder(typeof(IDictionary), DefaultDecoder.DictionaryDecoder());
            RegisterDecoder(typeof(Array), DefaultDecoder.ArrayDecoder());
            RegisterDecoder(typeof(IList), DefaultDecoder.ListDecoder());
            RegisterDecoder(typeof(ICollection), DefaultDecoder.CollectionDecoder());
            RegisterDecoder(typeof(IEnumerable), DefaultDecoder.EnumerableDecoder());
        }

        public static void RegisterDecoder(Type type, Decoder decoder)
        {
            if (type == typeof(object))
            {
                genericDecoder = decoder;
            }
            else
            {
                decoders.Add(type, decoder);
            }
        }

        public static void RegisterEncoder(Type type, Encoder encoder)
        {
            if (type == typeof(object))
            {
                genericEncoder = encoder;
            }
            else
            {
                encoders.Add(type, encoder);
            }
        }

        public static Decoder GetDecoder(Type type)
        {
            if (decoders.ContainsKey(type))
            {
                return decoders[type];
            }

            foreach (KeyValuePair<Type, Decoder> entry in decoders)
            {
                Type baseType = entry.Key;

                if (baseType.IsAssignableFrom(type))
                {
                    return entry.Value;
                }

                if (baseType.HasGenericInterface(type))
                {
                    return entry.Value;
                }
            }

            return genericDecoder;
        }

        public static Encoder GetEncoder(Type type)
        {
            if (encoders.ContainsKey(type))
            {
                return encoders[type];
            }

            foreach (KeyValuePair<Type, Encoder> entry in encoders)
            {
                Type baseType = entry.Key;

                if (baseType.IsAssignableFrom(type))
                {
                    return entry.Value;
                }

                if (baseType.HasGenericInterface(type))
                {
                    return entry.Value;
                }
            }

            return genericEncoder;
        }

        public static T DecodeJsonObject<T>(object jsonObj)
        {
            Decoder decoder = GetDecoder(typeof(T));

            return (T) decoder(typeof(T), jsonObj);
        }

        public static void EncodeValue(object value, JsonBuilder builder)
        {
            if (JsonBuilder.IsSupported(value))
            {
                builder.AppendValue(value);
            }
            else
            {
                Encoder encoder = GetEncoder(value.GetType());

                if (encoder != null)
                {
                    encoder(value, builder);
                }
                else
                {
                    Console.WriteLine("Encoder for " + value.GetType() + " not found");
                }
            }
        }

        public static void EncodeNameValue(string name, object value, JsonBuilder builder)
        {
            builder.AppendName(name);
            EncodeValue(value, builder);
        }

        private static object ConvertValue(object value, Type type)
        {
            if (value != null)
            {
                Type safeType = Nullable.GetUnderlyingType(type) ?? type;

                if (!type.IsEnum)
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(safeType);

                    if (converter.CanConvertFrom(value.GetType()))
                    {
                        return converter.ConvertFrom(value);
                    }

                    return Convert.ChangeType(value, safeType);
                }

                if (value is string)
                {
                    return Enum.Parse(type, (string) value);
                }

                return Enum.ToObject(type, value);
            }

            return value;
        }

        public static object DecodeValue(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (JsonBuilder.IsSupported(value))
            {
                value = ConvertValue(value, targetType);
            }

            // use a registered decoder
            if (value != null && !targetType.IsInstanceOfType(value))
            {
                Decoder decoder = GetDecoder(targetType);
                value = decoder(targetType, value);
            }

            if (value != null && targetType.IsInstanceOfType(value))
            {
                return value;
            }

            Console.WriteLine("Couldn't decode: " + targetType);

            return null;
        }

        public static bool DecodeValue(object target, string name, object value, PropertyInfo[] properties, bool matchSnakeCase)
        {
            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttribute<JsonIgnore>(true) == null)
                {
                    string propname = property.UnwrappedPropertyName();

                    if (matchSnakeCase)
                    {
                        propname = propname.SnakeCaseToCamelCase();
                        name = name.SnakeCaseToCamelCase();
                    }

                    if (propname.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        if (value == null)
                        {
                            property.SetValue(target, null);

                            return true;
                        }


                        object decodedValue = DecodeValue(value, targetType);

                        if (decodedValue != null && targetType.IsInstanceOfType(decodedValue))
                        {
                            property.SetValue(target, decodedValue);

                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        public static bool DecodeValue(object target, string name, object value, FieldInfo[] fields, bool matchSnakeCase)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttribute<JsonIgnore>(true) == null)
                {
                    string propname = field.UnwrappedFieldName();

                    if (matchSnakeCase)
                    {
                        propname = propname.SnakeCaseToCamelCase();
                        name = name.SnakeCaseToCamelCase();
                    }

                    if (propname.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                        if (value == null)
                        {
                            field.SetValue(target, null);

                            return true;
                        }

                        object decodedValue = DecodeValue(value, targetType);

                        if (decodedValue != null && targetType.IsInstanceOfType(decodedValue))
                        {
                            field.SetValue(target, decodedValue);

                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }
    }
}