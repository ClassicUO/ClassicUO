// Flat, book-ordered spell catalog for the cast-spell hotkey picker. Built from
// the same SpellSchools tables the spellbook gump uses, so a hotkey's stored
// Param IS the server cast id (Send_CastSpell). Magery 1-64, then Necromancy,
// Chivalry, Bushido, Ninjitsu, Spellweaving, Mysticism, Mastery.

using System.Collections.Generic;

namespace ClassicUO.Ecs;

internal static class HotkeySpells
{
    public readonly record struct Entry(SpellBookType Book, int CastId, string Name);

    public static readonly Entry[] All;

    private static readonly Dictionary<int, string> s_byId = new();

    static HotkeySpells()
    {
        var books = new[]
        {
            SpellBookType.Magery, SpellBookType.Necromancy, SpellBookType.Chivalry,
            SpellBookType.Bushido, SpellBookType.Ninjitsu, SpellBookType.Spellweaving,
            SpellBookType.Mysticism, SpellBookType.Mastery,
        };

        var list = new List<Entry>();
        foreach (var t in books)
        {
            var school = SpellSchools.Get(t);
            for (int i = 1; i <= school.MaxSpells; i++)
            {
                var castId = school.CastId(i);
                var name = school.Name(i);
                list.Add(new Entry(t, castId, name));
                s_byId[castId] = name;
            }
        }
        All = list.ToArray();
    }

    public static string NameOf(int castId) => s_byId.TryGetValue(castId, out var n) ? n : "(spell)";

    public static string BookName(SpellBookType t) => t.ToString();
}
