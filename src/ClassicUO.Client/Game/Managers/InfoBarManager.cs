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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal sealed class InfoBarManager
    {
        private readonly World _world;
        private readonly List<InfoBarItem> infoBarItems;

        public InfoBarManager(World world)
        {
            infoBarItems = new List<InfoBarItem>();
            _world = world; 
        }

        public List<InfoBarItem> GetInfoBars()
        {
            return infoBarItems;
        }

        public static string[] GetVars()
        {
            if (!CUOEnviroment.IsOutlands)
            {
                return Enum.GetNames(typeof(InfoBarVars));
            }

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
            string path = Path.Combine(ProfileManager.ProfilePath, "infobar.xml");

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
            infoBarItems.Add(new InfoBarItem(ResGeneral.Hits, InfoBarVars.HP, 0x1B6));
            infoBarItems.Add(new InfoBarItem(ResGeneral.Mana, InfoBarVars.Mana, 0x1ED));
            infoBarItems.Add(new InfoBarItem(ResGeneral.Stam, InfoBarVars.Stamina, 0x22E));
            infoBarItems.Add(new InfoBarItem(ResGeneral.Weight, InfoBarVars.Weight, 0x3D2));
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
        NameNotoriety,
        TithingPoints
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
        NameNotoriety,
        TithingPoints
    }

    internal class InfoBarItem
    {
        public InfoBarItem(string label, InfoBarVars var, ushort labelColor)
        {
            this.label = label;
            this.var = var;
            hue = labelColor;
        }


        public InfoBarItem(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            label = xml.GetAttribute("text");
            var = (InfoBarVars) int.Parse(xml.GetAttribute("var"));
            hue = ushort.Parse(xml.GetAttribute("hue"));
        }

        public ushort hue;

        public string label;
        public InfoBarVars var;

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