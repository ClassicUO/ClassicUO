using System;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenCompressedGumpPacket_0xDD : IPacket
{
    public byte Id => 0xDD;

    public uint Sender { get; private set; }
    public uint GumpId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public uint LayoutCompressedLength { get; private set; }
    public uint LayoutDecompressedLength { get; private set; }
    public byte[] LayoutData { get; private set; }
    public uint LinesCount { get; private set; }
    public uint LinesCompressedLength { get; private set; }
    public uint LinesDecompressedLength { get; private set; }
    public byte[] LinesData { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Sender = reader.ReadUInt32BE();
        GumpId = reader.ReadUInt32BE();
        X = reader.ReadInt32BE();
        Y = reader.ReadInt32BE();

        LayoutCompressedLength = reader.ReadUInt32BE() - 4u;
        LayoutDecompressedLength = reader.ReadUInt32BE();
        LayoutData = LayoutCompressedLength > 0 ? reader.ReadArray((int)LayoutCompressedLength) : Array.Empty<byte>();

        LinesCount = reader.ReadUInt32BE();

        if (LinesCount > 0)
        {
            LinesCompressedLength = reader.ReadUInt32BE() - 4u;
            LinesDecompressedLength = reader.ReadUInt32BE();
            LinesData = LinesCompressedLength > 0 ? reader.ReadArray((int)LinesCompressedLength) : Array.Empty<byte>();
        }
        else
        {
            LinesCompressedLength = 0;
            LinesDecompressedLength = 0;
            LinesData = Array.Empty<byte>();
        }
    }
}
