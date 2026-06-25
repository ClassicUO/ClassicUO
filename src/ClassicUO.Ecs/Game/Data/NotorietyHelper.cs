using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Ecs;

/// Profile-driven notoriety hue lookup shared by the overhead/aura renderers
/// (was a byte-identical NotorietyHue(Profile, NotorietyFlag) copy in
/// MobileHpOverheads + AuraOverlay).
internal static class NotorietyHelper
{
    // Legacy Notoriety.GetHue — profile-driven, invulnerable fixed yellow.
    public static ushort NotorietyHue(Profile profile, NotorietyFlag flag) => flag switch
    {
        NotorietyFlag.Innocent => profile.InnocentHue,
        NotorietyFlag.Ally => profile.FriendHue,
        NotorietyFlag.Criminal => profile.CriminalHue,
        NotorietyFlag.Gray => profile.CanAttackHue,
        NotorietyFlag.Enemy => profile.EnemyHue,
        NotorietyFlag.Murderer => profile.MurdererHue,
        NotorietyFlag.Invulnerable => 0x0034,
        _ => 0,
    };
}
