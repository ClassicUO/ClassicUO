#region license

// Copyright (c) 2024, andreakarasho
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class ProfessionInfo
    {
        public static readonly int[,] _VoidSkills = new int[4, 2]
        {
            { 0, InitialSkillValue }, { 0, InitialSkillValue },
            { 0, UOFileManager.Version < ClientVersion.CV_70160 ? 0 : InitialSkillValue }, { 0, InitialSkillValue }
        };
        public static readonly int[] _VoidStats = new int[3] { 60, RemainStatValue, RemainStatValue };
        public static int InitialSkillValue => UOFileManager.Version >= ClientVersion.CV_70160 ? 30 : 50;
        public static int RemainStatValue => UOFileManager.Version >= ClientVersion.CV_70160 ? 15 : 10;
        public string Name { get; set; }
        public string TrueName { get; set; }
        public int Localization { get; set; }
        public int Description { get; set; }
        public int DescriptionIndex { get; set; }
        public ProfessionLoader.PROF_TYPE Type { get; set; }

        public ushort Graphic { get; set; }

        public bool TopLevel { get; set; }
        public int[,] SkillDefVal { get; set; } = _VoidSkills;
        public int[] StatsVal { get; set; } = _VoidStats;
        public List<string> Children { get; set; }
    }

    public class ProfessionLoader : UOFileLoader
    {
        private static ProfessionLoader _instance;
        private readonly string[] _Keys =
        {
            "begin", "name", "truename", "desc", "toplevel", "gump", "type", "children", "skill",
            "stat", "str", "int", "dex", "end", "true", "category", "nameid", "descid"
        };

        private ProfessionLoader()
        {
        }

        public static ProfessionLoader Instance => _instance ?? (_instance = new ProfessionLoader());

        public Dictionary<ProfessionInfo, List<ProfessionInfo>> Professions { get; } = new Dictionary<ProfessionInfo, List<ProfessionInfo>>();

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    bool result = false;

                    FileInfo file = new FileInfo(UOFileManager.GetUOFilePath("Prof.txt"));

                    if (file.Exists)
                    {
                        if (file.Length > 0x100000) //1megabyte limit of string file
                        {
                            throw new InternalBufferOverflowException($"{file.FullName} exceeds the maximum 1Megabyte allowed size for a string text file, please, check that the file is correct and not corrupted -> {file.Length} file size");
                        }

                        //what if file doesn't exist? we skip section completely...directly into advanced selection
                        TextFileParser read = new TextFileParser(File.ReadAllText(file.FullName), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

                        while (!read.IsEOF())
                        {
                            List<string> strings = read.ReadTokens();

                            if (strings.Count > 0)
                            {
                                if (strings[0].ToLower() == "begin")
                                {
                                    result = ParseFilePart(read);

                                    if (!result)
                                    {
                                        break;
                                    }
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
                        kvp.Key.Children = null;

                        if (kvp.Value != null)
                        {
                            foreach (ProfessionInfo info in kvp.Value)
                            {
                                info.Children = null;
                            }
                        }
                    }
                }
            );
        }

        private int GetKeyCode(string key)
        {
            key = key.ToLowerInvariant();
            int result = 0;

            for (int i = 0; i < _Keys.Length && result <= 0; i++)
            {
                if (key == _Keys[i])
                {
                    result = i + 1;
                }
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
            int[,] skillIndex = new int[4, 2] { { 0xFF, 0 }, { 0xFF, 0 }, { 0xFF, 0 }, { 0xFF, 0 } };
            int[] stats = new int[3] { 0, 0, 0 };

            bool exit = false;

            while (!file.IsEOF() && !exit)
            {
                List<string> strings = file.ReadTokens();

                if (strings.Count < 1)
                {
                    continue;
                }

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
                        {
                            type = PROF_TYPE.CATEGORY;
                        }
                        else
                        {
                            type = PROF_TYPE.PROFESSION;
                        }

                        break;
                    }

                    case PM_CODE.CHILDREN:

                    {
                        for (int j = 1; j < strings.Count; j++)
                        {
                            childrens.Add(strings[j]);
                        }

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

                            for (int j = 0; j < SkillsLoader.Instance.SkillsCount; j++)
                            {
                                SkillEntry skill = SkillsLoader.Instance.Skills[j];

                                if (strings[1] == skill.Name || ((SkillEntry.HardCodedName) skill.Index).ToString().ToLower() == strings[1].ToLower())
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
                            {
                                stats[0] = val;
                            }
                            else if ((PM_CODE) code == PM_CODE.INT)
                            {
                                stats[1] = val;
                            }
                            else if ((PM_CODE) code == PM_CODE.DEX)
                            {
                                stats[2] = val;
                            }
                        }

                        break;
                    }

                    case PM_CODE.NAME_CLILOC_ID:

                    {
                        int.TryParse(strings[1], out nameClilocID);
                        name = ClilocLoader.Instance.GetString(nameClilocID, true, name);

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
                    Children = childrens
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
                {
                    Professions[info] = list;
                }
                else
                {
                    foreach (KeyValuePair<ProfessionInfo, List<ProfessionInfo>> kvp in Professions)
                    {
                        if (kvp.Key.Children != null && kvp.Value != null && kvp.Key.Children.Contains(trueName))
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

        public enum PROF_TYPE
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