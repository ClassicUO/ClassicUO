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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class SkillsGroupManager
    {
        public static Dictionary<string, HashSet<int>> Groups { get; } = new Dictionary<string, HashSet<int>>();

        private static void MakeCUODefault()
        {
            Groups.Clear();

            int count = UOFileManager.Skills.SkillsCount;

            Groups.Add("Miscellaneous", new HashSet<int>
                       {
                           4, 6, 10, 12, 19, 3, 36
                       }
                      );

            Groups.Add("Combat", new HashSet<int>
                       {
                           1, 31, 42, 17, 41, 5, 40, 27
                       }
                      );

            if (count > 57)
                Groups["Combat"].Add(57);
            Groups["Combat"].Add(43);

            if (count > 50)
                Groups["Combat"].Add(50);

            if (count > 51)
                Groups["Combat"].Add(51);

            if (count > 52)
                Groups["Combat"].Add(52);

            if (count > 53)
                Groups["Combat"].Add(53);


            Groups.Add("Trade Skills", new HashSet<int>
                       {
                           0, 7, 8, 11, 13, 23, 44, 45, 34, 37
                       }
                      );

            Groups.Add("Magic", new HashSet<int>
            {
                16
            });

            if (count > 56)
                Groups["Magic"].Add(56);
            Groups["Magic"].Add(25);
            Groups["Magic"].Add(46);

            if (count > 55)
                Groups["Magic"].Add(55);
            Groups["Magic"].Add(26);

            if (count > 54)
                Groups["Magic"].Add(54);
            Groups["Magic"].Add(32);

            if (count > 49)
                Groups["Magic"].Add(49);


            Groups.Add("Wilderness", new HashSet<int>
                       {
                           2, 35, 18, 20, 38, 39
                       }
                      );

            Groups.Add("Thieving", new HashSet<int>
                       {
                           14, 21, 24, 30, 48, 28, 33, 47
                       }
                      );

            Groups.Add("Bard", new HashSet<int>
                       {
                           15, 29, 9, 22
                       }
                      );

            Save();
        }

        public static void LoadDefault()
        {
            FileInfo info = new FileInfo(UOFileManager.GetUOFilePath("skillgrp.mul"));

            if (!info.Exists)
            {
                Log.Info("skillgrp.mul not present, using CUO defaults!");
                MakeCUODefault();

                return;
            }

            Groups.Clear();
            Log.Info("Loading skillgrp.mul...");

            try
            {
                int skillidx = 0;
                bool unicode = false;

                using (BinaryReader bin = new BinaryReader(File.OpenRead(info.FullName)))
                {
                    int start = 4;
                    int strlen = 17;
                    int count = bin.ReadInt32();

                    if (count == -1)
                    {
                        unicode = true;
                        count = bin.ReadInt32();
                        start *= 2;
                        strlen *= 2;
                    }

                    List<string> groups = new List<string>
                        {
                            "Miscellaneous"
                        };
                    Groups.Add("Miscellaneous", new HashSet<int>());
                    StringBuilder sb = new StringBuilder(17);

                    for (int i = 0; i < count - 1; ++i)
                    {
                        short strbuild;
                        bin.BaseStream.Seek(start + i * strlen, SeekOrigin.Begin);

                        if (unicode)
                        {
                            while ((strbuild = bin.ReadInt16()) != 0)
                                sb.Append((char) strbuild);
                        }
                        else
                        {
                            while ((strbuild = bin.ReadByte()) != 0)
                                sb.Append((char) strbuild);
                        }

                        groups.Add(sb.ToString());
                        Groups.Add(sb.ToString(), new HashSet<int>());
                        sb.Clear();
                    }

                    bin.BaseStream.Seek(start + (count - 1) * strlen, SeekOrigin.Begin);

                    while (bin.BaseStream.Length != bin.BaseStream.Position)
                    {
                        int grp = bin.ReadInt32();

                        if (grp < groups.Count && skillidx + 1 < UOFileManager.Skills.SkillsCount)
                            Groups[groups[grp]].Add(skillidx++);
                    }

                    Save();
                }
            }
            catch (Exception e)
            {
                Log.Debug($"Error while reading skillgrp.mul, using CUO defaults! exception given is: {e}");
                MakeCUODefault();
            }
        }

        public static bool AddNewGroup(string group)
        {
            if (!Groups.ContainsKey(group))
            {
                Groups.Add(group, new HashSet<int>());

                return true;
            }

            return false;
        }

        public static bool RemoveGroup(string group)
        {
            if (Groups.FirstOrDefault().Key != group && Groups.TryGetValue(group, out var list))
            {
                Groups.Remove(group);

                if (Groups.Count == 0)
                    Groups.Add("All", list);
                else
                {
                    foreach (var k in Groups)
                    {
                        foreach (var item in list)
                        {
                            k.Value.Add(item);
                        }

                        break;
                    }
                }


                return true;
            }

            return false;
        }

        public static HashSet<int> GetSkillsInGroup(string group)
        {
            Groups.TryGetValue(group, out var list);

            return list;
        }

        public static void ReplaceGroup(string oldGroup, string newGroup)
        {
            if (Groups.TryGetValue(oldGroup, out var oldList) && !Groups.TryGetValue(newGroup, out var newList))
            {
                Groups.Remove(oldGroup);
                Groups[newGroup] = oldList;
            }
        }

        public static void MoveSkillToGroup(string oldGroup, string newGroup, int skillIndex)
        {
            if (Groups.TryGetValue(oldGroup, out var oldList) && Groups.TryGetValue(newGroup, out var newList))
            {
                oldList.Remove(skillIndex);
                newList.Add(skillIndex);
            }
        }

        public static void Load()
        {
            Groups.Clear();

            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", ProfileManager.Current.Username, ProfileManager.Current.ServerName, ProfileManager.Current.CharacterName, "skillsgroups.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No skillsgroups.xml file. Creating a default file.");
                LoadDefault();
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return;
            }

            XmlElement root = doc["skillsgroups"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("group"))
                {
                    HashSet<int> list = new HashSet<int>();

                    string name = xml.GetAttribute("name");

                    XmlElement xmlIdsRoot = xml["skillids"];

                    if (xmlIdsRoot != null)
                    {
                        foreach (XmlElement xmlIds in xmlIdsRoot.GetElementsByTagName("skill"))
                        {
                            int id = int.Parse(xmlIds.GetAttribute("id"));

                            foreach (KeyValuePair<string, HashSet<int>> k in Groups)
                            {
                                if (k.Value.Contains(id))
                                {
                                    k.Value.Remove(id);
                                }
                            }

                            list.Add(id);
                        }
                    }

                    Groups[name] = list;
                }
            }
        }


        public static void Save()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", ProfileManager.Current.Username, ProfileManager.Current.ServerName, ProfileManager.Current.CharacterName, "skillsgroups.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = System.Xml.Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("skillsgroups");

                foreach (KeyValuePair<string, HashSet<int>> k in Groups)
                {
                    xml.WriteStartElement("group");

                    xml.WriteAttributeString("name", k.Key);

                    xml.WriteStartElement("skillids");
                    foreach (int skillID in k.Value)
                    {
                        xml.WriteStartElement("skill");
                        xml.WriteAttributeString("id", skillID.ToString());
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();

                    xml.WriteEndElement();
                }
                
                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
    }
}