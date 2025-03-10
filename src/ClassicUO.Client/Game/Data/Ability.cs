// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum Ability : ushort
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

    internal readonly record struct AbilityDefinition(int Index, string Name, ushort Icon);

    internal readonly record struct ItemAbilities(ushort Graphic, Ability First, Ability Second)
    {
        public void Set(Ability[] abilities)
        {
            abilities[0] = First;
            abilities[1] = Second;
        }
    }

    internal static class AbilityData
    {
        public static readonly AbilityDefinition[] Abilities =
        [
            new(1, "Armor Ignore", 0x5200),
            new(2, "Bleed Attack", 0x5201),
            new(3, "Concussion Blow", 0x5202),
            new(4, "Crushing Blow", 0x5203),
            new(5, "Disarm", 0x5204),
            new(6, "Dismount", 0x5205),
            new(7, "Double Strike", 0x5206),
            new(8, "Infecting", 0x5207),
            new(9, "Mortal Strike", 0x5208),
            new(10, "Moving Shot", 0x5209),
            new(11, "Paralyzing Blow", 0x520A),
            new(12, "Shadow Strike", 0x520B),
            new(13, "Whirlwind Attack", 0x520C),
            new(14, "Riding Swipe", 0x520D),
            new(15, "Frenzied Whirlwind", 0x520E),
            new(16, "Block", 0x520F),
            new(17, "Defense Mastery", 0x5210),
            new(18, "Nerve Strike", 0x5211),
            new(19, "Talon Strike", 0x5212),
            new(20, "Feint", 0x5213),
            new(21, "Dual Wield", 0x5214),
            new(22, "Double Shot", 0x5215),
            new(23, "Armor Pierce", 0x5216),
            new(24, "Bladeweave", 0x5217),
            new(25, "Force Arrow", 0x5218),
            new(26, "Lightning Arrow", 0x5219),
            new(27, "Psychic Attack", 0x521A),
            new(28, "Serpent Arrow", 0x521B),
            new(29, "Force of Nature", 0x521C),
            new(30, "Infused Throw", 0x521D),
            new(31, "Mystic Arc", 0x521E),
            new(32, "Disrobe", 0x521F)
        ];


        public static readonly ItemAbilities DefaultItemAbilities = new(0, Ability.ParalyzingBlow, Ability.Disarm);

        public static readonly Dictionary<ushort, ItemAbilities> GraphicToAbilitiesMap = new()
        {
            {0, DefaultItemAbilities},
            { 0x0901, new (0x0901, Ability.MovingShot, Ability.InfusedThrow) },        // Gargish Cyclone
            { 0x0902, new (0x0902, Ability.InfectiousStrike, Ability.ShadowStrike) },  // Gargish Dagger
            { 0x0905, new (0x0905, Ability.DoubleStrike, Ability.MortalStrike) },      // Glass Staff
            { 0x0906, new (0x0906, Ability.CrushingBlow, Ability.Dismount) },          // serpentstone staff
            { 0x090C, new (0x090C, Ability.BleedAttack, Ability.MortalStrike) },       // Glass Sword
            { 0x0DF0, new (0x0DF0, Ability.WhirlwindAttack, Ability.ParalyzingBlow) }, // Black Staves
            { 0x0DF1, new (0x0DF1, Ability.WhirlwindAttack, Ability.ParalyzingBlow) },
            { 0x0DF2, new (0x0DF2, Ability.Dismount, Ability.Disarm) },
            { 0x0DF3, new (0x0DF3, Ability.Dismount, Ability.Disarm) },
            { 0x0DF4, new (0x0DF4, Ability.Dismount, Ability.Disarm) },
            { 0x0DF5, new (0x0DF5, Ability.Dismount, Ability.Disarm) }, // Wands BookType A-D
            { 0x0E81, new (0x0E81, Ability.CrushingBlow, Ability.Disarm) },
            { 0x0E82, new (0x0E82, Ability.CrushingBlow, Ability.Disarm) }, // Shepherd's Crooks
            { 0x0E85, new (0x0E85, Ability.DoubleStrike, Ability.Disarm) },
            { 0x0E86, new (0x0E86, Ability.DoubleStrike, Ability.Disarm) }, // Pickaxes
            { 0x0E87, new (0x0E87, Ability.BleedAttack, Ability.Dismount) },
            { 0x0E88, new (0x0E88, Ability.BleedAttack, Ability.Dismount) }, // Pitchforks
            { 0x0E89, new (0x0E89, Ability.DoubleStrike, Ability.ConcussionBlow) },
            { 0x0E8A, new (0x0E8A, Ability.DoubleStrike, Ability.ConcussionBlow) }, // Quarter Staves
            { 0x0EC2, new (0x0EC2, Ability.BleedAttack, Ability.InfectiousStrike) },
            { 0x0EC3, new (0x0EC3, Ability.BleedAttack, Ability.InfectiousStrike) }, // Cleavers
            { 0x0EC4, new (0x0EC4, Ability.ShadowStrike, Ability.BleedAttack) },
            { 0x0EC5, new (0x0EC5, Ability.ShadowStrike, Ability.BleedAttack) }, // Skinning Knives
            { 0x0F43, new (0x0F43, Ability.ArmorIgnore, Ability.Disarm) },
            { 0x0F44, new (0x0F44, Ability.ArmorIgnore, Ability.Disarm) }, // Hatchets
            { 0x0F45, new (0x0F45, Ability.BleedAttack, Ability.MortalStrike) },
            { 0x0F46, new (0x0F46, Ability.BleedAttack, Ability.MortalStrike) }, // Executioner Axes
            { 0x0F47, new (0x0F47, Ability.BleedAttack, Ability.ConcussionBlow) },
            { 0x0F48, new (0x0F48, Ability.BleedAttack, Ability.ConcussionBlow) }, // Battle Axes
            { 0x0F49, new (0x0F49, Ability.CrushingBlow, Ability.Dismount) },
            { 0x0F4A, new (0x0F4A, Ability.CrushingBlow, Ability.Dismount) },        // Axes
            { 0x0F4B, new (0x0F4B, Ability.DoubleStrike, Ability.WhirlwindAttack) },
            { 0x0F4C, new (0x0F4C, Ability.DoubleStrike, Ability.WhirlwindAttack) }, // Double Axe
            { 0x0F4D, new (0x0F4D, Ability.ParalyzingBlow, Ability.Dismount) },
            { 0x0F4E, new (0x0F4E, Ability.ParalyzingBlow, Ability.Dismount) }, // Bardiches
            { 0x0F4F, new (0x0F4F, Ability.ConcussionBlow, Ability.MortalStrike) },
            { 0x0F50, new (0x0F50, Ability.ConcussionBlow, Ability.MortalStrike) }, // Crossbows
            { 0x0F51, new (0x0F51, Ability.InfectiousStrike, Ability.ShadowStrike) },
            { 0x0F52, new (0x0F52, Ability.InfectiousStrike, Ability.ShadowStrike) }, // Daggers
            { 0x0F5C, new (0x0F5C, Ability.ConcussionBlow, Ability.Disarm) },
            { 0x0F5D, new (0x0F5D, Ability.ConcussionBlow, Ability.Disarm) }, // Maces
            { 0x0F5E, new (0x0F5E, Ability.CrushingBlow, Ability.ArmorIgnore) },
            { 0x0F5F, new (0x0F5F, Ability.CrushingBlow, Ability.ArmorIgnore) }, // Broadswords
            { 0x0F60, new (0x0F60, Ability.ArmorIgnore, Ability.ConcussionBlow) },
            { 0x0F61, new (0x0F61, Ability.ArmorIgnore, Ability.ConcussionBlow) }, // Longswords
            { 0x0F62, new (0x0F62, Ability.ArmorIgnore, Ability.ParalyzingBlow) },
            { 0x0F63, new (0x0F63, Ability.ArmorIgnore, Ability.ParalyzingBlow) }, // Spears
            { 0x0FB5, new (0x0FB5, Ability.CrushingBlow, Ability.ShadowStrike) },
            { 0x13AF, new (0x13AF, Ability.ArmorIgnore, Ability.BleedAttack) },
            { 0x13B0, new (0x13B0, Ability.ArmorIgnore, Ability.BleedAttack) }, // War Axes
            { 0x13B1, new (0x13B1, Ability.ParalyzingBlow, Ability.MortalStrike) },
            { 0x13B2, new (0x13B2, Ability.ParalyzingBlow, Ability.MortalStrike) }, // Bows
            { 0x13B3, new (0x13B3, Ability.ShadowStrike, Ability.Dismount) },
            { 0x13B4, new (0x13B4, Ability.ShadowStrike, Ability.Dismount) }, // Clubs
            { 0x13B6, new (0x13B6, Ability.DoubleStrike, Ability.ParalyzingBlow) },
            { 0x13B7, new (0x13B7, Ability.DoubleStrike, Ability.ParalyzingBlow) },
            { 0x13B8, new (0x13B8, Ability.DoubleStrike, Ability.ParalyzingBlow) }, // Scimitars
            { 0x13B9, new (0x13B9, Ability.ParalyzingBlow, Ability.CrushingBlow) },
            { 0x13BA, new (0x13BA, Ability.ParalyzingBlow, Ability.CrushingBlow) },  // Viking Swords
            { 0x13FD, new (0x13FD, Ability.MovingShot, Ability.Dismount) },          // Heavy Crossbows
            { 0x13E3, new (0x13E3, Ability.CrushingBlow, Ability.ShadowStrike) },    // Smith's Hammers
            { 0x13F6, new (0x13F6, Ability.InfectiousStrike, Ability.Disarm) },      // Butcher Knives
            { 0x13F8, new (0x13F8, Ability.ConcussionBlow, Ability.ForceOfNature) }, // Gnarled Staves
            { 0x13FB, new (0x13FB, Ability.WhirlwindAttack, Ability.BleedAttack) },  // Large Battle Axes
            { 0x13FF, new (0x13FF, Ability.DoubleStrike, Ability.ArmorIgnore) },     // Katana
            { 0x1401, new (0x1401, Ability.ArmorIgnore, Ability.InfectiousStrike) }, // Kryss
            { 0x1402, new (0x1402, Ability.ShadowStrike, Ability.MortalStrike) },
            { 0x1403, new (0x1403, Ability.ShadowStrike, Ability.MortalStrike) }, // Short Spears
            { 0x1404, new (0x1404, Ability.BleedAttack, Ability.Disarm) },
            { 0x1405, new (0x1405, Ability.BleedAttack, Ability.Disarm) }, // War Forks
            { 0x1406, new (0x1406, Ability.CrushingBlow, Ability.MortalStrike) },
            { 0x1407, new (0x1407, Ability.CrushingBlow, Ability.MortalStrike) }, // War Maces
            { 0x1438, new (0x1438, Ability.WhirlwindAttack, Ability.CrushingBlow) },
            { 0x1439, new (0x1439, Ability.WhirlwindAttack, Ability.CrushingBlow) }, // War Hammers
            { 0x143A, new (0x143A, Ability.DoubleStrike, Ability.ConcussionBlow) },
            { 0x143B, new (0x143B, Ability.DoubleStrike, Ability.ConcussionBlow) }, // Mauls
            { 0x143C, new (0x143C, Ability.ArmorIgnore, Ability.MortalStrike) },
            { 0x143D, new (0x143D, Ability.ArmorIgnore, Ability.MortalStrike) }, // Hammer Picks
            { 0x143E, new (0x143E, Ability.WhirlwindAttack, Ability.ConcussionBlow) },
            { 0x143F, new (0x143F, Ability.WhirlwindAttack, Ability.ConcussionBlow) }, // Halberds
            { 0x1440, new (0x1440, Ability.BleedAttack, Ability.ShadowStrike) },
            { 0x1441, new (0x1441, Ability.BleedAttack, Ability.ShadowStrike) }, // Cutlasses
            { 0x1442, new (0x1442, Ability.DoubleStrike, Ability.ShadowStrike) },
            { 0x1443, new (0x1443, Ability.DoubleStrike, Ability.ShadowStrike) },       // Two-Handed Axes
            { 0x26BA, new (0x26BA, Ability.BleedAttack, Ability.ParalyzingBlow) },      // Scythes
            { 0x26BB, new (0x26BB, Ability.ParalyzingBlow, Ability.MortalStrike) },     // Bone Harvesters
            { 0x26BC, new (0x26BC, Ability.CrushingBlow, Ability.MortalStrike) },       // Scepters
            { 0x26BD, new (0x26BD, Ability.ArmorIgnore, Ability.Dismount) },            // Bladed Staves
            { 0x26BE, new (0x26BE, Ability.ParalyzingBlow, Ability.InfectiousStrike) }, // Pikes
            { 0x26BF, new (0x26BF, Ability.DoubleStrike, Ability.InfectiousStrike) },   // Double Bladed Staff
            { 0x26C0, new (0x26C0, Ability.Dismount, Ability.ConcussionBlow) },         // Lances
            { 0x26C1, new (0x26C1, Ability.DoubleStrike, Ability.MortalStrike) },       // Crescent Blades
            { 0x26C2, new (0x26C2, Ability.ArmorIgnore, Ability.MovingShot) },          // Composite Bows
            { 0x26C3, new (0x26C3, Ability.DoubleStrike, Ability.MovingShot) },         // Repeating Crossbows
            { 0x26C4, new (0x26C4, Ability.BleedAttack, Ability.ParalyzingBlow) },      // also Scythes
            { 0x26C5, new (0x26C5, Ability.ParalyzingBlow, Ability.MortalStrike) },     // also Bone Harvesters
            { 0x26C6, new (0x26C6, Ability.CrushingBlow, Ability.MortalStrike) },       // also Scepters
            { 0x26C7, new (0x26C7, Ability.ArmorIgnore, Ability.Dismount) },            // also Bladed Staves
            { 0x26C8, new (0x26C8, Ability.ParalyzingBlow, Ability.InfectiousStrike) }, // also Pikes
            { 0x26C9, new (0x26C9, Ability.DoubleStrike, Ability.InfectiousStrike) },   // also Double Bladed Staff
            { 0x26CA, new (0x26CA, Ability.Dismount, Ability.ConcussionBlow) },         // also Lances
            { 0x26CB, new (0x26CB, Ability.DoubleStrike, Ability.MortalStrike) },       // also Crescent Blades
            { 0x26CC, new (0x26CC, Ability.ArmorIgnore, Ability.MovingShot) },          // also Composite Bows
            { 0x26CD, new (0x26CD, Ability.DoubleStrike, Ability.MovingShot) },         // also Repeating Crossbows
            { 0x26CE, new (0x26CE, Ability.WhirlwindAttack, Ability.Disarm) },
            { 0x26CF, new (0x26CF, Ability.WhirlwindAttack, Ability.Disarm) },           // paladin sword
            { 0x27A2, new (0x27A2, Ability.CrushingBlow, Ability.RidingSwipe) },         // No-Dachi
            { 0x27A3, new (0x27A3, Ability.Feint, Ability.Block) },                      // Tessen
            { 0x27A4, new (0x27A4, Ability.FrenziedWhirlwind, Ability.DoubleStrike) },   // Wakizashi
            { 0x27A5, new (0x27A5, Ability.ArmorPierce, Ability.DoubleShot) },           // Yumi
            { 0x27A6, new (0x27A6, Ability.FrenziedWhirlwind, Ability.CrushingBlow) },   // Tetsubo
            { 0x27A7, new (0x27A7, Ability.DefenseMastery, Ability.FrenziedWhirlwind) }, // Lajatang
            { 0x27A8, new (0x27A8, Ability.Feint, Ability.NerveStrike) },                // Bokuto
            { 0x27A9, new (0x27A9, Ability.Feint, Ability.DoubleStrike) },               // Daisho
            { 0x27AA, new (0x27AA, Ability.Disarm, Ability.ParalyzingBlow) },            // Fukya
            { 0x27AB, new (0x27AB, Ability.DualWield, Ability.TalonStrike) },            // Tekagi
            { 0x27AD, new (0x27AD, Ability.WhirlwindAttack, Ability.DefenseMastery) },   // Kama
            { 0x27AE, new (0x27AE, Ability.Block, Ability.Feint) },                      // Nunchaku
            { 0x27AF, new (0x27AF, Ability.Block, Ability.ArmorPierce) },                // Sai
            { 0x27ED, new (0x27ED, Ability.CrushingBlow, Ability.RidingSwipe) },         // also No-Dachi
            { 0x27EE, new (0x27EE, Ability.Feint, Ability.Block) },                      // also Tessen
            { 0x27EF, new (0x27EF, Ability.FrenziedWhirlwind, Ability.DoubleStrike) },   // also Wakizashi
            { 0x27F0, new (0x27F0, Ability.ArmorPierce, Ability.DoubleShot) },           // also Yumi
            { 0x27F1, new (0x27F1, Ability.FrenziedWhirlwind, Ability.CrushingBlow) },   // also Tetsubo
            { 0x27F2, new (0x27F2, Ability.DefenseMastery, Ability.FrenziedWhirlwind) }, // also Lajatang
            { 0x27F3, new (0x27F3, Ability.Feint, Ability.NerveStrike) },                // also Bokuto
            { 0x27F4, new (0x27F4, Ability.Feint, Ability.DoubleStrike) },               // also Daisho
            { 0x27F5, new (0x27F5, Ability.Disarm, Ability.ParalyzingBlow) },            // also Fukya
            { 0x27F6, new (0x27F6, Ability.DualWield, Ability.TalonStrike) },            // also Tekagi
            { 0x27F8, new (0x27F8, Ability.WhirlwindAttack, Ability.DefenseMastery) },   // Kama
            { 0x27F9, new (0x27F9, Ability.Block, Ability.Feint) },                      // Nunchaku
            { 0x27FA, new (0x27FA, Ability.Block, Ability.ArmorPierce) },                // Sai
            { 0x2D1E, new (0x2D1E, Ability.ForceArrow, Ability.SerpentArrow) },          // Elven Composite Longbows
            { 0x2D1F, new (0x2D1F, Ability.LightningArrow, Ability.PsychicAttack) },     // Magical Shortbows
            { 0x2D20, new (0x2D20, Ability.PsychicAttack, Ability.BleedAttack) },        // Elven Spellblades
            { 0x2D21, new (0x2D21, Ability.InfectiousStrike, Ability.ShadowStrike) },    // Assassin Spikes
            { 0x2D22, new (0x2D22, Ability.Feint, Ability.ArmorIgnore) },                // Leafblades
            { 0x2D23, new (0x2D23, Ability.Disarm, Ability.Bladeweave) },                // War Cleavers
            { 0x2D24, new (0x2D24, Ability.ConcussionBlow, Ability.CrushingBlow) },      // Diamond Maces
            { 0x2D25, new (0x2D25, Ability.Block, Ability.ForceOfNature) },              // Wild Staves
            { 0x2D26, new (0x2D26, Ability.Disarm, Ability.Bladeweave) },                // Rune Blades
            { 0x2D27, new (0x2D27, Ability.WhirlwindAttack, Ability.Bladeweave) },       // Radiant Scimitars
            { 0x2D28, new (0x2D28, Ability.Disarm, Ability.CrushingBlow) },              // Ornate Axes
            { 0x2D29, new (0x2D29, Ability.DefenseMastery, Ability.Bladeweave) },        // Elven Machetes
            { 0x2D2A, new (0x2D2A, Ability.ForceArrow, Ability.SerpentArrow) },          // also Elven Composite Longbows
            { 0x2D2B, new (0x2D2B, Ability.LightningArrow, Ability.PsychicAttack) },     // also Magical Shortbows
            { 0x2D2C, new (0x2D2C, Ability.PsychicAttack, Ability.BleedAttack) },        // also Elven Spellblades
            { 0x2D2D, new (0x2D2D, Ability.InfectiousStrike, Ability.ShadowStrike) },    // also Assassin Spikes
            { 0x2D2E, new (0x2D2E, Ability.Feint, Ability.ArmorIgnore) },                // also Leafblades
            { 0x2D2F, new (0x2D2F, Ability.Disarm, Ability.Bladeweave) },                // also War Cleavers
            { 0x2D30, new (0x2D30, Ability.ConcussionBlow, Ability.CrushingBlow) },      // also Diamond Maces
            { 0x2D31, new (0x2D31, Ability.Block, Ability.ForceOfNature) },              // also Wild Staves
            { 0x2D32, new (0x2D32, Ability.Disarm, Ability.Bladeweave) },                // also Rune Blades
            { 0x2D33, new (0x2D33, Ability.WhirlwindAttack, Ability.Bladeweave) },       // also Radiant Scimitars
            { 0x2D34, new (0x2D34, Ability.Disarm, Ability.CrushingBlow) },              // also Ornate Axes
            { 0x2D35, new (0x2D35, Ability.DefenseMastery, Ability.Bladeweave) },        // also Elven Machetes
            { 0x4067, new (0x4067, Ability.MysticArc, Ability.ConcussionBlow) },         // Boomerang
            { 0x08FD, new (0x08FD, Ability.DoubleStrike, Ability.InfectiousStrike) },
            { 0x4068, new (0x4068, Ability.DoubleStrike, Ability.InfectiousStrike) }, // Dual Short Axes
            { 0x406B, new (0x406B, Ability.ArmorIgnore, Ability.MortalStrike) },      // Soul Glaive
            { 0x406C, new (0x406C, Ability.MovingShot, Ability.InfusedThrow) },       // Cyclone
            { 0x0904, new (0x0904, Ability.DoubleStrike, Ability.Disarm) },
            { 0x406D, new (0x406D, Ability.DoubleStrike, Ability.Disarm) }, // Dual Pointed Spear
            { 0x0903, new (0x0903, Ability.ArmorIgnore, Ability.Disarm) },
            { 0x406E, new (0x406E, Ability.ArmorIgnore, Ability.Disarm) },  // Disc Mace
            { 0x08FE, new (0x08FE, Ability.BleedAttack, Ability.ParalyzingBlow) },
            { 0x4072, new (0x4072, Ability.BleedAttack, Ability.ParalyzingBlow) }, // Blood Blade
            { 0x090B, new (0x090B, Ability.CrushingBlow, Ability.ConcussionBlow) },
            { 0x4074, new (0x4074, Ability.CrushingBlow, Ability.ConcussionBlow) }, // Dread Sword
            { 0x0908, new (0x0908, Ability.WhirlwindAttack, Ability.Dismount) },
            { 0x4075, new (0x4075, Ability.WhirlwindAttack, Ability.Dismount) }, // Gargish Talwar
            { 0x4076, new (0x4076, Ability.ArmorIgnore, Ability.MortalStrike) }, // Shortblade
            { 0x48AE, new (0x48AE, Ability.BleedAttack, Ability.InfectiousStrike) },
            { 0x48B0, new (0x48B0, Ability.BleedAttack, Ability.ConcussionBlow) }, // Gargish Battle Axe
            { 0x48B3, new (0x48B3, Ability.CrushingBlow, Ability.Dismount) },
            { 0x48B2, new (0x48B2, Ability.CrushingBlow, Ability.Dismount) }, // Gargish Axe
            { 0x48B5, new (0x48B5, Ability.ParalyzingBlow, Ability.Dismount) },
            { 0x48B4, new (0x48B4, Ability.ParalyzingBlow, Ability.Dismount) }, // Gargish Bardiche
            { 0x48B7, new (0x48B7, Ability.InfectiousStrike, Ability.Disarm) },
            { 0x48B6, new (0x48B6, Ability.InfectiousStrike, Ability.Disarm) }, // Gargish Butcher Knife
            { 0x48B9, new (0x48B9, Ability.ConcussionBlow, Ability.ParalyzingBlow) },
            { 0x48B8, new (0x48B8, Ability.ConcussionBlow, Ability.ParalyzingBlow) }, // Gargish Gnarled Staff
            { 0x48BB, new (0x48BB, Ability.DoubleStrike, Ability.ArmorIgnore) },
            { 0x48BA, new (0x48BA, Ability.DoubleStrike, Ability.ArmorIgnore) }, // Gargish Katana
            { 0x48BD, new (0x48BD, Ability.ArmorIgnore, Ability.InfectiousStrike) },
            { 0x48BC, new (0x48BC, Ability.ArmorIgnore, Ability.InfectiousStrike) }, // Gargish Kryss
            { 0x48BF, new (0x48BF, Ability.BleedAttack, Ability.Disarm) },
            { 0x48BE, new (0x48BE, Ability.BleedAttack, Ability.Disarm) }, // Gargish War Fork
            { 0x48CB, new (0x48CB, Ability.Dismount, Ability.ConcussionBlow) },
            { 0x48CA, new (0x48CA, Ability.Dismount, Ability.ConcussionBlow) },  // Gargish Lance
            { 0x0481, new (0x0481, Ability.WhirlwindAttack, Ability.CrushingBlow) },
            { 0x48C0, new (0x48C0, Ability.WhirlwindAttack, Ability.CrushingBlow) }, // Gargish War Hammer
            { 0x48C3, new (0x48C3, Ability.DoubleStrike, Ability.ConcussionBlow) },
            { 0x48C2, new (0x48C2, Ability.DoubleStrike, Ability.ConcussionBlow) }, // Gargish Maul
            { 0x48C5, new (0x48C5, Ability.BleedAttack, Ability.ParalyzingBlow) },
            { 0x48C4, new (0x48C4, Ability.BleedAttack, Ability.ParalyzingBlow) }, // Gargish Scyte
            { 0x48C7, new (0x48C7, Ability.ParalyzingBlow, Ability.MortalStrike) },
            { 0x48C6, new (0x48C6, Ability.ParalyzingBlow, Ability.MortalStrike) }, // Gargish Bone Harvester
            { 0x48C9, new (0x48C9, Ability.ParalyzingBlow, Ability.InfectiousStrike) },
            { 0x48C8, new (0x48C8, Ability.ParalyzingBlow, Ability.InfectiousStrike) },  // Gargish Pike
            { 0x48CC, new (0x48CC, Ability.Feint, Ability.Block) },
            { 0x48CD, new (0x48CD, Ability.Feint, Ability.Block) }, // Gargish Tessen
            { 0x48CF, new (0x48CF, Ability.DualWield, Ability.TalonStrike) },
            { 0x48CE, new (0x48CE, Ability.DualWield, Ability.TalonStrike) }, // Gargish Tekagi
            { 0x48D1, new (0x48D1, Ability.Feint, Ability.DoubleStrike) },
            { 0x48D0, new (0x48D0, Ability.Feint, Ability.DoubleStrike) },             // Gargish Daisho
            { 0xA289, new (0xA289, Ability.ConcussionBlow, Ability.WhirlwindAttack) }, // Barbed Whip
            { 0xA28A, new (0xA28A, Ability.ArmorPierce, Ability.WhirlwindAttack) },    // Spiked Whip
            { 0xA28B, new (0xA28B, Ability.BleedAttack, Ability.WhirlwindAttack) },    // Bladed Whip
            { 0x08FF, new (0x08FF, Ability.MysticArc, Ability.ConcussionBlow) },       // Boomerang
            { 0x0900, new (0x0900, Ability.ArmorIgnore, Ability.ParalyzingBlow) },     // Stone War Sword
            { 0x090A, new (0x090A, Ability.ArmorIgnore, Ability.MortalStrike) },       // Soul Glaive
            { 0xAEA5, new (0xAEA5, Ability.DoubleStrike, Ability.ArmorIgnore) },
            { 0xAEB4, new (0xAEB4, Ability.DoubleStrike, Ability.ArmorIgnore) },
            { 0xAEC3, new (0xAEC3, Ability.DoubleStrike, Ability.ArmorIgnore) },
            { 0xAED2, new (0xAED2, Ability.DoubleStrike, Ability.ArmorIgnore) },// Publish 119 Paladin War Forks
            { 0xAEA4, new (0xAEA4, Ability.DoubleStrike, Ability.WhirlwindAttack) },
            { 0xAEB3, new (0xAEB3, Ability.DoubleStrike, Ability.WhirlwindAttack) },
            { 0xAEC2, new (0xAEC2, Ability.DoubleStrike, Ability.WhirlwindAttack) },
            { 0xAED1, new (0xAED1, Ability.DoubleStrike, Ability.WhirlwindAttack) }, // Publish 119 Paladin War Hammers
        };
    }
}
