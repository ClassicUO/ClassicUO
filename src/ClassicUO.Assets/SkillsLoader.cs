// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class SkillsLoader : UOFileLoader
    {
        private UOFileMul _file;

        public SkillsLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public int SkillsCount => Skills.Count;
        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public readonly List<SkillEntry> SortedSkills = new List<SkillEntry>();

        public override unsafe void Load()
        {
            if (SkillsCount > 0)
            {
                return;
            }

            string path = FileManager.GetUOFilePath("skills.mul");
            string pathidx = FileManager.GetUOFilePath("Skills.idx");

            FileSystemHelper.EnsureFileExists(path);
            FileSystemHelper.EnsureFileExists(pathidx);

            _file = new UOFileMul(path, pathidx);
            _file.FillEntries();

            var buf = new byte[256];
            for (int i = 0, count = 0; i < _file.Entries.Length; i++)
            {
                ref var entry = ref _file.GetValidRefEntry(i);
                if (entry.Length <= 0) continue;

                _file.Seek(entry.Offset, System.IO.SeekOrigin.Begin);
                bool hasAction = _file.ReadInt8() != 0;
                if (buf.Length < entry.Length)
                    buf = new byte[entry.Length];

                _file.Read(buf.AsSpan(0, entry.Length - 1));
                var name = Encoding.ASCII.GetString(buf.AsSpan(0, entry.Length - 1)).TrimEnd('\0');

                Skills.Add(new SkillEntry(count++, name, hasAction));
            }

            // Add Vystia custom class skills (IDs 58-83)
            // These are added programmatically to match server-side skill definitions
            AddVystiaSkills();

            SortedSkills.AddRange(Skills);
            SortedSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
        }

        /// <summary>
        /// Adds Vystia custom class skills (26 skills, IDs 58-83)
        /// These must match the server-side SkillInfo definitions in ServUO/Server/Skills.cs
        /// </summary>
        private void AddVystiaSkills()
        {
            // Standard UO has 58 skills (0-57). Vystia adds 26 more (58-83) for total of 84.
            // Pad with empty entries if MUL file has fewer than 58 skills
            while (Skills.Count < 58)
            {
                Skills.Add(new SkillEntry(Skills.Count, $"Unknown{Skills.Count}", false));
            }

            // Add all 26 Vystia skills (IDs 58-83)
            // Using < comparison since Skills.Count represents the NEXT index to add

            // Vystia Magic Class Skills (IDs 58-69)
            if (Skills.Count < 59) Skills.Add(new SkillEntry(58, "Cryomancy", false));        // Ice Mage
            if (Skills.Count < 60) Skills.Add(new SkillEntry(59, "Demonology", false));       // Warlock
            if (Skills.Count < 61) Skills.Add(new SkillEntry(60, "Necromantic Arts", false)); // Necromancer
            if (Skills.Count < 62) Skills.Add(new SkillEntry(61, "Druidism", false));         // Druid
            if (Skills.Count < 63) Skills.Add(new SkillEntry(62, "Elementalism", false));     // Sorcerer
            if (Skills.Count < 64) Skills.Add(new SkillEntry(63, "Bardic Lore", false));      // Bard
            if (Skills.Count < 65) Skills.Add(new SkillEntry(64, "Hexcraft", false));         // Witch
            if (Skills.Count < 66) Skills.Add(new SkillEntry(65, "Divination", false));       // Oracle
            if (Skills.Count < 67) Skills.Add(new SkillEntry(66, "Conjuration", false));      // Summoner
            if (Skills.Count < 68) Skills.Add(new SkillEntry(67, "Spirit Calling", false));   // Shaman
            if (Skills.Count < 69) Skills.Add(new SkillEntry(68, "Runeweaving", false));      // Enchanter
            if (Skills.Count < 70) Skills.Add(new SkillEntry(69, "Illusion Magic", false));   // Illusionist

            // Vystia Martial Class Skills (IDs 70-83)
            if (Skills.Count < 71) Skills.Add(new SkillEntry(70, "Berserking", false));       // Barbarian
            if (Skills.Count < 72) Skills.Add(new SkillEntry(71, "Subterfuge", false));       // Rogue
            if (Skills.Count < 73) Skills.Add(new SkillEntry(72, "Martial Arts", false));     // Monk
            if (Skills.Count < 74) Skills.Add(new SkillEntry(73, "Chivalric Arts", false));   // Knight
            if (Skills.Count < 75) Skills.Add(new SkillEntry(74, "Holy Devotion", false));    // Paladin
            if (Skills.Count < 76) Skills.Add(new SkillEntry(75, "Marksmanship", false));     // Ranger
            if (Skills.Count < 77) Skills.Add(new SkillEntry(76, "Combat Mastery", false));   // Fighter
            if (Skills.Count < 78) Skills.Add(new SkillEntry(77, "Zealotry", false));         // Templar
            if (Skills.Count < 79) Skills.Add(new SkillEntry(78, "Manhunting", false));       // Bounty Hunter
            if (Skills.Count < 80) Skills.Add(new SkillEntry(79, "Beast Bonding", false));    // Beastmaster
            if (Skills.Count < 81) Skills.Add(new SkillEntry(80, "Engineering", false));      // Artificer
            if (Skills.Count < 82) Skills.Add(new SkillEntry(81, "Transmutation", false));    // Alchemist
            if (Skills.Count < 83) Skills.Add(new SkillEntry(82, "Divine Grace", false));     // Cleric
            if (Skills.Count < 84) Skills.Add(new SkillEntry(83, "Arcane Studies", false));   // Wizard
        }

        public int GetSortedIndex(int index)
        {
            if (index < SkillsCount)
            {
                return SortedSkills[index].Index;
            }

            return -1;
        }
    }

    public class SkillEntry
    {
        public SkillEntry(int index, string name, bool hasAction)
        {
            Index = index;
            Name = name;
            HasAction = hasAction;
        }

        public bool HasAction;
        public readonly int Index;
        public string Name;

        public override string ToString()
        {
            return Name;
        }

        public enum HardCodedName
        {
            Alchemy,
            Anatomy,
            AnimalLore,
            ItemID, // T2A
            ArmsLore,
            Parrying,
            Begging,
            Blacksmith,
            Bowcraft,
            Peacemaking,
            Camping,
            Carpentry,
            Cartography,
            Cooking,
            DetectHidden,
            Enticement,
            EvaluateIntelligence,
            Healing,
            Fishing,
            ForensicEvaluation,
            Herding,
            Hiding,
            Provocation,
            Inscription,
            Lockpicking,
            Magery,
            ResistingSpells,
            Tactics,
            Snooping,
            Musicanship,
            Poisoning,
            Archery,
            SpiritSpeak,
            Stealing,
            Tailoring,
            AnimalTaming,
            TasteIdentification,
            Tinkering,
            Tracking,
            Veterinary,
            Swordsmanship,
            MaceFighting,
            Fencing,
            Wrestling,
            Lumberjacking,
            Mining,
            Meditation,
            Stealth,
            Disarm,
            Necromancy,
            Focus,
            Chivalry,
            Bushido,
            Ninjitsu,
            Spellweaving,
            Mysticism,
            Imbuing,
            Throwing
        }
    }
}