// Spellbook content store + Magery spell table. The 0x24 (graphic 0xFFFF) open
// and the 0xBF 0x1B content packet feed SpellbookStore; SpellbookGumpPlugin
// renders the book from it. Only Magery is ported for now (the common case);
// other schools fall back to Magery layout until their tables are added.

using System.Collections.Generic;

namespace ClassicUO.Ecs;

internal struct SpellbookData
{
    public ushort Type;        // server spellbook type (1 == Magery offset table)
    public ulong Bitfields;    // 64-bit "spell present" mask (bit i => spell i+1)
}

// serial -> contents, populated by the 0xBF 0x1B observer. SpellbookGumpPlugin
// reads it when (re)building a book window.
internal sealed class SpellbookStore
{
    public readonly Dictionary<uint, SpellbookData> BySerial = new();
    public int Revision;  // bumped on content change so open books rebuild
}

// Magery spell table (64 spells). Icons on the book pages are iconStart(0x08C0)
// + (id-1); the cast/macro icon (GumpIconID) is 0x1B58 + (id-1). Names + power
// words are the canonical UO Magery list. Circle = (id-1)/8.
internal static class MageryBook
{
    public const ushort BookGraphic = 0x08AC;
    public const ushort MinimizedGraphic = 0x08BA;
    public const ushort IconStart = 0x08C0;
    public const int MaxSpells = 64;

    public static readonly string[] CircleNames =
    {
        "First Circle", "Second Circle", "Third Circle", "Fourth Circle",
        "Fifth Circle", "Sixth Circle", "Seventh Circle", "Eighth Circle",
    };

    public static readonly string[] Names =
    {
        "Clumsy", "Create Food", "Feeblemind", "Heal", "Magic Arrow", "Night Sight",
        "Reactive Armor", "Weaken", "Agility", "Cunning", "Cure", "Harm", "Magic Trap",
        "Magic Untrap", "Protection", "Strength", "Bless", "Fireball", "Magic Lock",
        "Poison", "Telekinesis", "Teleport", "Unlock", "Wall of Stone", "Arch Cure",
        "Arch Protection", "Curse", "Fire Field", "Greater Heal", "Lightning",
        "Mana Drain", "Recall", "Blade Spirits", "Dispel Field", "Incognito",
        "Magic Reflection", "Mind Blast", "Paralyze", "Poison Field", "Summon Creature",
        "Dispel", "Energy Bolt", "Explosion", "Invisibility", "Mark", "Mass Curse",
        "Paralyze Field", "Reveal", "Chain Lightning", "Energy Field", "Flamestrike",
        "Gate Travel", "Mana Vampire", "Mass Dispel", "Meteor Swarm", "Polymorph",
        "Earthquake", "Energy Vortex", "Resurrection", "Air Elemental", "Summon Daemon",
        "Earth Elemental", "Fire Elemental", "Water Elemental",
    };

    public static readonly string[] PowerWords =
    {
        "Uus Jux", "In Mani Ylem", "Rel Wis", "In Mani", "In Por Ylem", "In Lor",
        "Flam Sanct", "Des Mani", "Ex Uus", "Uus Wis", "An Nox", "An Mani", "In Jux",
        "An Jux", "Uus Sanct", "Uus Mani", "Rel Sanct", "Vas Flam", "An Por", "In Nox",
        "Ort Por Ylem", "Rel Por", "Ex Por", "In Sanct Ylem", "Vas An Nox",
        "Vas Uus Sanct", "Des Sanct", "In Flam Grav", "In Vas Mani", "Por Ort Grav",
        "Ort Rel", "Kal Ort Por", "In Jux Hur Ylem", "An Grav", "Kal In Ex",
        "In Jux Sanct", "Por Corp Wis", "An Ex Por", "In Nox Grav", "Kal Xen", "An Ort",
        "Corp Por", "Vas Ort Flam", "An Lor Xen", "Kal Por Ylem", "Vas Des Sanct",
        "In Ex Grav", "Wis Quas", "Vas Ort Grav", "In Sanct Grav", "Kal Vas Flam",
        "Vas Rel Por", "Ort Sanct", "Vas An Ort", "Flam Kal Des Ylem", "Vas Ylem Rel",
        "In Vas Por", "Vas Corp Por", "An Corp", "Kal Vas Xen Hur", "Kal Vas Xen Corp",
        "Kal Vas Xen Ylem", "Kal Vas Xen Flam", "Kal Vas Xen An Flam",
    };

