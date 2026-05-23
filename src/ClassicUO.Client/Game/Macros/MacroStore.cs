// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Concrete <see cref="IMacroStore"/>. Reads / writes <c>macros.xml</c>
    /// off the active profile directory and writes the built-in default macro
    /// set (Paperdoll, Options, Journal, Backpack, Radar, Bow, Salute) the
    /// first time the profile has no file. Operates on the host
    /// <see cref="MacroManager"/>'s inherited <see cref="LinkedObject"/>
    /// state — no per-store list of its own.
    /// </summary>
    internal sealed class MacroStore : IMacroStore
    {
        public void Load(MacroManager host)
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No macros.xml file. Creating a default file.");

                host.Clear();
                CreateDefaultMacros(host);
                Save(host);

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


            host.Clear();

            XmlElement root = doc["macros"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("macro"))
                {
                    Macro macro = new Macro(xml.GetAttribute("name"));
                    macro.Load(xml);
                    host.PushToBack(macro);
                }
            }
        }

        public void Save(MacroManager host)
        {
            List<Macro> list = GetAllMacros(host);

            string path = Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("macros");

                foreach (Macro macro in list)
                {
                    macro.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        public List<Macro> GetAllMacros(MacroManager host)
        {
            Macro m = (Macro) host.Items;

            while (m?.Previous != null)
            {
                m = (Macro) m.Previous;
            }

            List<Macro> macros = new List<Macro>();

            while (true)
            {
                if (m != null)
                {
                    macros.Add(m);
                }
                else
                {
                    break;
                }

                m = (Macro) m.Next;
            }

            return macros;
        }

        private static void CreateDefaultMacros(MacroManager host)
        {
            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Paperdoll,
                    (SDL.SDL_Keycode) 112,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 10)
                    {
                        SubMenuType = 1
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Options,
                    (SDL.SDL_Keycode) 111,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 9)
                    {
                        SubMenuType = 1
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Journal,
                    (SDL.SDL_Keycode) 106,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 12)
                    {
                        SubMenuType = 1
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Backpack,
                    (SDL.SDL_Keycode) 105,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 16)
                    {
                        SubMenuType = 1
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Radar,
                    (SDL.SDL_Keycode) 114,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 8, (MacroSubType) 17)
                    {
                        SubMenuType = 1
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Bow,
                    (SDL.SDL_Keycode) 98,
                    false,
                    true,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 18, 0)
                    {
                        SubMenuType = 0
                    }
                }
            );

            host.PushToBack
            (
                new Macro
                (
                    ResGeneral.Salute,
                    (SDL.SDL_Keycode) 115,
                    false,
                    true,
                    false
                )
                {
                    Items = new MacroObject((MacroType) 19, 0)
                    {
                        SubMenuType = 0
                    }
                }
            );
        }
    }
}
