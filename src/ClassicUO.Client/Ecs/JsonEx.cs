namespace ClassicUO.Ecs;


public static class JsonEx
{
    public static string ToJson<T>(this T obj) where T : Serde.ISerializeProvider<T>
    {
        return Serde.Json.JsonSerializer.Serialize(obj);
    }

    public static string ToJson<T, TProxy>(this T obj) where TProxy : Serde.ISerializeProvider<T>
    {
        return Serde.Json.JsonSerializer.Serialize<T, TProxy>(obj);
    }

    public static T FromJson<T>(this string json) where T : Serde.IDeserializeProvider<T>
    {
        return Serde.Json.JsonSerializer.Deserialize<T>(json);
    }
}