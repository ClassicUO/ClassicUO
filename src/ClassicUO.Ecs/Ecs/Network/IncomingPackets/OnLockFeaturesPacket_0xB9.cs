using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLockFeaturesPacket_0xB9 : IPacket
{
    public byte Id => 0xB9;

    public LockedFeatureFlags Flags { get; private set; }
    public BodyConvFlags BodyConversionFlags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = reader.Remaining >= 4
            ? (LockedFeatureFlags)reader.ReadUInt32BE()
            : (LockedFeatureFlags)reader.ReadUInt16BE();

        BodyConversionFlags = 0;
        if (Flags.HasFlag(LockedFeatureFlags.UOR))
            BodyConversionFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
        if (Flags.HasFlag(LockedFeatureFlags.LBR))
            BodyConversionFlags |= BodyConvFlags.Anim1;
        if (Flags.HasFlag(LockedFeatureFlags.AOS))
            BodyConversionFlags |= BodyConvFlags.Anim2;
        if (Flags.HasFlag(LockedFeatureFlags.SE))
            BodyConversionFlags |= BodyConvFlags.Anim3;
        if (Flags.HasFlag(LockedFeatureFlags.ML))
            BodyConversionFlags |= BodyConvFlags.Anim4;
    }
}
