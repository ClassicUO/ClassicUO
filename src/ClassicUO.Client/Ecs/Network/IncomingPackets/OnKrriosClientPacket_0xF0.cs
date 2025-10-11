using System;
using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnKrriosClientPacket_0xF0 : IPacket
{
    internal struct LocationEntry
    {
        public uint Serial;
        public ushort X;
        public ushort Y;
        public byte Map;
        public byte Hits;
    }

    public byte Id => 0xF0;

    public byte PacketType { get; private set; }
    public bool LocationsOnly { get; private set; }
    public List<LocationEntry> Locations { get; private set; }
    public byte[] ExtraData { get; private set; }

    public void Fill(StackDataReader reader)
    {
        PacketType = reader.ReadUInt8();
        Locations ??= new List<LocationEntry>();
        Locations.Clear();
        LocationsOnly = false;
        ExtraData = Array.Empty<byte>();

        switch (PacketType)
        {
            case 1:
            case 2:
                LocationsOnly = PacketType == 1 || reader.ReadBool();
                uint serial;
                while ((serial = reader.ReadUInt32BE()) != 0)
                {
                    Locations.Add(new LocationEntry
                    {
                        Serial = serial,
                        X = reader.ReadUInt16BE(),
                        Y = reader.ReadUInt16BE(),
                        Map = reader.ReadUInt8(),
                        Hits = PacketType == 1 ? (byte)0 : reader.ReadUInt8()
                    });
                }
                break;

            default:
                ExtraData = reader.Remaining > 0 ? reader.ReadArray(reader.Remaining) : Array.Empty<byte>();
                break;
        }
    }
}
