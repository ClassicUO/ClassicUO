// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Text;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Assets;

// https://github.com/cbnolok/UOETE/blob/master/src/uostringdictionary.cpp
public sealed class StringDictionaryLoader : UOFileLoader
{
    private string[] _strings = Array.Empty<string>();

    public StringDictionaryLoader(UOFileManager fileManager) : base(fileManager)
    {
    }

    public bool TryGetString(int index, out string str)
    {
        if (index < 0 || index >= _strings.Length)
        {
            str = string.Empty;
            return false;
        }

        str = _strings[index];
        return true;
    }

    public override void Load()
    {
        var path = FileManager.GetUOFilePath("string_dictionary.uop");
        if (!File.Exists(path))
            return;

        using var file = new UOFileUop(path, "build/stringdictionary/string_dictionary.bin");
        file.FillEntries();

        ref readonly var index = ref file.GetValidRefEntry(0);
        if (index.Length == 0)
            return;

        file.Seek(index.Offset, SeekOrigin.Begin);
        var buf = new byte[file.Length];
        file.Read(buf);

        var dbuf = new byte[index.DecompressedLength];
        var result = ZLib.Decompress(buf, dbuf);
        if (result != ZLib.ZLibError.Ok)
            return;

        var reader = new StackDataReader(dbuf);

        var unk1 = reader.ReadUInt64LE();
        var count = reader.ReadUInt32LE();
        _strings = new string[count];
        var unk2 = reader.ReadUInt32LE();
        for (var i = 0; i < count; ++i)
        {
            var len = reader.ReadUInt16LE();
            var str = Encoding.UTF8.GetString(reader.Buffer.Slice(reader.Position, len));
            _strings[i] = str;
            reader.Skip(len);
        }
    }
}