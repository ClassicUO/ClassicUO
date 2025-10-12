using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLockFeaturesPacket_0xB9_Pre60142 : IPacket
{
    public byte Id => 0xB9;

    public LockedFeatureFlags Flags { get; private set; }
    public BodyConvFlags BodyConversionFlags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = (LockedFeatureFlags)reader.ReadUInt16BE();
        BodyConversionFlags = ComputeFlags(Flags);
    }

    internal static BodyConvFlags ComputeFlags(LockedFeatureFlags flags)
    {
        BodyConvFlags bcFlags = 0;
        if (flags.HasFlag(LockedFeatureFlags.UOR))
            bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.LBR))
            bcFlags |= BodyConvFlags.Anim1;
        if (flags.HasFlag(LockedFeatureFlags.AOS))
            bcFlags |= BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.SE))
            bcFlags |= BodyConvFlags.Anim3;
        if (flags.HasFlag(LockedFeatureFlags.ML))
            bcFlags |= BodyConvFlags.Anim4;

        return bcFlags;
    }
}
