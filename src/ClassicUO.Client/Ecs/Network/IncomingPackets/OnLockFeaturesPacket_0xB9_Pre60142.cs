using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLockFeaturesPacket_0xB9_Pre60142 : IPacket
{
    public byte Id => 0xB9;

    public LockedFeatureFlags Flags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = (LockedFeatureFlags)reader.ReadUInt16BE();
    }
}
