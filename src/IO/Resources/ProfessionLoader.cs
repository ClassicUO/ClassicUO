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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class ProfessionLoader : ResourceLoader
    {
        private readonly string[] _Keys =
        {
            "begin", "name", "truename", "desc", "toplevel", "gump", "type", "children", "skill",
            "stat", "str", "int", "dex", "end", "true", "category", "nameid", "descid"
        };
        public Dictionary<ProfessionInfo, List<ProfessionInfo>> Professions = new Dictionary<ProfessionInfo, List<ProfessionInfo>>();

        public override Task Load()
        {
            return Task.Run(() =>
            {
                bool result = false;

                FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "Prof.txt"));

                if (file.Exists)
                {
                    if (file.Length > 0x100000) //1megabyte limit of string file
                        throw new InternalBufferOverflowException($"{file.FullName} exceeds the maximum 1Megabyte allowed size for a string text file, please, check that the file is correct and not corrupted -> {file.Length} file size");

                    //what if file doesn't exist? we skip section completely...directly into advanced selection
                    TextFileParser read = new TextFileParser(File.ReadAllText(file.FullName), new[] {' ', '\t', ','}, new[] {'#', ';'}, new[] {'"', '"'});

                    while (!read.IsEOF())
                    {
                        List<string> strings = read.ReadTokens();

                        if (strings.Count > 0)
                        {
                            if (strings[0].ToLower() == "begin")
                            {
                                result = ParseFilePart(read);

                                if (!result) break;
                            }
                        }
                    }
                }

                Professions[new ProfessionInfo
                {
                    Name = "Advanced",
                    Localization = 1061176,
                    Description = 1061226,
                    Graphic = 5545,
                    TopLevel = true,
                    Type = PROF_TYPE.PROFESSION,
                    DescriptionIndex = -1,
                    TrueName = "advanced"
                }] = null;

                foreach (KeyValuePair<ProfessionInfo, List<ProfessionInfo>> kvp in Professions)
                {
                    kvp.Key.Childrens = null;

                    if (kvp.Value != null)
                    {
                        foreach (ProfessionInfo info in kvp.Value)
                            info.Childrens = null;
                    }
                }

            });
        }

        private int GetKeyCode(string key)
        {
            key = key.ToLowerInvariant();
            int result = 0;

            for (int i = 0; i < _Keys.Length && result <= 0; i++)
            {
                if (key == _Keys[i])
                    result = i + 1;
            }

            return result;
        }

        private bool ParseFilePart(TextFileParser file)
        {
            List<string> childrens = new List<string>();
            PROF_TYPE type = PROF_TYPE.NO_PROF;
            string name = string.Empty;
            string trueName = string.Empty;
            int nameClilocID = 0;
            int descriptionClilocID = 0;
            int descriptionIndex = 0;
            ushort gump = 0;
            bool topLevel = false;
            int[,] skillIndex = new int[4, 2] {{0xFF, 0}, {0xFF, 0}, {0xFF, 0}, {0xFF, 0}};
            int[] stats = new int[3] {0, 0, 0};

            bool exit = false;

            while (!file.IsEOF() && !exit)
            {
                List<string> strings = file.ReadTokens();

                if (strings.Count < 1) continue;

                int code = GetKeyCode(strings[0]);

                switch ((PM_CODE) code)
                {
                    case PM_CODE.BEGIN:
                    case PM_CODE.END:

                    {
                        exit = true;

                        break;
                    }

                    case PM_CODE.NAME:

                    {
                        name = strings[1];

                        break;
                    }

                    case PM_CODE.TRUENAME:

                    {
                        trueName = strings[1];

                        break;
                    }

                    case PM_CODE.DESC:

                    {
                        int.TryParse(strings[1], out descriptionIndex);

                        break;
                    }

                    case PM_CODE.TOPLEVEL:

                    {
                        topLevel = GetKeyCode(strings[1]) == (int) PM_CODE.TRUE;

                        break;
                    }

                    case PM_CODE.GUMP:

                    {
                        ushort.TryParse(strings[1], out gump);

                        break;
                    }

                    case PM_CODE.TYPE:

                    {
                        if (GetKeyCode(strings[1]) == (int) PM_CODE.CATEGORY)
                            type = PROF_TYPE.CATEGORY;
                        else
                            type = PROF_TYPE.PROFESSION;

                        break;
                    }

                    case PM_CODE.CHILDREN:

                    {
                        for (int j = 1; j < strings.Count; j++)
                            childrens.Add(strings[j]);

                        break;
                    }

                    case PM_CODE.SKILL:

                    {
                        if (strings.Count > 2)
                        {
                            int idx = 0;

                            for (int i = 0, len = skillIndex.GetLength(0); i < len; i++)
                            {
                                if (skillIndex[i, 0] == 0xFF)
                                {
                                    idx = i;

                                    break;
                                }
                            }

                            for (int j = 0; j < FileManager.Skills.SkillsCount; j++)
                            {
                                SkillEntry skill = FileManager.Skills.GetSkill(j);

                                if (strings[1] == skill.Name)
                                {
                                    skillIndex[idx, 0] = j;
                                    int.TryParse(strings[2], out skillIndex[idx, 1]);

                                    break;
                                }
                            }
                        }

                        break;
                    }

                    case PM_CODE.STAT:

                    {
                        if (strings.Count > 2)
                        {
                            code = GetKeyCode(strings[1]);
                            int.TryParse(strings[2], out int val);

                            if ((PM_CODE) code == PM_CODE.STR)
                                stats[0] = val;
                            else if ((PM_CODE) code == PM_CODE.INT)
                                stats[1] = val;
                            else if ((PM_CODE) code == PM_CODE.DEX) stats[2] = val;
                        }

                        break;
                    }

                    case PM_CODE.NAME_CLILOC_ID:

                    {
                        int.TryParse(strings[1], out nameClilocID);
                        name = FileManager.Cliloc.GetString(nameClilocID);

                        break;
                    }

                    case PM_CODE.DESCRIPTION_CLILOC_ID:

                    {
                        int.TryParse(strings[1], out descriptionClilocID);

                        break;
                    }
                }
            }

            ProfessionInfo info = null;
            List<ProfessionInfo> list = null;

            if (type == PROF_TYPE.CATEGORY)
            {
                info = new ProfessionInfo
                {
                    Childrens = childrens
                };
                list = new List<ProfessionInfo>();
            }
            else if (type == PROF_TYPE.PROFESSION)
            {
                info = new ProfessionInfo
                {
                    StatsVal = stats,
                    SkillDefVal = skillIndex
                };
            }

            bool result = type != PROF_TYPE.NO_PROF;

            if (info != null)
            {
                info.Localization = nameClilocID;
                info.Description = descriptionClilocID;
                info.Name = name;
                info.TrueName = trueName;
                info.DescriptionIndex = descriptionIndex;
                info.TopLevel = topLevel;
                info.Graphic = gump;
                info.Type = type;

                if (topLevel)
                    Professions[info] = list;
                else
                {
                    foreach (KeyValuePair<ProfessionInfo, List<ProfessionInfo>> kvp in Professions)
                    {
                        if (kvp.Key.Childrens != null && kvp.Value != null && kvp.Key.Childrens.Contains(trueName))
                        {
                            Professions[kvp.Key].Add(info);
                            result = true;

                            break;
                        }
                    }
                }
            }

            return result;
        }

        public override void CleanResources()
        {
        }

        internal enum PROF_TYPE
        {
            NO_PROF = 0,
            CATEGORY,
            PROFESSION
        }

        private enum PM_CODE
        {
            BEGIN = 1,
            NAME,
            TRUENAME,
            DESC,
            TOPLEVEL,
            GUMP,
            TYPE,
            CHILDREN,
            SKILL,
            STAT,
            STR,
            INT,
            DEX,
            END,
            TRUE,
            CATEGORY,
            NAME_CLILOC_ID,
            DESCRIPTION_CLILOC_ID
        }
    }
}