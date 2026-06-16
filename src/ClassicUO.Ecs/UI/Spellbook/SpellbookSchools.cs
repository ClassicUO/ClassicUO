// Per-school spellbook descriptors. The gump renders any school from one of
// these: book graphic, per-spell names / power words / reagents / icons, and
// the full cast index sent to the server (Send_CastSpell). The school is
// resolved from the spellbook ITEM graphic (the 0xBF 0x1B content packet's
// graphic field) exactly like legacy SpellbookGump.AssignGraphic — the packet's
// "type" word is ignored. Magery reuses the MageryBook tables; the other schools
// pull from the *SpellData classes ported from ClassicUO.Client/Game/Data.

using System;
using System.Collections.Generic;

namespace ClassicUO.Ecs;

// Mirrors legacy ClassicUO.Game.Data.SpellBookType (not in the ECS stub tree).
internal enum SpellBookType
{
    Magery,
    Necromancy,
    Chivalry,
    Bushido = 4,
    Ninjitsu,
    Spellweaving,
    Mysticism,
    Mastery,
    Unknown = 0xFF,
}

internal sealed class SpellSchool
{
    public SpellBookType Type;
    public ushort BookGraphic;
    public ushort MinimizedGraphic;
    public int MaxSpells;
    public string[] Names;
    public string[] PowerWords;
    public string[] Reagents;     // display strings, "" when the spell has none
    public int[] Ids;             // full server cast index (Send_CastSpell)
    public int[] IconIds;         // gump graphic per spell
    public int[] ManaCost;
    public int[] MinSkill;
    public int[] TithingCost;
    public bool HasCircles;       // Magery: 8 circle buttons + circle-name headers
    public bool ShowReagents;     // Magery, Necromancy, Mysticism, Mastery
    public bool ShowRequires;     // non-Magery: mana / min-skill (+ tithing) line
    public string[] CircleNames = Array.Empty<string>();

    public string Name(int localId) => Names[localId - 1];
    public string PowerWord(int localId) => PowerWords[localId - 1];
    public string Reagent(int localId) => Reagents[localId - 1];
    public int CastId(int localId) => Ids[localId - 1];
    public ushort Icon(int localId) => (ushort)IconIds[localId - 1];
    public int Circle(int localId) => (localId - 1) / 8;
}

internal static class SpellSchools
{
    private static readonly Dictionary<SpellBookType, SpellSchool> _byType = new();

