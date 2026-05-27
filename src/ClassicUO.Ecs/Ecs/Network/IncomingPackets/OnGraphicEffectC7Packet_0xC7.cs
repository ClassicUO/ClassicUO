using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnGraphicEffectC7Packet_0xC7 : IPacket
{
    public byte Id => 0xC7;

    public GraphicEffectType EffectType { get; private set; }
    public uint SourceSerial { get; private set; }
    public uint TargetSerial { get; private set; }
    public ushort Graphic { get; private set; }
    public ushort SourceX { get; private set; }
    public ushort SourceY { get; private set; }
    public sbyte SourceZ { get; private set; }
    public ushort TargetX { get; private set; }
    public ushort TargetY { get; private set; }
    public sbyte TargetZ { get; private set; }
    public byte Speed { get; private set; }
    public byte Duration { get; private set; }
    public ushort Unknown { get; private set; }
    public bool FixedDirection { get; private set; }
    public bool WillExplode { get; private set; }
    public uint Hue { get; private set; }
    public GraphicEffectBlendMode BlendMode { get; private set; }
    public ushort TileId { get; private set; }
    public ushort ExplodeEffect { get; private set; }
    public ushort ExplodeSound { get; private set; }
    public uint ExtraSerial { get; private set; }
    public byte Layer { get; private set; }

    public void Fill(StackDataReader reader)
    {
        EffectType = (GraphicEffectType)reader.ReadUInt8();
        SourceSerial = reader.ReadUInt32BE();
        TargetSerial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        SourceX = reader.ReadUInt16BE();
        SourceY = reader.ReadUInt16BE();
        SourceZ = reader.ReadInt8();
        TargetX = reader.ReadUInt16BE();
        TargetY = reader.ReadUInt16BE();
        TargetZ = reader.ReadInt8();
        Speed = reader.ReadUInt8();
        Duration = reader.ReadUInt8();
        Unknown = reader.ReadUInt16BE();
        FixedDirection = reader.ReadBool();
        WillExplode = reader.ReadBool();
        Hue = reader.ReadUInt32BE();
        BlendMode = (GraphicEffectBlendMode)reader.ReadUInt32BE();
        TileId = reader.ReadUInt16BE();
        ExplodeEffect = reader.ReadUInt16BE();
        ExplodeSound = reader.ReadUInt16BE();
        ExtraSerial = reader.ReadUInt32BE();
        Layer = reader.ReadUInt8();
        reader.Skip(2); // padding
    }
}
