#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    public enum Ability : ushort
    {
        Invalid = 0xFF,
        None = 0,
        ArmorIgnore = 1,
        BleedAttack = 2,
        ConcussionBlow = 3,
        CrushingBlow = 4,
        Disarm = 5,
        Dismount = 6,
        DoubleStrike = 7,
        InfectiousStrike = 8,
        MortalStrike = 9,
        MovingShot = 10,
        ParalyzingBlow = 11,
        ShadowStrike = 12,
        WhirlwindAttack = 13,
        RidingSwipe = 14,
        FrenziedWhirlwind = 15,
        Block = 16,
        DefenseMastery = 17,
        NerveStrike = 18,
        TalonStrike = 19,
        Feint = 20,
        DualWield = 21,
        DoubleShot = 22,
        ArmorPierce = 23,
        Bladeweave = 24,
        ForceArrow = 25,
        LightningArrow = 26,
        PsychicAttack = 27,
        SerpentArrow = 28,
        ForceOfNature = 29,
        InfusedThrow = 30,
        MysticArc = 31
    }

    internal readonly struct AbilityDefinition
    {
        public AbilityDefinition(int index, string name, ushort icon)
        {
            Index = index;
            Name = name;
            Icon = icon;
        }

        public readonly int Index;
        public readonly string Name;
        public readonly ushort Icon;
    }

    internal static class AbilityData
    {
        public static readonly AbilityDefinition[] Abilities = new AbilityDefinition[Constants.MAX_ABILITIES_COUNT]
        {
            new AbilityDefinition(1, "Armor Ignore", 0x5200),
            new AbilityDefinition(2, "Bleed Attack", 0x5201),
            new AbilityDefinition(3, "Concussion Blow", 0x5202),
            new AbilityDefinition(4, "Crushing Blow", 0x5203),
            new AbilityDefinition(5, "Disarm", 0x5204),
            new AbilityDefinition(6, "Dismount", 0x5205),
            new AbilityDefinition(7, "Double Strike", 0x5206),
            new AbilityDefinition(8, "Infecting", 0x5207),
            new AbilityDefinition(9, "Mortal Strike", 0x5208),
            new AbilityDefinition(10, "Moving Shot", 0x5209),
            new AbilityDefinition(11, "Paralyzing Blow", 0x520A),
            new AbilityDefinition(12, "Shadow Strike", 0x520B),
            new AbilityDefinition(13, "Whirlwind Attack", 0x520C),
            new AbilityDefinition(14, "Riding Swipe", 0x520D),
            new AbilityDefinition(15, "Frenzied Whirlwind", 0x520E),
            new AbilityDefinition(16, "Block", 0x520F),
            new AbilityDefinition(17, "Defense Mastery", 0x5210),
            new AbilityDefinition(18, "Nerve Strike", 0x5211),
            new AbilityDefinition(19, "Talon Strike", 0x5212),
            new AbilityDefinition(20, "Feint", 0x5213),
            new AbilityDefinition(21, "Dual Wield", 0x5214),
            new AbilityDefinition(22, "Double Shot", 0x5215),
            new AbilityDefinition(23, "Armor Pierce", 0x5216),
            new AbilityDefinition(24, "Bladeweave", 0x5217),
            new AbilityDefinition(25, "Force Arrow", 0x5218),
            new AbilityDefinition(26, "Lightning Arrow", 0x5219),
            new AbilityDefinition(27, "Psychic Attack", 0x521A),
            new AbilityDefinition(28, "Serpent Arrow", 0x521B),
            new AbilityDefinition(29, "Force of Nature", 0x521C),
            new AbilityDefinition(30, "Infused Throw", 0x521D),
            new AbilityDefinition(31, "Mystic Arc", 0x521E),
            new AbilityDefinition(32, "Disrobe", 0x521F)
        };
    }
}