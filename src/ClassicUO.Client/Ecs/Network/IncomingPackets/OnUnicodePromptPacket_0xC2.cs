using System;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUnicodePromptPacket_0xC2 : IPacket
{
    public byte Id => 0xC2;

    public uint Serial { get; private set; }
    public uint MessageId { get; private set; }
    public byte[] RemainingData { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        MessageId = reader.ReadUInt32BE();
        RemainingData = reader.Remaining > 0 ? reader.ReadArray(reader.Remaining) : Array.Empty<byte>();
    }
}
