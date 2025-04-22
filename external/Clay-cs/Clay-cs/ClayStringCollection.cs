using System.Runtime.InteropServices;
using System.Text;

namespace Clay_cs;

/// <summary>
/// stores string pointers to utf8 byte array versions of utf16 c# strings
/// It is safe to clear this the start of every layout loop.
/// TODO: find a way to do this without alloc every frame
/// </summary>
public readonly struct ClayStringCollection() : IDisposable
{
    private readonly Dictionary<string, (GCHandle handle, int length)> _dictionary = new();

    public Clay_String Get(ReadOnlySpan<char> txt)
    {
        var lookup = _dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(txt, out var pair)) return Get(pair);

        var count = Encoding.UTF8.GetByteCount(txt);

        var buffer = new byte[count];
        var read = Encoding.UTF8.GetBytes(txt, buffer.AsSpan(0, count));

        pair.handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        pair.length = read;

        _ = lookup.TryAdd(txt, pair);
        return Get(pair);
    }

    private static unsafe Clay_String Get((GCHandle handle, int length) pair)
    {
        return new Clay_String
        {
            length = pair.length,
            chars = (sbyte*)pair.handle.AddrOfPinnedObject(),
        };
    }

    public void Clear()
    {
        foreach (var pair in _dictionary)
        {
            pair.Value.handle.Free();
        }

        _dictionary.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    public Clay_String this[ReadOnlySpan<char> str] => Get(str);
}