using System;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPlaceMultiPacket_0x99 : IPacket
{
    public byte Id => 0x99;

    public bool OnGround { get; private set; }
    public uint TargetSerial { get; private set; }
    public byte Flags { get; private set; }
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
        // Legacy MultiPlacement does Seek(18) — absolute from the packet start
        // with the id at byte 0. This reader is sliced past the id, so the same
        // anchor (multi id) sits at byte 17. The bytes between flags and here are
        // unused. The wrong relative ReadArray(18) landed the read at byte 24,
        // so MultiId/offsets/hue were garbage and no preview multi resolved.
        reader.Seek(17);
        MultiId = reader.ReadUInt16BE();
        OffsetX = reader.ReadInt16BE();
        OffsetY = reader.ReadInt16BE();
        OffsetZ = reader.ReadInt16BE();
        Hue = reader.ReadUInt16BE();
    }
}
