using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDenyMoveItemPacket_0x27 : IPacket
{
    public byte Id => 0x27;

    public byte Code { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Code = reader.ReadUInt8();
    }
}
