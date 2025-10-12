using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLockFeaturesPacket_0xB9_Post60142 : IPacket
{
    public byte Id => 0xB9;

    public LockedFeatureFlags Flags { get; private set; }
    public BodyConvFlags BodyConversionFlags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = (LockedFeatureFlags)reader.ReadUInt32BE();
        BodyConversionFlags = OnLockFeaturesPacket_0xB9_Pre60142.ComputeFlags(Flags);
    }
}
