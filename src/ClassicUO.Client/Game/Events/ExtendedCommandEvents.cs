// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Events
{
    // ---- 0xBF ExtendedCommand sub-command events ----

    /// <summary>Sub-cmd 0x01: server resets the fast-walk prevention stack with six new keys.</summary>
    internal readonly record struct FastWalkStackInitArgs(IReadOnlyList<uint> Values);

    /// <summary>Sub-cmd 0x02: server pushes one key to the fast-walk prevention stack.</summary>
    internal readonly record struct FastWalkStackAddArgs(uint Value);

    /// <summary>Sub-cmd 0x04: server requests the client to close a generic gump (or dispatch a virtual button click on it).</summary>
    internal readonly record struct GenericGumpCloseArgs(uint Serial, int Button);

    /// <summary>Sub-cmd 0x06: party manager packet payload (multi-format inner packet). Bytes carried verbatim because the party manager owns its own sub-format parser.</summary>
    internal readonly record struct PartyPacketArgs(byte[] Bytes);

    /// <summary>Sub-cmd 0x08: server changes the active map facet.</summary>
    internal readonly record struct MapIndexChangedArgs(byte MapIndex);

    /// <summary>Sub-cmd 0x0C: close a mobile's status / health bar gump.</summary>
    internal readonly record struct CloseStatusbarGumpArgs(uint Serial);

    /// <summary>One attribute line in <see cref="EquipInfoArgs"/>.</summary>
    /// <param name="Cliloc">Cliloc identifier for the attribute string.</param>
    /// <param name="Charges">-1 for a tag attribute, otherwise the numeric value.</param>
    internal readonly record struct EquipInfoLine(int Cliloc, short Charges);

    /// <summary>Sub-cmd 0x10: equip info for an item (name cliloc + optional crafter + optional unidentified flag + attribute lines).</summary>
    internal readonly record struct EquipInfoArgs(
        uint ItemSerial,
        uint Cliloc,
        string CrafterName,
        bool Unidentified,
        IReadOnlyList<EquipInfoLine> Lines);

    /// <summary>Sub-cmd 0x14: display popup / context menu.</summary>
    internal readonly record struct PopupMenuArgs(Data.PopupMenuData Data);

    /// <summary>Sub-cmd 0x16: server requests the client to close a UI window of a known kind.</summary>
    internal readonly record struct CloseUserInterfaceArgs(uint GumpKindId, uint Serial);

    /// <summary>Sub-cmd 0x18: server pushed a map patches update. Bytes are forwarded verbatim because the map loader consumes them with its own reader.</summary>
    internal readonly record struct MapPatchesEnabledArgs(byte[] Bytes);

    /// <summary>Sub-cmd 0x19 version 0: bonded-pet dead flag update.</summary>
    internal readonly record struct ExtendedStatsBondedArgs(uint Serial, bool IsDead);

    /// <summary>Sub-cmd 0x19 version 2: stat lock state for the player.</summary>
    internal readonly record struct ExtendedStatsLocksArgs(uint Serial, byte StrLock, byte DexLock, byte IntLock);

    /// <summary>Sub-cmd 0x19 version 5 with type byte 0xFF: forced single-frame mobile animation.</summary>
    internal readonly record struct ExtendedStatsAnimationArgs(uint Serial, ushort Animation, ushort Frame);

    /// <summary>Sub-cmd 0x1B: new spellbook content for a spellbook item.</summary>
    internal readonly record struct SpellbookContentArgs(
        uint Serial,
        ushort SpellbookGraphic,
        ushort SpellbookType,
        IReadOnlyList<int> SpellIds);

    /// <summary>Sub-cmd 0x1D: server-published house revision state.</summary>
    internal readonly record struct HouseRevisionStateArgs(uint Serial, uint Revision);

    /// <summary>Sub-cmd 0x20: house design tool state change.</summary>
    /// <param name="Action">1 = update, 2 = remove, 3 = update multi pos, 4 = begin customization, 5 = end customization.</param>
    internal readonly record struct HouseDesignStateArgs(
        uint Serial,
        byte Action,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z);

    /// <summary>Sub-cmd 0x21: server requested the client to clear "in use" flags from all primary/secondary abilities.</summary>
    internal readonly record struct AbilityIconsResetArgs();

    /// <summary>Sub-cmd 0x22: short damage indicator over an entity.</summary>
    internal readonly record struct DamageOverheadArgs(uint Serial, byte Damage);

    /// <summary>Sub-cmd 0x25: spell icon toggle.</summary>
    internal readonly record struct SpellIconToggleArgs(ushort SpellId, bool Active);

    /// <summary>Sub-cmd 0x26: character speed mode change.</summary>
    internal readonly record struct CharacterSpeedModeArgs(byte SpeedMode);

    /// <summary>Sub-cmd 0x2A: race-change gump request.</summary>
    internal readonly record struct RaceChangeRequestedArgs(bool IsFemale, byte Race);

    /// <summary>Sub-cmd 0x2B: forced single-frame animation by short serial.</summary>
    internal readonly record struct MobileAnimationFrameArgs(ushort PartialSerial, byte AnimationId, byte FrameCount);

    /// <summary>Sub-cmd 0xBEEF: ClassicUO custom server command.</summary>
    internal readonly record struct CuoCommandArgs(ushort Type);
}
