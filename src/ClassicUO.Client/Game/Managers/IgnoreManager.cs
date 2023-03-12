using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class IgnoreManager
    {
        /// <summary>
        /// Set of Char names
        /// </summary>
        public static HashSet<string> IgnoredCharsList = new HashSet<string>();

        /// <summary>
        /// Initialize Ignore Manager
        /// - Load List from XML file
        /// </summary>
        public static void Initialize()
        {
            ReadIgnoreList();
        }

        /// <summary>
        /// Add Char to ignored list
        /// </summary>
        /// <param name="entity">Targeted Entity</param>
        public static void AddIgnoredTarget(Entity entity)
        {
            if (entity is Mobile m && !m.IsYellowHits && m.Serial != World.Player.Serial)
            {
                var charName = m.Name;

                if (IgnoredCharsList.Contains(charName))
                {
                    GameActions.Print(string.Format(ResGumps.AddToIgnoreListExist, charName));
                    return;
                }

                IgnoredCharsList.Add(charName);
                // Redraw list of chars
                UIManager.GetGump<IgnoreManagerGump>()?.Redraw();

                GameActions.Print(string.Format(ResGumps.AddToIgnoreListSuccess, charName));
                return;
            }

            GameActions.Print(string.Format(ResGumps.AddToIgnoreListNotMobile));
        }

        /// <summary>
        /// Remove Char from Ignored List
        /// </summary>
        /// <param name="charName">Char name</param>
        public static void RemoveIgnoredTarget(string charName)
        {
            if (IgnoredCharsList.Contains(charName))
                IgnoredCharsList.Remove(charName);
        }

        /// <summary>
        /// Load Ignored List from XML file
        /// </summary>
        private static void ReadIgnoreList()
        {
            HashSet<string> list = new HashSet<string>();

            string ignoreXmlPath = Path.Combine(ProfileManager.ProfilePath, "ignore_list.xml");

            if (!File.Exists(ignoreXmlPath))
            {
                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(ignoreXmlPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            XmlElement root = doc["ignore"];

            if (root != null)
            {
                foreach (XmlElement xml in root.ChildNodes)
                {
                    if (xml.Name != "info")
                    {
                        continue;
                    }

                    string charName = xml.GetAttribute("charname");
                    list.Add(charName);
                }
            }

            IgnoredCharsList = list;
        }

        /// <summary>
        /// Save List to XML File
        /// </summary>
        public static void SaveIgnoreList()
        {
            string ignoreXmlPath = Path.Combine(ProfileManager.ProfilePath, "ignore_list.xml");

            using (XmlTextWriter xml = new XmlTextWriter(ignoreXmlPath, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("ignore");

                foreach (var ch in IgnoredCharsList)
                {
                    xml.WriteStartElement("info");
                    xml.WriteAttributeString("charname", ch);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
    }
}
