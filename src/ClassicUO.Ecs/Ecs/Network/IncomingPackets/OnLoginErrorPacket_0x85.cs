using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLoginErrorPacket_0x85 : IPacket, ILoginErrorPacket
{
    public byte Id => 0x85;

    public byte Code { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Code = reader.ReadUInt8();
    }
}
