namespace TinyJson
{
    public static class Json
    {
        public static T Decode<T>(this string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            object jsonObj = JsonParser.ParseValue(json);

            if (jsonObj == null)
            {
                return default;
            }

            return JsonMapper.DecodeJsonObject<T>(jsonObj);
        }

        public static string Encode(this object value, bool pretty = false)
        {
            JsonBuilder builder = new JsonBuilder(pretty);
            JsonMapper.EncodeValue(value, builder);

            return builder.ToString();
        }
    }
}