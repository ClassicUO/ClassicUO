using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Data
{
    internal static class SpellsBushido
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;
        private static readonly List<SpellDefinition> _spells;

        static SpellsBushido()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Honorable Execution", 401, 0x5420, string.Empty, 0, 25, Reagents.None)
                },
                {
                    2, new SpellDefinition("Confidence", 402, 0x5421, string.Empty, 10, 25, Reagents.None)
                },
                {
                    3, new SpellDefinition("Evasion", 403, 0x5422, string.Empty, 10, 60, Reagents.None)
                },
                {
                    4, new SpellDefinition("Counter Attack", 404, 0x5423, string.Empty, 5, 40, Reagents.None)
                },
                {
                    5, new SpellDefinition("Lightning Strike", 405, 0x5424, string.Empty, 10, 50, Reagents.None)
                },
                {
                    6, new SpellDefinition("Momentum Strike", 406, 0x5425, string.Empty, 10, 70, Reagents.None)
                }
            };
            _spells = _spellsDict.Values.ToList();
        }

        public static IReadOnlyList<SpellDefinition> Spells => _spells;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            SpellDefinition spell;

            if (_spellsDict.TryGetValue(spellIndex, out spell))
                return spell;

            return SpellDefinition.EmptySpell;
        }
    }
}