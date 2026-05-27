using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateContainerPacket_0x25_Post6017 : IPacket, IUpdateContainerPacket
{
    public byte Id => 0x25;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public sbyte GraphicIncrement { get; private set; }
    public ushort Amount { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public byte GridIndex { get; private set; }
    public uint ContainerSerial { get; private set; }
    public ushort Hue { get; private set; }
    public bool HasGridIndex => true;

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        GraphicIncrement = reader.ReadInt8();
        var amount = reader.ReadUInt16BE();
        Amount = amount == 0 ? (ushort)1 : amount;
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        GridIndex = reader.ReadUInt8();
        ContainerSerial = reader.ReadUInt32BE();
        Hue = reader.ReadUInt16BE();
    }
}
