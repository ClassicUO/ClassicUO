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
using ClassicUO.Utility.Logging;

using Newtonsoft.Json;



namespace ClassicUO.Game.Managers
{
    internal class InfoBarManager
    {
        private List<InfoBarItem> infoBarItems;

        public InfoBarManager()
        {
            infoBarItems = new List<InfoBarItem>();

            if (ProfileManager.Current.InfoBarItems != null)
            {
                infoBarItems.AddRange(ProfileManager.Current.InfoBarItems);

                ProfileManager.Current.InfoBarItems = null;
                Save();
            }
        }

        public List<InfoBarItem> GetInfoBars()
        {
            return infoBarItems;
        }

        public static string[] GetVars(int shardtype)
        {
            if (shardtype != 2)
                return Enum.GetNames(typeof(InfoBarVars));
            else
                return Enum.GetNames(typeof(InfoBarVarsOutlands));
        }

        public void AddItem(InfoBarItem ibi)
        {
            infoBarItems.Add(ibi);
        }

        public void RemoveItem(InfoBarItem item)
        {
            infoBarItems.Remove(item);
        }

        public void Clear()
        {
            infoBarItems.Clear();
        }

        public void Save()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", ProfileManager.Current.Username, ProfileManager.Current.ServerName, ProfileManager.Current.CharacterName, "infobar.xml");
            
            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = System.Xml.Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("infos");

                foreach (InfoBarItem info in infoBarItems)
                {
                    info.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        public void Load()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", ProfileManager.Current.Username, ProfileManager.Current.ServerName, ProfileManager.Current.CharacterName, "infobar.xml");

            if (!File.Exists(path))
            {
                CreateDefault();
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

            infoBarItems.Clear();

            XmlElement root = doc["infos"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("info"))
                {
                   InfoBarItem item = new InfoBarItem(xml);
                   infoBarItems.Add(item);
                }
            }
        }

        public void CreateDefault()
        {
            infoBarItems.Clear();

            infoBarItems.Add(new InfoBarItem("", InfoBarVars.NameNotoriety, 0x3D2));
            infoBarItems.Add(new InfoBarItem("Hits", InfoBarVars.HP, 0x1B6));
            infoBarItems.Add(new InfoBarItem("Mana", InfoBarVars.Mana, 0x1ED));
            infoBarItems.Add(new InfoBarItem("Stam", InfoBarVars.Stamina, 0x22E));
            infoBarItems.Add(new InfoBarItem("Weight", InfoBarVars.Weight, 0x3D2));
        }
    }

    internal enum InfoBarVars
    {
        HP = 0,
        Mana,
        Stamina,
        Weight,
        Followers,
        Gold,
        Damage,
        Armor,
        Luck,
        FireResist,
        ColdResist,
        PoisonResist,
        EnergyResist,
        LowerReagentCost,
        SpellDamageInc,
        FasterCasting,
        FasterCastRecovery,
        HitChanceInc,
        DefenseChanceInc,
        LowerManaCost,
        DamageChanceInc,
        SwingSpeedInc,
        StatsCap,
        NameNotoriety
    }

    internal enum InfoBarVarsOutlands
    {
        HP = 0,
        Mana,
        Stamina,
        Weight,
        Followers,
        Gold,
        Damage,
        Armor,
        FoodSatisfaction,
        MurderTimer,
        CriminalTimer,
        PvpCooldown,
        BandageTimer,
        LowerReagentCost,
        SpellDamageInc,
        FasterCasting,
        FasterCastRecovery,
        HitChanceInc,
        DefenseChanceInc,
        LowerManaCost,
        DamageChanceInc,
        SwingSpeedInc,
        MurderCount,
        NameNotoriety
    }

    [JsonObject]
    internal class InfoBarItem
    {
        [JsonConstructor]
        public InfoBarItem(string label, InfoBarVars var, ushort labelColor)
        {
            this.label = label;
            this.var = var;
            hue = labelColor;
        }

        
        public InfoBarItem(XmlElement xml)
        {
            if (xml == null)
                return;
            label = xml.GetAttribute("text");
            var = (InfoBarVars) int.Parse(xml.GetAttribute("var"));
            hue = ushort.Parse(xml.GetAttribute("hue"));
        }

        [JsonProperty] public string label;
        [JsonProperty] public InfoBarVars var;
        [JsonProperty] public ushort hue;

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("info");
            writer.WriteAttributeString("text", label);
            writer.WriteAttributeString("var", ((int) var).ToString());
            writer.WriteAttributeString("hue", hue.ToString());
            writer.WriteEndElement();
        }
    }
}
