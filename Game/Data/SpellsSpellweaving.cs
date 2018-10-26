using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Data
{
    internal static class SpellsSpellweaving
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;
        private static readonly List<SpellDefinition> _spells;

        static SpellsSpellweaving()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {1, new SpellDefinition("Arcane Circle", 601, 0x59D8, "Myrshalee", 20, 0, Reagents.None)},
                {2, new SpellDefinition("Gift of Renewal", 602, 0x59D9, "Olorisstra", 24, 0, Reagents.None)},
                {3, new SpellDefinition("Immolating Weapon", 603, 0x59DA, "Thalshara", 32, 10, Reagents.None)},
                {4, new SpellDefinition("Attune Weapon", 604, 0x59DB, "Haeldril", 24, 0, Reagents.None)},
                {5, new SpellDefinition("Thunderstorm", 605, 0x59DC, "Erelonia", 32, 10, Reagents.None)},
                {6, new SpellDefinition("Nature's Fury", 606, 0x59DD, "Rauvvrae", 24, 0, Reagents.None)},
                {7, new SpellDefinition("Summon Fey", 607, 0x59DE, "Alalithra", 10, 38, Reagents.None)},
                {8, new SpellDefinition("Summon Fiend", 608, 0x59DF, "Nylisstra", 10, 38, Reagents.None)},
                {9, new SpellDefinition("Reaper Form", 609, 0x59E0, "Tarisstree", 34, 24, Reagents.None)},
                {10, new SpellDefinition("Wildfire", 610, 0x59E1, "Haelyn", 50, 66, Reagents.None)},
                {11, new SpellDefinition("Essence of Wind", 611, 0x59E2, "Anathrae", 40, 52, Reagents.None)},
                {12, new SpellDefinition("Dryad Allure", 612, 0x59E3, "Rathril", 40, 52, Reagents.None)},
                {13, new SpellDefinition("Ethereal Voyage", 613, 0x59E4, "Orlavdra", 32, 24, Reagents.None)},
                {14, new SpellDefinition("Word of Death", 614, 0x59E5, "Nyraxle", 50, 23, Reagents.None)},
                {15, new SpellDefinition("Gift of Life", 615, 0x59E6, "Illorae", 70, 38, Reagents.None)},
                {16, new SpellDefinition("Arcane Empowerment", 616, 0x59E7, "Aslavdra", 50, 24, Reagents.None)}
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