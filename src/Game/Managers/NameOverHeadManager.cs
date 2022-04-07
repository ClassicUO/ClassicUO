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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using SDL2;

namespace ClassicUO.Game.Managers
{
    [Flags]
    internal enum NameOverheadTypeAllowed
    {
        All,
        Mobiles,
        Items,
        Corpses,
        MobilesCorpses = Mobiles | Corpses
    }


    [Flags]
    internal enum NameOverheadOptions
    {
        None = 0,

        // Items
        Containers = 1 << 0,
        Gold = 1 << 1,
        Stackable = 1 << 2,
        Other = 1 << 3,

        // Corpses
        MonsterCorpses = 1 << 4,
        HumanoidCorpses = 1 << 5,
        OwnCorpses = 1 << 6,

        // Mobiles (type)
        Humanoid = 1 << 7,
        Monster = 1 << 8,
        OwnFollowers = 1 << 9,

        // Mobiles (notoriety)
        Innocent = 1 << 10,
        Ally = 1 << 11,
        Gray = 1 << 12,
        Criminal = 1 << 13,
        Enemy = 1 << 14,
        Murderer = 1 << 15,
        Invulnerable = 1 << 16,

        AllItems = Containers | Gold | Stackable | Other,
        AllMobiles = Humanoid | Monster,
        MobilesAndCorpses = AllMobiles | MonsterCorpses | HumanoidCorpses,
    }

    internal static class NameOverHeadManager
    {
        private static NameOverHeadHandlerGump _gump;

        public static NameOverheadTypeAllowed TypeAllowed
        {
            get => ProfileManager.CurrentProfile.NameOverheadTypeAllowed;
            set => ProfileManager.CurrentProfile.NameOverheadTypeAllowed = value;
        }

        public static string LastActiveNameOverheadOption
        {
            get => ProfileManager.CurrentProfile.LastActiveNameOverheadOption;
            set => ProfileManager.CurrentProfile.LastActiveNameOverheadOption = value;
        }

        public static NameOverheadOptions ActiveOverheadOptions { get; set; }

        public static bool IsToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadToggled;
            set => ProfileManager.CurrentProfile.NameOverheadToggled = value;
        }

        public static List<NameOverheadOption> Options { get; set; } = new();

        public static bool IsAllowed(Entity serial)
        {
            if (serial == null)
                return false;

            if (SerialHelper.IsItem(serial))
                return HandleItemOverhead(serial);

            if (SerialHelper.IsMobile(serial))
                return HandleMobileOverhead(serial);

            return false;
        }

