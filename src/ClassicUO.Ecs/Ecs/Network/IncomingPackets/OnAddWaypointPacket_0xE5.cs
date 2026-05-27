using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnAddWaypointPacket_0xE5 : IPacket
{
    public byte Id => 0xE5;

    public uint Serial { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public sbyte Z { get; private set; }
    public byte Map { get; private set; }
    public WaypointsType WaypointType { get; private set; }
    public bool IgnoreObject { get; private set; }
    public uint Cliloc { get; private set; }
    public string Name { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadInt8();
        Map = reader.ReadUInt8();
        WaypointType = (WaypointsType)reader.ReadUInt16BE();
        IgnoreObject = reader.ReadUInt16BE() != 0;
        Cliloc = reader.ReadUInt32BE();
        Name = reader.ReadUnicodeLE();
    }
}
