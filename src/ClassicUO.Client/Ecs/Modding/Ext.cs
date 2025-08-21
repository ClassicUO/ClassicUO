using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.IO;
using Extism.Sdk;

namespace ClassicUO.Ecs.Modding;

internal static class Ext
{
    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }

    public static T FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }
}
