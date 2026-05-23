// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Concrete <see cref="ISkillsGroupStore"/>. Owns the
    /// <see cref="SkillsGroup"/> list, persists it to
    /// <c>skillsgroups.xml</c> in the active profile directory, and enforces
    /// the rule that the first group cannot be deleted (shows a
    /// <see cref="MessageBoxGump"/> on attempts to do so).
    /// </summary>
    internal sealed class SkillsGroupStore : ISkillsGroupStore
    {
        private readonly World _world;

        public SkillsGroupStore(World world)
        {
            _world = world;
        }

        public List<SkillsGroup> Groups { get; } = new List<SkillsGroup>();

        public void Add(SkillsGroup g)
        {
            Groups.Add(g);
        }

        public bool Remove(SkillsGroup g)
        {
            if (Groups[0] == g)
            {
                var camera = Client.Game.Scene.Camera;

                MessageBoxGump messageBox = new MessageBoxGump(_world, 200, 125, ResGeneral.CannotDeleteThisGroup, null)
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

        public void Load(ISkillsGroupDefaults defaults)
        {
            Groups.Clear();

            string path = Path.Combine(ProfileManager.ProfilePath, "skillsgroups.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No skillsgroups.xml file. Creating a default file.");

                defaults.MakeDefault(this);

                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                defaults.MakeDefault(this);

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

        public void Save()
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

                foreach (SkillsGroup k in Groups)
                {
                    k.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
    }
}
