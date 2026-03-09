using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Configuration.Json
{
    sealed class UOFontIndexConverter : JsonConverter<byte>
    {
        public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                reader.GetString();
                return 1;
            }
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetByte(out byte b))
                return (byte)Math.Max(0, Math.Min(19, (int)b));
            return 1;
        }

        public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Math.Max(0, Math.Min(19, (int)value)));
        }
    }
}
