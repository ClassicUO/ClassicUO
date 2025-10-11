using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDragAnimationPacket_0x23 : IPacket
{
    public byte Id => 0x23;

    public ushort Graphic { get; private set; }
    public byte GraphicIncrement { get; private set; }
    public ushort Hue { get; private set; }
    public ushort Amount { get; private set; }
    public uint SourceSerial { get; private set; }
    public ushort SourceX { get; private set; }
    public ushort SourceY { get; private set; }
    public sbyte SourceZ { get; private set; }
    public uint TargetSerial { get; private set; }
    public ushort TargetX { get; private set; }
    public ushort TargetY { get; private set; }
    public sbyte TargetZ { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Graphic = reader.ReadUInt16BE();
        GraphicIncrement = reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Amount = reader.ReadUInt16BE();
        SourceSerial = reader.ReadUInt32BE();
        SourceX = reader.ReadUInt16BE();
        SourceY = reader.ReadUInt16BE();
        SourceZ = reader.ReadInt8();
        TargetSerial = reader.ReadUInt32BE();
        TargetX = reader.ReadUInt16BE();
        TargetY = reader.ReadUInt16BE();
        TargetZ = reader.ReadInt8();
    }
}
