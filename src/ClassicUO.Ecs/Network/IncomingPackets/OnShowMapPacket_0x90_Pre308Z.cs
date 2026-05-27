using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnShowMapPacket_0x90_Pre308Z : IPacket
{
    public byte Id => 0x90;

    public uint Serial { get; private set; }
    public ushort GumpId { get; private set; }
    public ushort StartX { get; private set; }
    public ushort StartY { get; private set; }
    public ushort EndX { get; private set; }
    public ushort EndY { get; private set; }
    public ushort Width { get; private set; }
    public ushort Height { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        GumpId = reader.ReadUInt16BE();
        StartX = reader.ReadUInt16BE();
        StartY = reader.ReadUInt16BE();
        EndX = reader.ReadUInt16BE();
        EndY = reader.ReadUInt16BE();
        Width = reader.ReadUInt16BE();
        Height = reader.ReadUInt16BE();
    }
}
