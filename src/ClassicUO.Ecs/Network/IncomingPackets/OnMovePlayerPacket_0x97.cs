using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnMovePlayerPacket_0x97 : IPacket
{
    public byte Id => 0x97;

    public Direction Direction { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Direction = (Direction)reader.ReadUInt8();
    }
}
