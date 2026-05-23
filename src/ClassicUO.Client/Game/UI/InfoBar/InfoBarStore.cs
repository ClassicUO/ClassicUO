// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.InfoBar
{
    /// <summary>
    /// Concrete <see cref="IInfoBarStore"/>. Owns the
    /// <see cref="InfoBarItem"/> list and persists it to <c>infobar.xml</c>
    /// in the active profile directory.
    /// </summary>
    internal sealed class InfoBarStore : IInfoBarStore
    {
        public List<InfoBarItem> Items { get; } = new List<InfoBarItem>();

        public void Add(InfoBarItem item)
        {
            Items.Add(item);
        }

        public void Remove(InfoBarItem item)
        {
            Items.Remove(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void Load(IInfoBarDefaults defaults)
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "infobar.xml");

            if (!File.Exists(path))
            {
                defaults.CreateDefault(this);
                Save();

                return;
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

            Items.Clear();

            XmlElement root = doc["infos"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("info"))
                {
                    InfoBarItem item = new InfoBarItem(xml);
                    Items.Add(item);
                }
            }
        }

        public void Save()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "infobar.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("infos");

                foreach (InfoBarItem info in Items)
                {
                    info.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
    }
}
