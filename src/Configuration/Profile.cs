#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal sealed class Profile
    {
        [JsonConstructor]
        public Profile(string username, string servername, string charactername)
        {
            Username = username;
            ServerName = servername;
            CharacterName = charactername;
        }

        [JsonProperty]
        public string Username { get; }
        [JsonProperty]
        public string ServerName { get; }
        [JsonProperty]
        public string CharacterName { get; }

        // sounds
        [JsonProperty] public bool EnableSound { get; set; } = true;
        [JsonProperty] public int SoundVolume { get; set; } = 100;
        [JsonProperty] public bool EnableMusic { get; set; } = true;
        [JsonProperty] public int MusicVolume { get; set; } = 100;
        [JsonProperty] public bool EnableFootstepsSound { get; set; } = true;
        [JsonProperty] public bool EnableCombatMusic { get; set; } = true;
        [JsonProperty] public bool ReproduceSoundsInBackground { get; set; } = false;

        // fonts and speech
        [JsonProperty] public byte ChatFont { get; set; } = 1;
        [JsonProperty] public int SpeechDelay { get; set; } = 100;
        [JsonProperty] public bool ScaleSpeechDelay { get; set; } = true;

        // hues
        [JsonProperty] public ushort SpeechHue { get; set; } = 0x02B2;
        [JsonProperty] public ushort EmoteHue { get; set; } = 0x0021;
        [JsonProperty] public ushort PartyMessageHue { get; set; } = 0x0044;
        [JsonProperty] public ushort GuildMessageHue { get; set; } = 0x0044;
        [JsonProperty] public ushort AllyMessageHue { get; set; } = 0x0057;
        [JsonProperty] public ushort InnocentHue { get; set; } = 0x005A;
        [JsonProperty] public ushort FriendHue { get; set; } = 0x0044;
        [JsonProperty] public ushort CriminalHue { get; set; } = 0x03B2;
        [JsonProperty] public ushort AnimalHue { get; set; } = 0x03B2;
        [JsonProperty] public ushort EnemyHue { get; set; } = 0x0031;
        [JsonProperty] public ushort MurdererHue { get; set; } = 0x0023;

        // visual
        [JsonProperty] public bool EnabledCriminalActionQuery { get; set; } = true;
        [JsonProperty] public bool ShowIncomingNames { get; set; } = true;
        [JsonProperty] public bool EnableStatReport { get; set; } = true;
        [JsonProperty] public bool EnableSkillReport { get; set; } = true;
        [JsonProperty] public bool UseOldStatusGump { get; set; } = false;
        [JsonProperty] public int BackpackStyle { get; set; } = 0;
        [JsonProperty] public bool HighlightGameObjects { get; set; } = true;
        [JsonProperty] public bool HighlightMobilesByFlags { get; set; } = true;
        [JsonProperty] public bool ShowMobilesHP { get; set; } = false;
        [JsonProperty] public int MobileHPType { get; set; } = 0;
        [JsonProperty] public bool DrawRoofs { get; set; } = true;
        [JsonProperty] public bool TreeToStumps { get; set; } = false;
        [JsonProperty] public bool EnableCaveBorder { get; set; } = false;
        [JsonProperty] public bool HideVegetation { get; set; } = false;
        [JsonProperty] public int FieldsType { get; set; } = 0; // 0 = normal, 1 = static, 2 = tile
        [JsonProperty] public bool NoColorObjectsOutOfRange { get; set; } = false;
        [JsonProperty] public bool UseCircleOfTransparency { get; set; } = false;
        [JsonProperty] public int CircleOfTransparencyRadius { get; set; } = 5;
        [JsonProperty] public bool EnableScaleZoom { get; set; } = false;
        [JsonProperty] public bool SaveScaleAfterClose { get; set; } = false;
        [JsonProperty] public float ScaleZoom { get; set; } = 1.0f;

        // tooltip
        [JsonProperty] public bool EnableTooltip { get; set; } = true;
        [JsonProperty] public int DelayShowTooltip { get; set; } = 250;
        [JsonProperty] public ushort TooltipTextHue { get; set; } = 0xFFFF;

        // movements
        [JsonProperty] public bool EnablePathfind { get; set; } = true;
        [JsonProperty] public bool AlwaysRun { get; set; }
        [JsonProperty] public bool SmoothMovements { get; set; } = true;
        [JsonProperty] public bool HoldDownKeyTab { get; set; } = true;

        // general
        [JsonProperty] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonProperty] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        [JsonProperty] public bool GameWindowLock { get; set; } = false;
        [JsonProperty] public bool GameWindowFullSize { get; set; } = false;
        [JsonProperty] public Point GameWindowSize { get; set; } = new Point(600, 480);
        [JsonProperty] public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        [JsonProperty] public bool TopbarGumpIsMinimized { get; set; } = false;
        [JsonProperty] public bool TopbarGumpIsDisabled { get; set; } = false;

        [JsonProperty] public int MaxFPS { get; set; } = 60;

        [JsonProperty] public Macro[] Macros { get; set; } = new Macro[0];


        public void Save(List<Gump> gumps = null)
        {
            if (string.IsNullOrEmpty(ServerName))
                throw new InvalidDataException();
            if (string.IsNullOrEmpty(Username))
                throw new InvalidDataException();
            if (string.IsNullOrEmpty(CharacterName))
                throw new InvalidDataException();

            string path = FileSystemHelper.CreateFolderIfNotExists(Engine.ExePath, "Data", "Profiles", Username, ServerName, CharacterName);

            Log.Message(LogTypes.Trace, $"Saving path:\t\t{path}");

            // save settings.json
            ConfigurationResolver.Save(this, Path.Combine(path, "settings.json"));

            // save gumps.bin
            SaveGumps(path, gumps);

            Log.Message(LogTypes.Trace, "Saving done!");
        }

        private void SaveGumps(string path, List<Gump> gumps)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(path, "gumps.bin"))))
            {
                const uint VERSION = 1;

                writer.Write(VERSION);
                writer.Write(0);

                /*
                 * int gumpsCount
                 * loop:
                 *      ushort typeLen
                 *      string type
                 *      int x
                 *      int y
                 *      undefinited data
                 * endloop.
                 */


                if (gumps != null)
                { 
                    writer.Write(gumps.Count);

                    foreach (Gump gump in gumps)
                    {
                        gump.Save(writer);
                    }
                }
                else
                {
                    writer.Write(0);
                }
            }
        }

        public List<Gump> ReadGumps()
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(Engine.ExePath, "Data", "Profiles", Username, ServerName, CharacterName);

            string binpath = Path.Combine(path, "gumps.bin");
            if (!File.Exists(binpath))
                return null;

            List<Gump> gumps = new List<Gump>();

            using (BinaryReader reader = new BinaryReader(File.OpenRead(binpath)))
            {
                if (reader.BaseStream.Position + 12 < reader.BaseStream.Length)
                {
                    uint version = reader.ReadUInt32();
                    uint empty = reader.ReadUInt32();

                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            int typeLen = reader.ReadUInt16();
                            string typeName = reader.ReadUTF8String(typeLen);
                            int x = reader.ReadInt32();
                            int y = reader.ReadInt32();

                            Type type = Type.GetType(typeName, true);
                            Gump gump = (Gump)Activator.CreateInstance(type);
                            gump.Initialize();
                            gump.Restore(reader);
                            gump.X = x;
                            gump.Y = y;

                            if (gump.LocalSerial != 0)
                                Engine.UI.SavePosition(gump.LocalSerial, new Point(x, y));

                            if (!gump.IsDisposed)
                            {
                                gumps.Add(gump);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Message(LogTypes.Error, e.Message);
                        }
                      
                    }
                }
            }

            return gumps;
        }

        
    }
}
