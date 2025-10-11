using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnMapDataPacket_0x56 : IPacket
{
    public byte Id => 0x56;

    public uint Serial { get; private set; }
    public MapMessageType MessageType { get; private set; }
    public bool PlotEnabled { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        MessageType = (MapMessageType)reader.ReadUInt8();
        PlotEnabled = reader.ReadBool();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
    }
}
