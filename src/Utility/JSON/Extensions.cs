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
using System.Linq;
using System.Reflection;
using System.Text;

namespace TinyJson
{
    public static class StringExtensions
    {
        private static readonly StringBuilder _sb = new StringBuilder(256);

        public static string SnakeCaseToCamelCase(this string snakeCaseName)
        {
            _sb.Clear();

            bool next_upper = true;

            for (int i = 0; i < snakeCaseName.Length; i++)
            {
                if (snakeCaseName[i] == '_')
                {
                    next_upper = true;
                }
                else
                {
                    if (next_upper)
                    {
                        _sb.Append(char.ToUpperInvariant(snakeCaseName[i]));
                        next_upper = false;
                    }
                    else
                    {
                        _sb.Append(snakeCaseName[i]);
                    }
                }
            }

            return _sb.ToString();
        }

        public static string CamelCaseToSnakeCase(this string camelCaseName)
        {
            _sb.Clear();

            if (char.IsUpper(camelCaseName[0]))
            {
                _sb.Append(char.ToLowerInvariant(camelCaseName[0]));
            }

            for (int i = 1; i < camelCaseName.Length; i++)
            {
                if (char.IsUpper(camelCaseName[i]))
                {
                    _sb.Append("_");
                    _sb.Append(char.ToLowerInvariant(camelCaseName[i]));
                }
                else
                {
                    _sb.Append(camelCaseName[i]);
                }
            }

            return _sb.ToString();
        }
    }

    public static class TypeExtensions
    {
        public static bool IsInstanceOfGenericType(this Type type, Type genericType)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        public static bool HasGenericInterface(this Type type, Type genericInterface)
        {
            if (genericInterface == null)
            {
                throw new ArgumentNullException();
            }

            Predicate<Type> interfaceTest = i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(genericInterface);

            return interfaceTest(type) || type.GetInterfaces().Any(i => interfaceTest(i));
        }

        private static string UnwrapFieldName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (name[0] == '<')
                {
                    for (int i = 1; i < name.Length; i++)
                    {
                        if (name[i] == '>')
                        {
                            return name.Substring(1, i - 1);
                        }
                    }
                }
            }

            return name;
        }

        public static string UnwrappedPropertyName(this PropertyInfo property)
        {
            JsonPropertyAttribute attr = property.GetCustomAttribute<JsonPropertyAttribute>(true);

            if (attr != null)
            {
                return attr.Name;
            }

            return property.Name;
        }

        public static string UnwrappedFieldName(this FieldInfo field)
        {
            JsonPropertyAttribute attr = field.GetCustomAttribute<JsonPropertyAttribute>(true);

            if (attr != null)
            {
                return attr.Name;
            }

            return UnwrapFieldName(field.Name);
        }
    }

    public static class JsonExtensions
    {
        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsPrimitive;
        }

        public static bool IsNumeric(this Type type)
        {
            if (type.IsEnum)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single: return true;

                case TypeCode.Object:
                    Type underlyingType = Nullable.GetUnderlyingType(type);

                    return underlyingType != null && underlyingType.IsNumeric();

                default: return false;
            }
        }

        public static bool IsFloatingPoint(this Type type)
        {
            if (type.IsEnum)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single: return true;

                case TypeCode.Object:
                    Type underlyingType = Nullable.GetUnderlyingType(type);

                    return underlyingType != null && underlyingType.IsFloatingPoint();

                default: return false;
            }
        }
    }
}