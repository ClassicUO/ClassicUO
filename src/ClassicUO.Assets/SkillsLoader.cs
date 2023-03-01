#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class SkillsLoader : UOFileLoader
    {
        private static SkillsLoader _instance;
        private UOFileMul _file;

        private SkillsLoader()
        {
        }

        public static SkillsLoader Instance => _instance ?? (_instance = new SkillsLoader());

        public int SkillsCount => Skills.Count;
        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public readonly List<SkillEntry> SortedSkills = new List<SkillEntry>();

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    if (SkillsCount > 0)
                    {
                        return;
                    }

                    string path = UOFileManager.GetUOFilePath("skills.mul");
                    string pathidx = UOFileManager.GetUOFilePath("Skills.idx");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, 0, 16);
                    _file.FillEntries(ref Entries);

                    for (int i = 0, count = 0; i < Entries.Length; i++)
                    {
                        ref UOFileIndex entry = ref GetValidRefEntry(i);

                        if (entry.Length > 0)
                        {
                            _file.SetData(entry.Address, entry.FileSize);
                            _file.Seek(entry.Offset);
                          
                            bool hasAction = _file.ReadBool();
                            string name = Encoding.UTF8.GetString((byte*)_file.PositionAddress, entry.Length - 1).TrimEnd('\0');

                            Skills.Add(new SkillEntry(count++, name, hasAction));
                        }
                    }

                    SortedSkills.AddRange(Skills);
                    SortedSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
                }
            );
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