        private static bool HandleMobileOverhead(Entity serial)
        {
            var mobile = serial as Mobile;

            if (mobile == null)
                return false;

            // Mobile types
            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Humanoid) && mobile.IsHuman)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Monster) && !mobile.IsHuman)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.OwnFollowers) && mobile.IsRenamable && mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.NotorietyFlag != NotorietyFlag.Enemy)
                return true;

            // Mobile notorieties
            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Innocent) && mobile.NotorietyFlag == NotorietyFlag.Innocent)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Ally) && mobile.NotorietyFlag == NotorietyFlag.Ally)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gray) && mobile.NotorietyFlag == NotorietyFlag.Gray)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Criminal) && mobile.NotorietyFlag == NotorietyFlag.Criminal)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Enemy) && mobile.NotorietyFlag == NotorietyFlag.Enemy)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Murderer) && mobile.NotorietyFlag == NotorietyFlag.Murderer)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Invulnerable) && mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                return true;

            return false;
        }

        private static bool HandleItemOverhead(Entity serial)
        {
            var item = serial as Item;

            if (item == null)
                return false;

            if (item.IsCorpse)
            {
                return HandleCorpseOverhead(item);
            }

            if (item.ItemData.IsContainer && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Containers))
                return true;

            if (item.IsCoin && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gold))
                return true;

            if (item.ItemData.IsStackable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Stackable))
                return true;

            return !item.ItemData.IsContainer && !item.IsCoin && !item.ItemData.IsStackable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Other);
        }

        private static bool HandleCorpseOverhead(Item item)
        {
            var isHumanCorpse = item.IsHumanCorpse;

            if (isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.HumanoidCorpses))
                return true;

            if (!isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.MonsterCorpses))
                return true;

            // TODO: Add support for IsOwnCorpse, which was coded by Dyru
            return false;
        }

        public static void Open()
        {
            if (_gump == null || _gump.IsDisposed)
            {
                _gump = new NameOverHeadHandlerGump();
                UIManager.Add(_gump);
            }

            _gump.IsEnabled = true;
            _gump.IsVisible = true;
        }

        public static void Close()
        {
            if (_gump == null)
                return;

            _gump.IsEnabled = false;
            _gump.IsVisible = false;
        }

        public static void ToggleOverheads()
        {
            IsToggled = !IsToggled;
        }

        public static void Load()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No nameoverhead.xml file. Creating a default file.");


                Options.Clear();
                CreateDefaultEntries();
                Save();

                return;
            }

            Options.Clear();
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


            XmlElement root = doc["nameoverhead"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("nameoverheadoption"))
                {
                    var option = new NameOverheadOption(xml.GetAttribute("name"));
                    option.Load(xml);
                    Options.Add(option);
                }
            }
        }

        public static void Save()
        {
            var list = Options;

            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
                   {
                       Formatting = Formatting.Indented,
                       IndentChar = '\t',
                       Indentation = 1
                   })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("nameoverhead");

                foreach (var option in list)
                {
                    option.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        private static void CreateDefaultEntries()
        {
            Options.AddRange
            (
                new[]
                {
                    new NameOverheadOption("All", int.MaxValue),
                    new NameOverheadOption("Mobiles only", (int)NameOverheadOptions.AllMobiles),
                    new NameOverheadOption("Items only", (int)NameOverheadOptions.AllItems),
                    new NameOverheadOption("Mobiles & Corpses only", (int)NameOverheadOptions.MobilesAndCorpses),
                    new NameOverheadOption("Only Allies", (int)(NameOverheadOptions.Ally | NameOverheadOptions.Innocent | NameOverheadOptions.OwnFollowers)),
                    new NameOverheadOption("My Followers", (int)NameOverheadOptions.OwnFollowers),
                    new NameOverheadOption("Stackable items", (int)NameOverheadOptions.Stackable),
                    new NameOverheadOption("Stuff I can attack", (int)(NameOverheadOptions.Monster | NameOverheadOptions.Gray | NameOverheadOptions.Murderer)),
                }
            );
        }
    }

    internal class NameOverheadOption : LinkedObject, IEquatable<NameOverheadOption>
    {
        public NameOverheadOption(string name, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift, int optionflagscode) : this(name)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
            NameOverheadOptionFlags = optionflagscode;
        }

        public NameOverheadOption(string name)
        {
            Name = name;
        }

        public NameOverheadOption(string name, int optionflagcode)
        {
            Name = name;
            NameOverheadOptionFlags = optionflagcode;
        }

        public string Name { get; }

        public SDL.SDL_Keycode Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public int NameOverheadOptionFlags { get; set; }

        public bool Equals(NameOverheadOption other)
        {
            if (other == null)
            {
                return false;
            }

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift && Name == other.Name;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("nameoverheadoption");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("key", ((int)Key).ToString());
            writer.WriteAttributeString("alt", Alt.ToString());
            writer.WriteAttributeString("ctrl", Ctrl.ToString());
            writer.WriteAttributeString("shift", Shift.ToString());
            writer.WriteAttributeString("optionflagscode", NameOverheadOptionFlags.ToString());

            writer.WriteEndElement();
        }

        public void Load(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            Key = (SDL.SDL_Keycode)int.Parse(xml.GetAttribute("key"));
            Alt = bool.Parse(xml.GetAttribute("alt"));
            Ctrl = bool.Parse(xml.GetAttribute("ctrl"));
            Shift = bool.Parse(xml.GetAttribute("shift"));
            NameOverheadOptionFlags = int.Parse(xml.GetAttribute("optionflagscode"));
        }
    }
}
