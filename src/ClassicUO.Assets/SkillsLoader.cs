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

            SortedSkills.AddRange(Skills);
            SortedSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
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