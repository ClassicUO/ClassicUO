using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnConfirmWalkPacket_0x22 : IPacket
{
    public byte Id => 0x22;

    public byte Sequence { get; private set; }
    public NotorietyFlag Notoriety { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Sequence = reader.ReadUInt8();
        Notoriety = (NotorietyFlag)reader.ReadUInt8();
    }
}
