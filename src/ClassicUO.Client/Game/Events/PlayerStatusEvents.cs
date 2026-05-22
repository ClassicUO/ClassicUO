// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    // Args carried by EventSink.CharacterStatusReceived. Mirrors the wire
    // layout of the 0x11 CharacterStatus packet (a.k.a. MobileStatus) so the
    // subscriber can replay every side effect the legacy handler had.
    //
    // The wire `Type` byte determines which trailing blocks are present:
    //   0           -> basic mobile only (Name/Hits/HitsMax + IsRenamable)
    //   > 0         -> self packet (HasExtendedStats == true), str/dex/int...
    //   >= 3        -> Renaissance fields (StatsCap, Followers/Max)
    //   >= 4        -> AOS fields (resistances, luck, damage, tithing points)
    //   >= 5        -> ML fields (WeightMax + Race) — for older type values
    //                  the subscriber is expected to compute WeightMax from
    //                  strength and the client version.
    //   >= 6        -> KR/SA fields (max resistances + chance/cost modifiers,
    //                  optional / possibly truncated on wire)
    //
    // Fields not present at the wire `Type` value are left at their default.
    internal readonly record struct CharacterStatusReceivedArgs(
        uint Serial,
        string Name,
        ushort Hits,
        ushort HitsMax,
        bool IsRenamable,
        bool IsFemale,
        byte Type,
        bool HasExtendedStats,
        ushort Strength,
        ushort Dexterity,
        ushort Intelligence,
        ushort Stamina,
        ushort StaminaMax,
        ushort Mana,
        ushort ManaMax,
        uint Gold,
        short PhysicalResistance,
        ushort Weight,
        ushort WeightMax,
        byte Race,
        bool HasRenaissanceStats,
        short StatsCap,
        byte Followers,
        byte FollowersMax,
        bool HasAosStats,
        short FireResistance,
        short ColdResistance,
        short PoisonResistance,
        short EnergyResistance,
        ushort Luck,
        short DamageMin,
        short DamageMax,
        uint TithingPoints,
        bool HasKrSaStats,
        short MaxPhysicResistence,
        short MaxFireResistence,
        short MaxColdResistence,
        short MaxPoisonResistence,
        short MaxEnergyResistence,
        short DefenseChanceIncrease,
        short MaxDefenseChanceIncrease,
        short HitChanceIncrease,
        short SwingSpeedIncrease,
        short DamageIncrease,
        short LowerReagentCost,
        short SpellDamageIncrease,
        short FasterCastRecovery,
        short FasterCasting,
        short LowerManaCost);
}
