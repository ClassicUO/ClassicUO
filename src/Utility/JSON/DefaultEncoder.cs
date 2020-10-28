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

                PropertyInfo[] properties = type.GetProperties
                    (BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (properties.Length == 0)
                {
                    FieldInfo[] fields = type.GetFields
                        (BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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