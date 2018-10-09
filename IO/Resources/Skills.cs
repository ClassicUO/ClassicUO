#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassicUO.IO.Resources
{
    public static class Skills
    {
        /*[StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SkillEntryI
        {
            public bool HasButton;
            public fixed char Name;
        }*/

        private static UOFileMul _file;

        private static readonly Dictionary<int, SkillEntry> _skills = new Dictionary<int, SkillEntry>();

        public static int SkillsCount => _skills.Count;

        public static void Load()
        {
            if (SkillsCount > 0)
                return;

            string path = Path.Combine(FileManager.UoFolderPath, "Skills.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "Skills.idx");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, 56, 16);

            int i = 0;
            while (_file.Position < _file.Length) GetSkill(i++);
        }

        public static SkillEntry GetSkill(int index)
        {
            if (!_skills.TryGetValue(index, out SkillEntry value))
            {
                (int length, int extra, bool patched) = _file.SeekByEntryIndex(index);
                if (length == 0)
                    return default;

                //SkillEntryI entry = _file.ReadStruct<SkillEntryI>(_file.Position );


                value = new SkillEntry
                {
                    HasButton = _file.ReadBool(), Name = Encoding.UTF8.GetString(_file.ReadArray<byte>(length - 1)),
                    Index = index
                };

                _skills[index] = value;
            }

            return value;
        }
    }

    public struct SkillEntry
    {
        public int Index;
        public string Name;
        public bool HasButton;
    }
}