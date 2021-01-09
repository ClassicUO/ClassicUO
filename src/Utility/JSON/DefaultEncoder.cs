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
using System.Reflection;

namespace TinyJson
{
    using Encoder = Action<object, JsonBuilder>;

    public static class DefaultEncoder
    {
        public static Encoder GenericEncoder()
        {
            return (obj, builder) =>
            {
                builder.AppendBeginObject();
                Type type = obj.GetType();
                bool matchSnakeCase = type.GetCustomAttribute<MatchSnakeCaseAttribute>(true) != null;
                bool first = true;

                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (properties.Length == 0)
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    foreach (FieldInfo fieldinfo in fields)
                    {
                        if (fieldinfo.GetCustomAttribute<JsonIgnore>(true) == null)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                builder.AppendSeperator();
                            }

                            string fieldName = fieldinfo.UnwrappedFieldName();

                            if (matchSnakeCase)
                            {
                                fieldName = fieldName.CamelCaseToSnakeCase();
                            }

                            JsonMapper.EncodeNameValue(fieldName, fieldinfo.GetValue(obj), builder);
                        }
                    }
                }
                else
                {
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.GetCustomAttribute<JsonIgnore>(true) == null)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                builder.AppendSeperator();
                            }

                            string fieldName = property.UnwrappedPropertyName();

                            if (matchSnakeCase)
                            {
                                fieldName = fieldName.CamelCaseToSnakeCase();
                            }

                            JsonMapper.EncodeNameValue(fieldName, property.GetValue(obj), builder);
                        }
                    }
                }


                builder.AppendEndObject();
            };
        }

        public static Encoder DictionaryEncoder()
        {
            return (obj, builder) =>
            {
                builder.AppendBeginObject();
                bool first = true;
                IDictionary dict = (IDictionary) obj;

                foreach (object key in dict.Keys)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.AppendSeperator();
                    }

                    JsonMapper.EncodeNameValue(key.ToString(), dict[key], builder);
                }

                builder.AppendEndObject();
            };
        }

        public static Encoder EnumerableEncoder()
        {
            return (obj, builder) =>
            {
                builder.AppendBeginArray();
                bool first = true;

                foreach (object item in (IEnumerable) obj)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.AppendSeperator();
                    }

                    JsonMapper.EncodeValue(item, builder);
                }

                builder.AppendEndArray();
            };
        }

        public static Encoder ZuluDateEncoder()
        {
            return (obj, builder) =>
            {
                DateTime date = (DateTime) obj;

                string zulu = date.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                builder.AppendString(zulu);
            };
        }
    }
}