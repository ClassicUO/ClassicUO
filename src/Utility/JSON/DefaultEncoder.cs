using System;
using System.Collections;
using System.Reflection;

namespace TinyJson
{
	
	using Encoder = Action<object, JsonBuilder>;

	public static class DefaultEncoder {

		public static Encoder GenericEncoder() {
			return (obj, builder) => {
				builder.AppendBeginObject();
				Type type = obj.GetType();
				bool matchSnakeCase = type.GetCustomAttributes(typeof(MatchSnakeCaseAttribute), true).Length == 1;
				bool first = true;
				while (type != null) {
					foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
						if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0) {
							if (first) first = false; else builder.AppendSeperator();

							var fieldName = field.UnwrappedFieldName(type, false);
							if (matchSnakeCase) {
								fieldName = fieldName.CamelCaseToSnakeCase();
							}
							JsonMapper.EncodeNameValue(fieldName, field.GetValue(obj), builder);
						}
					}
					type = type.BaseType;
				}
				builder.AppendEndObject();
			};
		}

		public static Encoder DictionaryEncoder() {
			return (obj, builder) => {
				builder.AppendBeginObject();
				bool first = true;
				IDictionary dict = (IDictionary)obj;
				foreach (var key in dict.Keys) {
					if (first) first = false; else builder.AppendSeperator();
					JsonMapper.EncodeNameValue(key.ToString(), dict[key], builder);
				}
				builder.AppendEndObject();
			};
		}

		public static Encoder EnumerableEncoder() {
			return (obj, builder) => {
				builder.AppendBeginArray();
				bool first = true;
				foreach (var item in (IEnumerable)obj) {
					if (first) first = false; else builder.AppendSeperator();
					JsonMapper.EncodeValue(item, builder);
				}
				builder.AppendEndArray();
			};
		}

		public static Encoder ZuluDateEncoder() {
			return (obj, builder) => {
				DateTime date = (DateTime)obj;
				string zulu = date.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
				builder.AppendString(zulu);
			};
		}		
	}
}
