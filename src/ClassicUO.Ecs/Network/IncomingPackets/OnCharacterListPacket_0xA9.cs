using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnCharacterListPacket_0xA9 : IPacket
{
    public byte Id => 0xA9;

    public List<CharacterInfo> Characters { get; private set; }
    public byte CityCount { get; private set; }
    // Raw tail after the city count: the city block, then the 4-byte
    // CharacterListFlags, then (on 70130+ clients) a trailing (short)-1. The
    // city block is version-sized, so flags can't be located here — the plugin
    // parses the cities (it has the client version) and reads the flags right
    // after them. The old "flags = last 4 bytes" shortcut mis-read the trailing
    // -1 into the flags (0xFFFF), which forced the AOS/tooltip bit on.
    public byte[] CityData { get; private set; }

    public void Fill(StackDataReader reader)
    {
        var characterCount = reader.ReadUInt8();
        var characters = new List<CharacterInfo>();

        for (uint i = 0; i < characterCount; ++i)
        {
            var name = reader.ReadASCII(30).TrimEnd('\0').Trim();
            reader.Skip(30);

            if (!string.IsNullOrEmpty(name))
            {
                characters.Add(new CharacterInfo(name, i));
            }
        }

        Characters = characters;

        CityCount = reader.ReadUInt8();

        CityData = reader.Remaining > 0 ? reader.ReadArray(reader.Remaining) : Array.Empty<byte>();
    }
}
