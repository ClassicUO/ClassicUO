// SPDX-License-Identifier: BSD-2-Clause
//
// Player skill values + the skill-group layout, populated from the 0x3A
// UpdateSkills packet. Mirrors main's World.Player.Skills array plus
// SkillsGroupManager. The StandardSkillsGump (SkillsGumpPlugin) reads both.
//
// Values are stored raw (server tenths: 1000 == 100.0); the gump divides by
// 10 for display. Names come from the shared SkillsLoader (assets), not the
// packet — only the rarely-used 0xFE definition variant carries names.

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Ecs;

internal sealed class PlayerSkills
{
    internal struct SkillValue
    {
        public ushort Real;  // value with bonuses (0x3A RealValue)
        public ushort Base;  // base value      (0x3A BaseValue)
        public ushort Cap;   // skill cap        (0x3A Cap, when present)
        public Lock Lock;
        public bool Received;
    }

    // Indexed by skill id. 256 covers every expansion's skill table.
    public readonly SkillValue[] Values = new SkillValue[256];
    public bool HasData;

    // Group layout (names + member skill ids). Built once from the CUO
    // defaults the first time the gump opens (skillgrp.mul is not consulted —
    // ModernUO ships the CUO default 7-group layout).
    public readonly List<SkillsGroup> Groups = new();

    public float SumValues(bool useBase)
    {
        float sum = 0;
        for (int i = 0; i < Values.Length; i++)
        {
            if (!Values[i].Received) continue;
            sum += (useBase ? Values[i].Base : Values[i].Real) / 10f;
        }
        return sum;
    }
}

// One collapsible skill group. Membership is the ordered list of skill ids.
// IsMaximized drives the collapse arrow + whether the rows render.
internal sealed class SkillsGroup
{
    public string Name;
    public bool IsMaximized;
    public readonly List<byte> Skills = new();

    public SkillsGroup(string name) => Name = name;

    public void Add(byte id)
    {
        if (!Skills.Contains(id)) Skills.Add(id);
    }
}

internal static class SkillsGroupDefaults
{
    // Port of SkillsGroupManager.MakeDefault* (Game/Managers/SkillsGroupManager.cs).
    // Conditional adds gate on the live skill count so older clients don't list
    // skills their table lacks. Group names match ResGeneral.* exactly.
    public static void Build(List<SkillsGroup> groups, int skillCount)
    {
        groups.Clear();

        var misc = new SkillsGroup("Miscellaneous");
        foreach (var id in new byte[] { 4, 6, 10, 12, 19, 3, 36 }) misc.Add(id);
        groups.Add(misc);

        var combat = new SkillsGroup("Combat");
        foreach (var id in new byte[] { 1, 31, 42, 17, 41, 5, 40, 27 }) combat.Add(id);
        if (skillCount > 57) combat.Add(57);
        combat.Add(43);
        if (skillCount > 50) combat.Add(50);
        if (skillCount > 51) combat.Add(51);
        if (skillCount > 52) combat.Add(52);
        if (skillCount > 53) combat.Add(53);
        groups.Add(combat);

        var trade = new SkillsGroup("Trade Skills");
        foreach (var id in new byte[] { 0, 7, 8, 11, 13, 23, 44, 45, 34, 37 }) trade.Add(id);
        groups.Add(trade);

        var magic = new SkillsGroup("Magic");
        magic.Add(16);
        if (skillCount > 56) magic.Add(56);
        magic.Add(25);
        magic.Add(46);
        if (skillCount > 55) magic.Add(55);
        magic.Add(26);
        if (skillCount > 54) magic.Add(54);
        magic.Add(32);
        if (skillCount > 49) magic.Add(49);
        groups.Add(magic);

        var wild = new SkillsGroup("Wilderness");
        foreach (var id in new byte[] { 2, 35, 18, 20, 38, 39 }) wild.Add(id);
        groups.Add(wild);

        var thief = new SkillsGroup("Thieving");
        foreach (var id in new byte[] { 14, 21, 24, 30, 48, 28, 33, 47 }) thief.Add(id);
        groups.Add(thief);

        var bard = new SkillsGroup("Bard");
        foreach (var id in new byte[] { 15, 29, 9, 22 }) bard.Add(id);
        groups.Add(bard);
    }
}