    static SpellSchools()
    {
        // Magery — built from the existing MageryBook tables (ids 1..64,
        // icons sequential from IconStart).
        var mageryIds = new int[MageryBook.MaxSpells];
        var mageryIcons = new int[MageryBook.MaxSpells];
        for (int i = 0; i < MageryBook.MaxSpells; i++)
        {
            mageryIds[i] = i + 1;
            mageryIcons[i] = MageryBook.IconStart + i;
        }
        _byType[SpellBookType.Magery] = new SpellSchool
        {
            Type = SpellBookType.Magery,
            BookGraphic = MageryBook.BookGraphic,
            MinimizedGraphic = MageryBook.MinimizedGraphic,
            MaxSpells = MageryBook.MaxSpells,
            Names = MageryBook.Names,
            PowerWords = MageryBook.PowerWords,
            Reagents = MageryBook.Reagents,
            Ids = mageryIds,
            IconIds = mageryIcons,
            ManaCost = new int[MageryBook.MaxSpells],
            MinSkill = new int[MageryBook.MaxSpells],
            TithingCost = new int[MageryBook.MaxSpells],
            HasCircles = true,
            ShowReagents = true,
            ShowRequires = false,
            CircleNames = MageryBook.CircleNames,
        };

        _byType[SpellBookType.Necromancy] = Std(SpellBookType.Necromancy, 0x2B00, 0x2B03,
            NecromancySpellData.Names, NecromancySpellData.PowerWords, NecromancySpellData.Reagents,
            NecromancySpellData.Ids, NecromancySpellData.IconIds, NecromancySpellData.ManaCost,
            NecromancySpellData.MinSkill, NecromancySpellData.TithingCost, showReagents: true);

        _byType[SpellBookType.Chivalry] = Std(SpellBookType.Chivalry, 0x2B01, 0x2B04,
            ChivalrySpellData.Names, ChivalrySpellData.PowerWords, ChivalrySpellData.Reagents,
            ChivalrySpellData.Ids, ChivalrySpellData.IconIds, ChivalrySpellData.ManaCost,
            ChivalrySpellData.MinSkill, ChivalrySpellData.TithingCost, showReagents: false);

        _byType[SpellBookType.Bushido] = Std(SpellBookType.Bushido, 0x2B07, 0x2B09,
            BushidoSpellData.Names, BushidoSpellData.PowerWords, BushidoSpellData.Reagents,
            BushidoSpellData.Ids, BushidoSpellData.IconIds, BushidoSpellData.ManaCost,
            BushidoSpellData.MinSkill, BushidoSpellData.TithingCost, showReagents: false);

        _byType[SpellBookType.Ninjitsu] = Std(SpellBookType.Ninjitsu, 0x2B06, 0x2B08,
            NinjitsuSpellData.Names, NinjitsuSpellData.PowerWords, NinjitsuSpellData.Reagents,
            NinjitsuSpellData.Ids, NinjitsuSpellData.IconIds, NinjitsuSpellData.ManaCost,
            NinjitsuSpellData.MinSkill, NinjitsuSpellData.TithingCost, showReagents: false);

        _byType[SpellBookType.Spellweaving] = Std(SpellBookType.Spellweaving, 0x2B2F, 0x2B2D,
            SpellweavingSpellData.Names, SpellweavingSpellData.PowerWords, SpellweavingSpellData.Reagents,
            SpellweavingSpellData.Ids, SpellweavingSpellData.IconIds, SpellweavingSpellData.ManaCost,
            SpellweavingSpellData.MinSkill, SpellweavingSpellData.TithingCost, showReagents: false);

        _byType[SpellBookType.Mysticism] = Std(SpellBookType.Mysticism, 0x2B32, 0x2B30,
            MysticismSpellData.Names, MysticismSpellData.PowerWords, MysticismSpellData.Reagents,
            MysticismSpellData.Ids, MysticismSpellData.IconIds, MysticismSpellData.ManaCost,
            MysticismSpellData.MinSkill, MysticismSpellData.TithingCost, showReagents: true);

        // Mastery shares the Magery book art (0x08AC). It carries a richer
        // group / passive layout in legacy; here it renders through the generic
        // path (sequential spell pages), which is functional but not grouped.
        _byType[SpellBookType.Mastery] = Std(SpellBookType.Mastery, 0x08AC, 0x08BA,
            MasterySpellData.Names, MasterySpellData.PowerWords, MasterySpellData.Reagents,
            MasterySpellData.Ids, MasterySpellData.IconIds, MasterySpellData.ManaCost,
            MasterySpellData.MinSkill, MasterySpellData.TithingCost, showReagents: true);
    }

    private static SpellSchool Std(SpellBookType type, ushort book, ushort minimized,
        string[] names, string[] words, string[] reagents, int[] ids, int[] icons,
        int[] mana, int[] skill, int[] tithing, bool showReagents)
        => new SpellSchool
        {
            Type = type,
            BookGraphic = book,
            MinimizedGraphic = minimized,
            MaxSpells = names.Length,
            Names = names,
            PowerWords = words,
            Reagents = reagents,
            Ids = ids,
            IconIds = icons,
            ManaCost = mana,
            MinSkill = skill,
            TithingCost = tithing,
            HasCircles = false,
            ShowReagents = showReagents,
            ShowRequires = true,
        };

    // Item graphic -> school, mirroring legacy SpellbookGump.AssignGraphic.
    public static SpellBookType Resolve(ushort itemGraphic) => itemGraphic switch
    {
        0x2253 => SpellBookType.Necromancy,
        0x2252 => SpellBookType.Chivalry,
        0x238C => SpellBookType.Bushido,
        0x23A0 => SpellBookType.Ninjitsu,
        0x2D50 => SpellBookType.Spellweaving,
        0x2D9D => SpellBookType.Mysticism,
        0x225A or 0x225B => SpellBookType.Mastery,
        _ => SpellBookType.Magery,
    };

    // Bushido/Ninjitsu books only resolve when the server advertised the
    // Samurai/Ninja expansion (CLF_SAMURAI_NINJA) — legacy SpellbookGump.AssignGraphic.
    // ponytail: falls back to Magery rather than refusing to open (a non-SE shard
    // never sends these graphics anyway); upgrade to a "don't open" path if a shard
    // ever pushes a ninja book with the flag cleared.
    public static SpellBookType Resolve(ushort itemGraphic, bool samuraiNinjaEnabled)
    {
        var type = Resolve(itemGraphic);
        if (!samuraiNinjaEnabled && (type == SpellBookType.Bushido || type == SpellBookType.Ninjitsu))
            return SpellBookType.Magery;
        return type;
    }

    public static SpellSchool Get(SpellBookType type) => _byType[type];
}
