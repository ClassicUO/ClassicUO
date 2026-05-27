using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnEquipItemPacket_0x2E : IPacket
{
    public byte Id => 0x2E;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public byte GraphicIncrement { get; private set; }
    public Layer Layer { get; private set; }
    public uint ContainerSerial { get; private set; }
    public ushort Hue { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        GraphicIncrement = reader.ReadUInt8();
        Layer = (Layer)reader.ReadUInt8();
        ContainerSerial = reader.ReadUInt32BE();
        Hue = reader.ReadUInt16BE();
    }
}
