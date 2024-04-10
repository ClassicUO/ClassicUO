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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace ClassicUO.Game.Managers
{
    internal sealed class SkillsGroup
    {
        private readonly byte[] _list = new byte[60];

        public SkillsGroup()
        {
            for (int i = 0; i < _list.Length; i++)
            {
                _list[i] = 0xFF;
            }
        }

        public SkillsGroup Left { get; set; }
        public SkillsGroup Right { get; set; }
        public int Count;
        public bool IsMaximized;
        public string Name = ResGeneral.NoName;

        public byte GetSkill(int index)
        {
            if (index < 0 || index >= Count)
            {
                return 0xFF;
            }

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
                {
                    Count = 0;
                }

                _list[Count] = 0xFF;
            }
        }

        public bool Contains(byte item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_list[i] == item)
                {
                    return true;
                }
            }

            return false;
        }

        public unsafe void Sort()
        {
            byte* table = stackalloc byte[60];
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
            xml.WriteAttributeString("isMaximized", IsMaximized.ToString());
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

    internal static class SkillsGroupManager
    {
        private static bool _isActive;
        public static readonly List<SkillsGroup> Groups = new List<SkillsGroup>();
        public static bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
            }
        }

        public static void Add(SkillsGroup g)
        {
            Groups.Add(g);
        }

        public static bool Remove(SkillsGroup g)
        {
            if (Groups[0] == g)
            {
                var camera = Client.Game.Scene.Camera;

                MessageBoxGump messageBox = new MessageBoxGump(200, 125, ResGeneral.CannotDeleteThisGroup, null)
                {
                    X = camera.Bounds.X + camera.Bounds.Width / 2 - 100,
                    Y = camera.Bounds.Y + camera.Bounds.Height / 2 - 62
                };

                UIManager.Add(messageBox);

                return false;
            }

            Groups.Remove(g);
            g.TransferTo(Groups[0]);

            return true;
        }

        public static void Load()
        {
            Groups.Clear();

            string path = Path.Combine(ProfileManager.ProfilePath, "skillsgroups.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No skillsgroups.xml file. Creating a default file.");

                MakeDefault();

                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                MakeDefault();

                Log.Error(ex.ToString());

                return;
            }



            XmlElement root = doc["skillsgroups"];
            if (root != null)
            {
                Boolean.TryParse(root.GetAttribute("isActive"), out _isActive);
                foreach (XmlElement xml in root.GetElementsByTagName("group"))
                {
                    SkillsGroup g = new SkillsGroup();
                    g.Name = xml.GetAttribute("name");

                    Boolean.TryParse(xml.GetAttribute("isMaximized"), out g.IsMaximized);

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
            string path = Path.Combine(ProfileManager.ProfilePath, "skillsgroups.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("skillsgroups");
                xml.WriteAttributeString("isActive", IsActive.ToString());

                foreach (SkillsGroup k in Groups)
                {
                    k.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }


        public static void MakeDefault()
        {
            Groups.Clear();

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

            foreach (SkillsGroup g in Groups)
            {
                g.Sort();
            }

            Save();
        }

        private static void MakeDefaultMiscellaneous()
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Miscellaneous;
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
            g.Name = ResGeneral.Combat;
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
            g.Name = ResGeneral.TradeSkills;
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
            g.Name = ResGeneral.Magic;
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
            g.Name = ResGeneral.Wilderness;
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
            g.Name = ResGeneral.Thieving;
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
            g.Name = ResGeneral.Bard;
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
            {
                return false;
            }

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
                    g.Name = ResGeneral.Miscellaneous;

                    SkillsGroup[] groups = new SkillsGroup[count];
                    groups[0] = g;

                    for (int i = 0; i < count - 1; ++i)
                    {
                        short strbuild;
                        bin.BaseStream.Seek(start + i * strlen, SeekOrigin.Begin);

                        if (unicode)
                        {
                            while ((strbuild = bin.ReadInt16()) != 0)
                            {
                                sb.Append((char)strbuild);
                            }
                        }
                        else
                        {
                            while ((strbuild = bin.ReadByte()) != 0)
                            {
                                sb.Append((char)strbuild);
                            }
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

                        if (grp < groups.Length && skillidx < SkillsLoader.Instance.SkillsCount)
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
                Log.Error($"Error while reading skillgrp.mul, using CUO defaults! exception given is: {e}");

                return false;
            }

            return Groups.Count != 0;
        }
    }
}