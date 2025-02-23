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

	public unsafe Clay_String Get(string txt)
	{
		if (_dictionary.TryGetValue(txt, out var pair)) return Get(pair);

		var bytes = Encoding.UTF8.GetBytes(txt);
		pair.handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		pair.length = bytes.Length;
		_dictionary.Add(txt, pair);
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

	public Clay_String this[string str] => Get(str);
}