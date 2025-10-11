using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenGumpPacket_0xB0 : IPacket
{
    internal struct GumpLine
    {
        public ushort Length;
        public string Text;
    }

    public byte Id => 0xB0;

    public uint Sender { get; private set; }
    public uint GumpId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public ushort CommandLength { get; private set; }
    public string Command { get; private set; }
    public ushort LinesCount { get; private set; }
    public List<GumpLine> Lines { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Sender = reader.ReadUInt32BE();
        GumpId = reader.ReadUInt32BE();
        X = reader.ReadInt32BE();
        Y = reader.ReadInt32BE();
        CommandLength = reader.ReadUInt16BE();
        Command = CommandLength > 0 ? reader.ReadASCII(CommandLength) : string.Empty;
        LinesCount = reader.ReadUInt16BE();

        Lines ??= new List<GumpLine>();
        Lines.Clear();
        Lines.Capacity = LinesCount;
        for (var i = 0; i < LinesCount; ++i)
        {
            var length = reader.ReadUInt16BE();
            Lines.Add(new GumpLine
            {
                Length = length,
                Text = reader.ReadUnicodeBE(length)
            });
        }
    }
}
