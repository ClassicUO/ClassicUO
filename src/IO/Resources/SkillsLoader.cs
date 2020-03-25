#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicUO.Utility;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class SkillsLoader : UOFileLoader
    {    
        private UOFileMul _file;

        private SkillsLoader()
        {

        }

        private static SkillsLoader _instance;
        public static SkillsLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SkillsLoader();
                }

                return _instance;
            }
        }



        public int SkillsCount => Skills.Count;
        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public readonly List<SkillEntry> SortedSkills = new List<SkillEntry>();


        public override Task Load()
        {
            return Task.Run(() =>
            {
                if (SkillsCount > 0)
                    return;

                string path = UOFileManager.GetUOFilePath("skills.mul");
                string pathidx = UOFileManager.GetUOFilePath("Skills.idx");

                FileSystemHelper.EnsureFileExists(path);
                FileSystemHelper.EnsureFileExists(pathidx);

                _file = new UOFileMul(path, pathidx, 0, 16);
                _file.FillEntries(ref Entries);

                for (int i = 0, count = 0; i < Entries.Length; i++)
                { 
                    ref readonly var entry = ref GetValidRefEntry(i);

                    if (entry.Length > 0)
                    {
                        _file.Seek(entry.Offset);
                        var hasAction = _file.ReadBool();
                        var name = Encoding.UTF8.GetString(_file.ReadArray<byte>(entry.Length - 1)).TrimEnd('\0');
                        var skill = new SkillEntry(count++, name, hasAction);

                        Skills.Add(skill);
                    }
                }

                SortedSkills.AddRange(Skills);
                SortedSkills.Sort((a, b) => a.Name.CompareTo(b.Name));
            });
        }

        public int GetSortedIndex(int index)
        {
            if (index < SkillsCount)
            {
                return SortedSkills[index].Index;
            }

            return -1;
        }

        public override void CleanResources()
        {
            //
        }
    }

    internal class SkillEntry
    {
        internal enum HardCodedName
        {
            Alchemy,
            Anatomy,
            AnimalLore,
            ItemID,
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
            Spellweaving
        }

        public SkillEntry(int index, string name, bool hasAction)
        {
            Index = index;
            Name = name;
            HasAction = hasAction;
        }

        public readonly int Index;
        public string Name;
        public bool HasAction;

        public override string ToString()
        {
            return Name;
        }
    }
}