using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    static class SpellsNinjitsu
    {
        static readonly Dictionary<int, SpellDefinition> _spellsDict;
        static readonly List<SpellDefinition> _spells;

        public static IReadOnlyList<SpellDefinition> Spells => _spells;


        public static SpellDefinition GetSpell(int spellIndex)
        {
            SpellDefinition spell;
            if (_spellsDict.TryGetValue(spellIndex, out spell))
                return spell;

            return SpellDefinition.EmptySpell;
        }

        static SpellsNinjitsu()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>()
            {
                // Spell List
                { 1, new SpellDefinition("Focus Attack", 501, 0x5320, string.Empty, 20, 60, Reagents.None) },
                { 2, new SpellDefinition("Death Strike", 502, 0x5321, string.Empty, 30, 85, Reagents.None) },
                { 3, new SpellDefinition("Animal Form", 503, 0x5322, string.Empty, 0, 10, Reagents.None) },
                { 4, new SpellDefinition("Ki Attack", 504, 0x5323, string.Empty, 25, 80, Reagents.None) },
                { 5, new SpellDefinition("Surprise Attack", 505, 0x5324, string.Empty, 20, 30, Reagents.None) },
                { 6, new SpellDefinition("Backstab", 506, 0x5325, string.Empty, 30, 20, Reagents.None) },
                { 7, new SpellDefinition("Shadowjump", 507, 0x5326, string.Empty, 15, 50, Reagents.None) },
                { 8, new SpellDefinition("Mirror Image", 508, 0x5327, string.Empty, 10, 40, Reagents.None) },
            };

            _spells = _spellsDict.Values.ToList();

        }
    }
}
