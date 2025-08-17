using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.IO;
using Extism.Sdk;

namespace ClassicUO.Ecs.Modding;

internal static class Ext
{
    public static TempBuffer<byte> Buffer(this CurrentPlugin p, long offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
        }

        var span = p.ReadBytes(offset);

        var temp = new TempBuffer<byte>(span.Length);
        span.CopyTo(temp.Span);

        return temp;
    }

    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }

    public static T FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }
}
