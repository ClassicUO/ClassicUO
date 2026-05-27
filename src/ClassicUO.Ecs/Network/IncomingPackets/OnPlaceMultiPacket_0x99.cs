using System;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPlaceMultiPacket_0x99 : IPacket
{
    public byte Id => 0x99;

    public bool OnGround { get; private set; }
    public uint TargetSerial { get; private set; }
    public byte Flags { get; private set; }
    public byte[] UnknownData { get; private set; }
    public ushort MultiId { get; private set; }
    public short OffsetX { get; private set; }
    public short OffsetY { get; private set; }
    public short OffsetZ { get; private set; }
    public ushort Hue { get; private set; }

    public void Fill(StackDataReader reader)
    {
        OnGround = reader.ReadBool();
        TargetSerial = reader.ReadUInt32BE();
        Flags = reader.ReadUInt8();
        UnknownData = reader.ReadArray(18);
        MultiId = reader.ReadUInt16BE();
        OffsetX = reader.ReadInt16BE();
        OffsetY = reader.ReadInt16BE();
        OffsetZ = reader.ReadInt16BE();
        Hue = reader.ReadUInt16BE();
    }
}
