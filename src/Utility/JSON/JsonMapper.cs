using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace TinyJson
{

	using Encoder = Action<object, JsonBuilder>;
	using Decoder = Func<Type, object, object>;

	public static class JsonMapper {

		internal static Encoder genericEncoder;
		internal static Decoder genericDecoder;
		internal static IDictionary<Type, Encoder> encoders = new Dictionary<Type, Encoder>();
		internal static IDictionary<Type, Decoder> decoders = new Dictionary<Type, Decoder>();

		static JsonMapper() {
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

		public static void RegisterDecoder(Type type, Decoder decoder) {
			if (type == typeof(object)) {
				genericDecoder = decoder;
			} else {
				decoders.Add(type, decoder);
			}
		}

		public static void RegisterEncoder(Type type, Encoder encoder) {
			if (type == typeof(object)) {
				genericEncoder = encoder;
			} else {
				encoders.Add(type, encoder);
			}
		}

		public static Decoder GetDecoder(Type type) {
			if (decoders.ContainsKey(type)) {
				return decoders[type];
			} 
			foreach (var entry in decoders) {
				Type baseType = entry.Key;
				if (baseType.IsAssignableFrom(type)) {
					return entry.Value;
				}
				if (baseType.HasGenericInterface(type)) {
					return entry.Value;
				}
			}
			return genericDecoder;
		}

		public static Encoder GetEncoder(Type type) {
			if (encoders.ContainsKey(type)) {
				return encoders[type];
			} 
			foreach (var entry in encoders) {
				Type baseType = entry.Key;
				if (baseType.IsAssignableFrom(type)) {
					return entry.Value;
				}
				if (baseType.HasGenericInterface(type)) {
					return entry.Value;
				}
			}
			return genericEncoder;
		}

		public static T DecodeJsonObject<T>(object jsonObj) {
			Decoder decoder = GetDecoder(typeof(T));
			return (T)decoder(typeof(T), jsonObj);
		}

		public static void EncodeValue(object value, JsonBuilder builder) {
			if (JsonBuilder.IsSupported(value)) {
				builder.AppendValue(value);
			} else {
				Encoder encoder = GetEncoder(value.GetType()); 
				if (encoder != null) {
					encoder(value, builder);
				} else {
					Console.WriteLine("Encoder for " + value.GetType() + " not found");
				}
			}
		}

		public static void EncodeNameValue(string name, object value, JsonBuilder builder) {
			builder.AppendName(name);
			EncodeValue(value, builder);
		}

		static object ConvertValue(object value, Type type) {
			if (value != null) {
				Type safeType = Nullable.GetUnderlyingType(type) ?? type;
                if (!type.IsEnum)
                {
                    var converter = TypeDescriptor.GetConverter(safeType);

                    if (converter.CanConvertFrom(value.GetType()))
                    {
                        return converter.ConvertFrom(value);
                    }
                    return Convert.ChangeType(value, safeType);
                } else {
                    if (value is string) {
                        return Enum.Parse(type, (string)value);
                    } else {
                        return Enum.ToObject(type, value);
                    }
                }
			}
			return value;
		}

		public static object DecodeValue(object value, Type targetType) {
			if (value == null) return null;

			if (JsonBuilder.IsSupported(value)) {
				value = ConvertValue(value, targetType);
			}

			// use a registered decoder
			if (value != null && !targetType.IsAssignableFrom(value.GetType())) {
				Decoder decoder = GetDecoder(targetType);
				value = decoder(targetType, value);
			}

			if (value != null && targetType.IsAssignableFrom(value.GetType())) {
				return value;
			} else {
				Console.WriteLine("Couldn't decode: " + targetType);
				return null;
			}
		}

		public static bool DecodeValue(object target, string name, object value) {
			Type type = target.GetType();
			bool matchSnakeCase = type.GetCustomAttributes(typeof(MatchSnakeCaseAttribute), true).Length == 1;

			while (type != null) {
				foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
					if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0) {
						if (field.MatchFieldName(name, type, matchSnakeCase)) {
							if (value != null) {
								Type targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
								object decodedValue = DecodeValue(value, targetType);

								if (decodedValue != null && targetType.IsAssignableFrom(decodedValue.GetType())) {
									field.SetValue(target, decodedValue);
									return true;
								} else {
									return false;
								}
							} else {
								field.SetValue(target, null);
								return true;
							}
						}
					}
				}
				type = type.BaseType;
			}
			return false;
		}
	}
}

