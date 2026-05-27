using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnChatMessagePacket_0xB2 : IPacket
{
    public byte Id => 0xB2;

    public ushort Command { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Command = reader.ReadUInt16BE();
    }
}
