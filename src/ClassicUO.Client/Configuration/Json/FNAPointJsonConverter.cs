using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration.Json
{
    sealed class Point2Converter : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                return Point.Zero;
            }

            var point = new Point();

            point.X = reader.GetInt32();

            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                return Point.Zero;
            }

            point.Y = reader.GetInt32();

            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                return Point.Zero;
            }

            return point;
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }

    sealed class NullablePoint2Converter : JsonConverter<Point?>
    {
        public override Point? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                return Point.Zero;
            }

            var point = new Point();

            point.X = reader.GetInt32();

            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return Point.Zero;
            }

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                return Point.Zero;
            }

            point.Y = reader.GetInt32();

            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                return Point.Zero;
            }

            return point;
        }

        public override void Write(Utf8JsonWriter writer, Point? value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.Value.X);
            writer.WriteNumber("Y", value.Value.Y);
            writer.WriteEndObject();
        }
    }
}