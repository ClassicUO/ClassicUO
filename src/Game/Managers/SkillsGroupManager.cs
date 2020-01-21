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
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    sealed class SkillsGroup
    {
        private readonly byte[] _list = new byte[60];

        public SkillsGroup()
        {
            for (int i = 0; i < _list.Length; i++)
            {
                _list[i] = 0xFF;
            }
        }


        public int Count;
        public bool IsMaximized;
        public string Name = "No Name";
        public SkillsGroup Left { get; set; }
        public SkillsGroup Right { get; set; }

        public byte GetSkill(int index)
        {
            if (index < 0 || index >= Count)
                return 0xFF;

            return _list[index];
        }

        public void Add(byte item)
        {
            if (!Contains(item))
            {
                _list[Count++] = item;
            }
        }

        public void Remove(byte item)
        {
            bool removed = false;

            for (int i = 0; i < Count; i++)
            {
                if (_list[i] == item)
                {
                    removed = true;

                    for (; i < Count - 1; i++)
                    {
                        _list[i] = _list[i + 1];
                    }

                    break;
                }
            }

            if (removed)
            {
                Count--;

                if (Count < 0)
                    Count = 0;

                _list[Count] = 0xFF;
            }
        }

        public bool Contains(byte item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_list[i] == item)
                    return true;
            }

            return false;
        }

        public void Sort()
        {
            byte[] table = new byte[60];
            int index = 0;

            int count = SkillsLoader.Instance.SkillsCount;

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < Count; j++)
                {
                    if (SkillsLoader.Instance.GetSortedIndex(i) == _list[j])
                    {
                        table[index++] = _list[j];
                        break;
                    }
                }
            }

            for (int j = 0; j < Count; j++)
            {
                _list[j] = table[j];
            }
        }

        public void TransferTo(SkillsGroup group)
        {
            for (int i = 0; i < Count; i++)
            {
                group.Add(_list[i]);
            }

            group.Sort();
        }

        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("group");
            xml.WriteAttributeString("name", Name);
            xml.WriteStartElement("skillids");

            for (int i = 0; i < Count; i++)
            {
                byte idx = GetSkill(i);

                if (idx != 0xFF)
                {
                    xml.WriteStartElement("skill");
                    xml.WriteAttributeString("id", idx.ToString());
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
        }
    }

    static class SkillsGroupManager
    {
        public static readonly List<SkillsGroup> Groups = new List<SkillsGroup>();


      
        public static void Add(SkillsGroup g)
        {
            Groups.Add(g);
        }

        public static bool Remove(SkillsGroup g)
        {
            if (Groups[0] == g)
            {
                MessageBoxGump messageBox = new MessageBoxGump(200, 125, "Cannot delete this group.", null)
                {
                    X = ProfileManager.Current.GameWindowPosition.X + ProfileManager.Current.GameWindowSize.X / 2 - 100,
                    Y = ProfileManager.Current.GameWindowPosition.Y + ProfileManager.Current.GameWindowSize.Y / 2 - 62,
                };
                UIManager.Add(messageBox);
                return false;
            }

            Groups.Remove(g);
            g.TransferTo(Groups[0]);

            return true;
        }

        public static void Clear()
        {
            Groups.Clear();
        }


        public static void Load()
        {
            Groups.Clear();

            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", ProfileManager.Current.Username, ProfileManager.Current.ServerName, ProfileManager.Current.CharacterName, "skillsgroups.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No skillsgroups.xml file. Creating a default file.");
                MakeDefault();
            }

            Groups.Clear();

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
                    SkillsGroup g = new SkillsGroup();
                    g.Name = xml.GetAttribute("name");

                    XmlElement xmlIdsRoot = xml["skillids"];

                    if (xmlIdsRoot != null)
                    {
                        foreach (XmlElement xmlIds in xmlIdsRoot.GetElementsByTagName("skill"))
                        {
                            g.Add(byte.Parse(xmlIds.GetAttribute("id")));
                        }
                    }

                    g.Sort();
                    Add(g);
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

                foreach (var k in Groups)
                {
                    k.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }


        public static void MakeDefault()
        {
            Clear();

            if (!LoadMULFile(UOFileManager.GetUOFilePath("skillgrp.mul")))
            {
                MakeDefaultMiscellaneous();
                MakeDefaultCombat();
                MakeDefaultTradeSkills();
                MakeDefaultMagic();
                MakeDefaultWilderness();
                MakeDefaultThieving();
                MakeDefaultBard();
            }

            foreach (var g in Groups)
            {
                g.Sort();
            }

            Save();
        }

        private static void MakeDefaultMiscellaneous()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = "Miscellaneous";
            g.Add(4);
            g.Add(6);
            g.Add(10);
            g.Add(12);
            g.Add(19);
            g.Add(3);
            g.Add(36);

            Add(g);
        }

        private static void MakeDefaultCombat()
        {
            int count = SkillsLoader.Instance.SkillsCount;

            SkillsGroup g = new SkillsGroup();
            g.Name = "Combat";
            g.Add(1);
            g.Add(31);
            g.Add(42);
            g.Add(17);
            g.Add(41);
            g.Add(5);
            g.Add(40);
            g.Add(27);

            if (count > 57)
            {
                g.Add(57);
            }

            g.Add(43);

            if (count > 50)
            {
                g.Add(50);
            }

            if (count > 51)
            {
                g.Add(51);
            }

            if (count > 52)
            {
                g.Add(52);
            }

            if (count > 53)
            {
                g.Add(53);
            }

            Add(g);
        }

        private static void MakeDefaultTradeSkills()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = "Trade Skills";
            g.Add(0);
            g.Add(7);
            g.Add(8);
            g.Add(11);
            g.Add(13);
            g.Add(23);
            g.Add(44);
            g.Add(45);
            g.Add(34);
            g.Add(37);
            
            Add(g);
        }

        private static void MakeDefaultMagic()
        {
            int count = SkillsLoader.Instance.SkillsCount;

            SkillsGroup g = new SkillsGroup();
            g.Name = "Magic";
            g.Add(16);

            if (count > 56)
            {
                g.Add(56);
            }

            g.Add(25);
            g.Add(46);

            if (count > 55)
            {
                g.Add(55);
            }

            g.Add(26);

            if (count > 54)
            {
                g.Add(54);
            }

            g.Add(32);

            if (count > 49)
            {
                g.Add(49);
            }

            Add(g);
        }

        private static void MakeDefaultWilderness()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = "Wilderness";
            g.Add(2);
            g.Add(35);
            g.Add(18);
            g.Add(20);
            g.Add(38);
            g.Add(39);

            Add(g);
        }

        private static void MakeDefaultThieving()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = "Thieving";
            g.Add(14);
            g.Add(21);
            g.Add(24);
            g.Add(30);
            g.Add(48);
            g.Add(28);
            g.Add(33);
            g.Add(47);

            Add(g);
        }

        private static void MakeDefaultBard()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = "Bard";
            g.Add(15);
            g.Add(29);
            g.Add(9);
            g.Add(22);

            Add(g);
        }

        private static bool LoadMULFile(string path)
        {
            FileInfo info = new FileInfo(path);

            if (!info.Exists)
                return false;

            try
            {
                byte skillidx = 0;
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

  
                    StringBuilder sb = new StringBuilder(17);

                    SkillsGroup g = new SkillsGroup();
                    g.Name = "Miscellaneous";

                    SkillsGroup[] groups = new SkillsGroup[count];
                    groups[0] = g;

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

                        groups[i + 1] = new SkillsGroup
                        {
                            Name = sb.ToString()
                        };

                        sb.Clear();
                    }

                    bin.BaseStream.Seek(start + (count - 1) * strlen, SeekOrigin.Begin);

                    while (bin.BaseStream.Length != bin.BaseStream.Position)
                    {
                        int grp = bin.ReadInt32();

                        if (grp < groups.Length && skillidx + 1 < SkillsLoader.Instance.SkillsCount)
                        {
                            groups[grp].Add(skillidx++);
                        }
                    }

                    for (int i = 0; i < groups.Length; i++)
                    {
                        Add(groups[i]);
                    }

                }
            }
            catch (Exception e)
            {
                Log.Debug($"Error while reading skillgrp.mul, using CUO defaults! exception given is: {e}");

                return false;
            }

            return Groups.Count != 0;
        }
    }
}