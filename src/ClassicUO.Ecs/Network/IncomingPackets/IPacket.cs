using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal interface IPacket
{
    byte Id { get; }
    void Fill(StackDataReader reader);
}
