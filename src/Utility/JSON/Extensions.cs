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

            Predicate<Type> interfaceTest = i =>
                i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(genericInterface);

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