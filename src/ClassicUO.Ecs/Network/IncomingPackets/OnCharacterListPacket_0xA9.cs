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
    public byte[] CityData { get; private set; }
    public CharacterListFlags Flags { get; private set; }

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

        var remaining = reader.Remaining;
        if (remaining >= sizeof(uint))
        {
            var cityDataLength = remaining - sizeof(uint);
            CityData = cityDataLength > 0 ? reader.ReadArray(cityDataLength) : Array.Empty<byte>();
            Flags = (CharacterListFlags)reader.ReadUInt32BE();
        }
        else
        {
            CityData = Array.Empty<byte>();
            Flags = 0;
        }
    }
}