    // Reagents per spell (comma-joined display names), shown on the spell page.
    public static readonly string[] Reagents =
    {
        "Blood Moss, Nightshade", "Garlic, Ginseng, Mandrake Root", "Nightshade, Ginseng", "Garlic, Ginseng, Spider's Silk",
        "Sulfurous Ash", "Spider's Silk, Sulfurous Ash", "Garlic, Spider's Silk, Sulfurous Ash", "Garlic, Nightshade",
        "Blood Moss, Mandrake Root", "Nightshade, Mandrake Root", "Garlic, Ginseng", "Nightshade, Spider's Silk",
        "Garlic, Spider's Silk, Sulfurous Ash", "Blood Moss, Sulfurous Ash", "Garlic, Ginseng, Sulfurous Ash", "Mandrake Root, Nightshade",
        "Garlic, Mandrake Root", "Black Pearl", "Blood Moss, Garlic, Sulfurous Ash", "Nightshade",
        "Blood Moss, Mandrake Root", "Blood Moss, Mandrake Root", "Blood Moss, Sulfurous Ash", "Blood Moss, Garlic",
        "Garlic, Ginseng, Mandrake Root", "Garlic, Ginseng, Mandrake Root, Sulfurous Ash", "Garlic, Nightshade, Sulfurous Ash", "Black Pearl, Spider's Silk, Sulfurous Ash",
        "Garlic, Ginseng, Mandrake Root, Spider's Silk", "Mandrake Root, Sulfurous Ash", "Black Pearl, Mandrake Root, Spider's Silk", "Black Pearl, Blood Moss, Mandrake Root",
        "Black Pearl, Mandrake Root, Nightshade", "Black Pearl, Garlic, Spider's Silk, Sulfurous Ash", "Blood Moss, Garlic, Nightshade", "Garlic, Mandrake Root, Spider's Silk",
        "Black Pearl, Mandrake Root, Nightshade, Sulfurous Ash", "Garlic, Mandrake Root, Spider's Silk", "Black Pearl, Nightshade, Spider's Silk", "Blood Moss, Mandrake Root, Spider's Silk",
        "Garlic, Mandrake Root, Sulfurous Ash", "Black Pearl, Nightshade", "Blood Moss, Mandrake Root", "Blood Moss, Nightshade",
        "Black Pearl, Blood Moss, Mandrake Root", "Garlic, Mandrake Root, Nightshade, Sulfurous Ash", "Black Pearl, Ginseng, Spider's Silk", "Blood Moss, Sulfurous Ash",
        "Black Pearl, Blood Moss, Mandrake Root, Sulfurous Ash", "Black Pearl, Mandrake Root, Spider's Silk, Sulfurous Ash", "Spider's Silk, Sulfurous Ash", "Black Pearl, Mandrake Root, Sulfurous Ash",
        "Black Pearl, Blood Moss, Mandrake Root, Spider's Silk", "Black Pearl, Garlic, Mandrake Root, Sulfurous Ash", "Blood Moss, Mandrake Root, Spider's Silk, Sulfurous Ash", "Blood Moss, Mandrake Root, Spider's Silk",
        "Blood Moss, Ginseng, Mandrake Root, Sulfurous Ash", "Black Pearl, Blood Moss, Mandrake Root, Nightshade", "Blood Moss, Ginseng, Garlic", "Blood Moss, Mandrake Root, Spider's Silk",
        "Blood Moss, Mandrake Root, Spider's Silk, Sulfurous Ash", "Blood Moss, Mandrake Root, Spider's Silk", "Blood Moss, Mandrake Root, Spider's Silk, Sulfurous Ash", "Blood Moss, Mandrake Root, Spider's Silk",
    };

    public static string Name(int id) => id >= 1 && id <= MaxSpells ? Names[id - 1] : "?";
    public static string PowerWord(int id) => id >= 1 && id <= MaxSpells ? PowerWords[id - 1] : "";
    public static string Reagent(int id) => id >= 1 && id <= MaxSpells ? Reagents[id - 1] : "";
    public static ushort Icon(int id) => (ushort)(IconStart + (id - 1));
    public static int Circle(int id) => (id - 1) / 8;
}
