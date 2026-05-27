using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateItemPacket_0x1A : IPacket
{
    public byte Id => 0x1A;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public byte GraphicIncrement { get; private set; }
    public ushort Amount { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public sbyte Z { get; private set; }
    public Direction Direction { get; private set; }
    public ushort Hue { get; private set; }
    public Flags Flags { get; private set; }
    public byte Type { get; private set; }
    public bool HasAmount { get; private set; }
    public bool HasDirection { get; private set; }
    public bool HasHue { get; private set; }
    public bool HasFlags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        HasAmount = (Serial & 0x8000_0000u) != 0;
        if (HasAmount)
        {
            Serial &= 0x7FFF_FFFFu;
        }

        Graphic = reader.ReadUInt16BE();
        if ((Graphic & 0x8000) != 0)
        {
            Graphic &= 0x7FFF;
            GraphicIncrement = reader.ReadUInt8();
        }
        else
        {
            GraphicIncrement = 0;
        }

        Amount = HasAmount ? reader.ReadUInt16BE() : (ushort)1;

        X = reader.ReadUInt16BE();
        HasDirection = (X & 0x8000) != 0;
        if (HasDirection)
        {
            X &= 0x7FFF;
        }

        Y = reader.ReadUInt16BE();
        HasHue = (Y & 0x8000) != 0;
        if (HasHue)
        {
            Y &= 0x7FFF;
        }

        HasFlags = (Y & 0x4000) != 0;
        if (HasFlags)
        {
            Y &= 0x3FFF;
        }

        Direction = HasDirection ? (Direction)reader.ReadUInt8() : Direction.North;
        Z = reader.ReadInt8();
        Hue = HasHue ? reader.ReadUInt16BE() : (ushort)0;
        Flags = HasFlags ? (Flags)reader.ReadUInt8() : 0;

        Type = Graphic >= 0x4000 ? (byte)2 : (byte)0;
    }
}
