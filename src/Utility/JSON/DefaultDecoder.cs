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
using System.Reflection;

namespace TinyJson
{
    using Decoder = Func<Type, object, object>;

    public static class DefaultDecoder
    {
        public static Decoder GenericDecoder()
        {
            return (type, jsonObj) =>
            {
                object instance = Activator.CreateInstance(type, true);

                if (jsonObj is IDictionary)
                {
                    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    bool match_snake_case = type.GetCustomAttribute<MatchSnakeCaseAttribute>() != null;

                    if (properties.Length == 0)
                    {
                        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                        foreach (DictionaryEntry item in (IDictionary) jsonObj)
                        {
                            string name = (string) item.Key;
                            object value = item.Value;

                            if (!JsonMapper.DecodeValue
                            (
                                instance,
                                name,
                                value,
                                fields,
                                match_snake_case
                            ))
                            {
                                Console.WriteLine("Couldn't decode field \"" + name + "\" of " + type);
                            }
                        }
                    }
                    else
                    {
                        foreach (DictionaryEntry item in (IDictionary) jsonObj)
                        {
                            string name = (string) item.Key;
                            object value = item.Value;

                            if (!JsonMapper.DecodeValue
                            (
                                instance,
                                name,
                                value,
                                properties,
                                match_snake_case
                            ))
                            {
                                Console.WriteLine("Couldn't decode field \"" + name + "\" of " + type);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Unsupported json type: " + (jsonObj != null ? jsonObj.GetType().ToString() : "null"));
                }

                return instance;
            };
        }

        public static Decoder DictionaryDecoder()
        {
            return (type, jsonObj) =>
            {
                Console.WriteLine("Decode Dictionary");

                // Dictionary
                if (jsonObj is IDictionary<string, object>)
                {
                    Dictionary<string, object> jsonDict = (Dictionary<string, object>) jsonObj;

                    if (type.GetGenericArguments().Length == 2)
                    {
                        IDictionary instance = null;
                        Type keyType = type.GetGenericArguments()[0];
                        Type genericType = type.GetGenericArguments()[1];
                        bool nullable = genericType.IsNullable();

                        if (type != typeof(IDictionary) && typeof(IDictionary).IsAssignableFrom(type))
                        {
                            Console.WriteLine("Create Dictionary Instance");
                            instance = Activator.CreateInstance(type, true) as IDictionary;
                        }
                        else
                        {
                            Console.WriteLine("Create Dictionary Instance for IDictionary interface");
                            Type genericDictType = typeof(Dictionary<,>).MakeGenericType(keyType, genericType);
                            instance = Activator.CreateInstance(genericDictType) as IDictionary;
                        }

                        foreach (KeyValuePair<string, object> item in jsonDict)
                        {
                            Console.WriteLine(item.Key + " = " + JsonMapper.DecodeValue(item.Value, genericType));
                            object value = JsonMapper.DecodeValue(item.Value, genericType);
                            object key = item.Key;

                            if (keyType == typeof(int))
                            {
                                key = int.Parse(item.Key);
                            }

                            if (value != null || nullable)
                            {
                                instance.Add(key, value);
                            }
                        }

                        return instance;
                    }

                    Console.WriteLine("Unexpected type arguemtns");
                }

                // Dictionary (convert int to string key)
                if (jsonObj is IDictionary<int, object>)
                {
                    Dictionary<string, object> jsonDict = new Dictionary<string, object>();

                    foreach (KeyValuePair<int, object> keyValuePair in (Dictionary<int, object>) jsonObj)
                    {
                        jsonDict.Add(keyValuePair.Key.ToString(), keyValuePair.Value);
                    }

                    if (type.GetGenericArguments().Length == 2)
                    {
                        IDictionary instance = null;
                        Type keyType = type.GetGenericArguments()[0];
                        Type genericType = type.GetGenericArguments()[1];
                        bool nullable = genericType.IsNullable();

                        if (type != typeof(IDictionary) && typeof(IDictionary).IsAssignableFrom(type))
                        {
                            instance = Activator.CreateInstance(type, true) as IDictionary;
                        }
                        else
                        {
                            Type genericDictType = typeof(Dictionary<,>).MakeGenericType(keyType, genericType);
                            instance = Activator.CreateInstance(genericDictType) as IDictionary;
                        }

                        foreach (KeyValuePair<string, object> item in jsonDict)
                        {
                            Console.WriteLine(item.Key + " = " + JsonMapper.DecodeValue(item.Value, genericType));
                            object value = JsonMapper.DecodeValue(item.Value, genericType);

                            if (value != null || nullable)
                            {
                                instance.Add(Convert.ToInt32(item.Key), value);
                            }
                        }

                        return instance;
                    }

                    Console.WriteLine("Unexpected type arguemtns");
                }

                Console.WriteLine("Couldn't decode Dictionary: " + type);

                return null;
            };
        }

        public static Decoder ArrayDecoder()
        {
            return (type, jsonObj) =>
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (jsonObj is IList)
                    {
                        IList jsonList = (IList) jsonObj;

                        if (type.IsArray)
                        {
                            Type elementType = type.GetElementType();
                            bool nullable = elementType.IsNullable();
                            Array array = Array.CreateInstance(elementType, jsonList.Count);

                            for (int i = 0; i < jsonList.Count; i++)
                            {
                                object value = JsonMapper.DecodeValue(jsonList[i], elementType);

                                if (value != null || nullable)
                                {
                                    array.SetValue(value, i);
                                }
                            }

                            return array;
                        }
                    }
                }

                Console.WriteLine("Couldn't decode Array: " + type);

                return null;
            };
        }

        public static Decoder ListDecoder()
        {
            return (type, jsonObj) =>
            {
                if (type.HasGenericInterface(typeof(IList<>)) && type.GetGenericArguments().Length == 1)
                {
                    Type genericType = type.GetGenericArguments()[0];

                    if (jsonObj is IList)
                    {
                        IList jsonList = (IList) jsonObj;
                        IList instance = null;
                        bool nullable = genericType.IsNullable();

                        if (type != typeof(IList) && typeof(IList).IsAssignableFrom(type))
                        {
                            instance = Activator.CreateInstance(type, true) as IList;
                        }
                        else
                        {
                            Type genericListType = typeof(List<>).MakeGenericType(genericType);
                            instance = Activator.CreateInstance(genericListType) as IList;
                        }

                        foreach (object item in jsonList)
                        {
                            object value = JsonMapper.DecodeValue(item, genericType);

                            if (value != null || nullable)
                            {
                                instance.Add(value);
                            }
                        }

                        return instance;
                    }
                }

                Console.WriteLine("Couldn't decode List: " + type);

                return null;
            };
        }

        public static Decoder CollectionDecoder()
        {
            return (type, jsonObj) =>
            {
                if (type.HasGenericInterface(typeof(ICollection<>)))
                {
                    Type genericType = type.GetGenericArguments()[0];

                    if (jsonObj is IList)
                    {
                        IList jsonList = (IList) jsonObj;

                        Type listType = type.IsInstanceOfGenericType(typeof(HashSet<>)) ? typeof(HashSet<>) : typeof(List<>);

                        Type constructedListType = listType.MakeGenericType(genericType);
                        object instance = Activator.CreateInstance(constructedListType, true);
                        bool nullable = genericType.IsNullable();
                        MethodInfo addMethodInfo = type.GetMethod("Add");

                        if (addMethodInfo != null)
                        {
                            foreach (object item in jsonList)
                            {
                                object value = JsonMapper.DecodeValue(item, genericType);

                                if (value != null || nullable)
                                {
                                    addMethodInfo.Invoke(instance, new[] { value });
                                }
                            }

                            return instance;
                        }
                    }
                }

                Console.WriteLine("Couldn't decode Collection: " + type);

                return null;
            };
        }

        public static Decoder EnumerableDecoder()
        {
            return (type, jsonObj) =>
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    // It could be an dictionary
                    if (jsonObj is IDictionary)
                    {
                        // Decode a dictionary
                        return DictionaryDecoder().Invoke(type, jsonObj);
                    }

                    // Or it could be also be a list
                    if (jsonObj is IList)
                    {
                        // Decode an array
                        if (type.IsArray)
                        {
                            return ArrayDecoder().Invoke(type, jsonObj);
                        }

                        // Decode a list
                        if (type.HasGenericInterface(typeof(IList<>)))
                        {
                            return ListDecoder().Invoke(type, jsonObj);
                        }

                        // Decode a collection
                        if (type.HasGenericInterface(typeof(ICollection<>)))
                        {
                            return CollectionDecoder().Invoke(type, jsonObj);
                        }
                    }
                }

                Console.WriteLine("Couldn't decode Enumerable: " + type);

                return null;
            };
        }
    }
}