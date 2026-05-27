using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLogoutRequestPacket_0xD1 : IPacket
{
    public byte Id => 0xD1;

    public bool ShouldDisconnect { get; private set; }

    public void Fill(StackDataReader reader)
    {
        ShouldDisconnect = reader.ReadBool();
    }
}
