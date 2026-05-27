using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLoginErrorPacket_0x82 : IPacket, ILoginErrorPacket
{
    public byte Id => 0x82;

    public byte Code { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Code = reader.ReadUInt8();
    }
}
