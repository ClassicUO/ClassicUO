#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class SkillsLoader : UOFileLoader
    {
        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public readonly List<SkillEntry> SortedSkills = new List<SkillEntry>();

        private UOFileMul _file;

        public int SkillsCount => Skills.Count;

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

        public override void CleanResources()
        {
            //
        }
    }

    internal class SkillEntry
    {
        public SkillEntry(int index, string name, bool hasAction)
        {
            Index = index;
            Name = name;
            HasAction = hasAction;
        }

        public readonly int Index;
        public readonly string Name;
        public readonly bool HasAction;

        public override string ToString()
        {
            return Name;
        }
    }
}