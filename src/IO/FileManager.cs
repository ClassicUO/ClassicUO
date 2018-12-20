#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.IO;

using ClassicUO.Configuration;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal static class FileManager
    {
        private static string _uofolderpath;

        public static string UoFolderPath
        {
            get => _uofolderpath;
            set
            {
                _uofolderpath = value;
                //FileInfo client = new FileInfo(Path.Combine(value, "client.exe"));
                //if (!client.Exists)
                //    throw new FileNotFoundException();

                //FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(client.FullName);

                if (!Version.TryParse(Service.Get<Settings>().ClientVersion.Replace(",", ".").Trim(), out Version version))
                {
                    Log.Message(LogTypes.Error, "Wrong version.");

                    throw new InvalidDataException("Wrong version");
                }

                ClientVersion = (ClientVersions) ((version.Major << 24) | (version.Minor << 16) | (version.Build << 8) | version.Revision);
                Log.Message(LogTypes.Trace, $"Client version: {version} - {ClientVersion}");
            }
        }

        public static ClientVersions ClientVersion { get; private set; }

        public static bool IsUOPInstallation => ClientVersion >= ClientVersions.CV_70240;

        public static ushort GraphicMask => IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;

        public static bool UseUOPGumps { get; set; }

        public static AnimationsLoader Animations { get; private set; }
        public static AnimDataLoader AnimData { get; private set; }
        public static ArtLoader Art { get; private set; }
        public static MapLoader Map { get; private set; }
        public static ClilocLoader Cliloc { get; private set; }
        public static GumpsLoader Gumps { get; private set; }
        public static FontsLoader Fonts { get; private set; }    
        public static HuesLoader Hues { get; private set; }
        public static TileDataLoader TileData { get; private set; }
        public static MultiLoader Multi { get; private set; }
        public static SkillsLoader Skills { get; private set; }
        public static TexmapsLoader Textmaps { get; private set; }
        public static SpeechesLoader Speeches { get; private set; }
        public static LightsLoader Lights { get; private set; }

        public static void LoadFiles()
        {
            Animations = new AnimationsLoader();
            Animations.Load();

            AnimData = new AnimDataLoader();
            AnimData.Load();

            Art = new ArtLoader();
            Art.Load();

            Map = new MapLoader();
            Map.Load();

            Cliloc = new ClilocLoader();
            Cliloc.Load();

            Gumps = new GumpsLoader();
            Gumps.Load();

            Fonts = new FontsLoader();
            Fonts.Load();

            Hues = new HuesLoader();
            Hues.Load();

            TileData = new TileDataLoader();
            TileData.Load();

            Multi = new MultiLoader();
            Multi.Load();

            Skills = new SkillsLoader();
            Skills.Load();

            Textmaps = new TexmapsLoader();
            Textmaps.Load();

            Speeches = new SpeechesLoader();
            Speeches.Load();

            Lights = new LightsLoader();
            Lights.Load();

            //Map.Load();
            //Art.Load();
            //Cliloc.Load();
            //Animations.Load();
            //Gumps.Load();
            //Fonts.Load();
            //FileManager.Hues.Load();
            //TileData.Load();
            //Multi.Load();
            //Skills.Load();
            //TextmapTextures.Load();
            //Speeches.Load();
            //AnimData.Load();
            //Light.Load();
        }
    }
}