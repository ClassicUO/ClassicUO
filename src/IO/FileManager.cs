﻿#region license

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
using System.Diagnostics;
using System.IO;

using ClassicUO.Game;
using ClassicUO.Game.Managers;
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

                string[] vers = Engine.GlobalSettings.ClientVersion.ToLower().Split('.');

                if (vers.Length < 3)
                {
                    Log.Message(LogTypes.Error, "Invalid UO version.");

                    throw new InvalidDataException("Invalid UO version");
                }

                int major = int.Parse(vers[0]);
                int minor = int.Parse(vers[1]);
                int extra = 0;

                if (!int.TryParse(vers[2], out int build))
                {
                    int index = vers[2].IndexOf('.');

                    if (index != -1)
                    {
                        build = int.Parse(vers[2].Substring(0, index));
                    }
                    else
                    {
                        int i = 0;

                        for (; i < vers[2].Length; i++)
                        {
                            if (!char.IsNumber(vers[2][i]))
                            {
                                build = int.Parse(vers[2].Substring(0, i));
                                break;
                            }
                        }

                        if (i < vers[2].Length)
                        {
                            extra = (sbyte) vers[2].Substring(i, vers[2].Length - i)[0];

                            char start = 'a';
                            index = 0;
                            while (start != extra && start <= 'z')
                            {
                                start++;
                                index++;
                            }

                            extra = index;
                        }
                    }
                }

                if (vers.Length > 3)
                    extra = int.Parse(vers[3]);

                ClientBufferVersion[0] = (byte) major;
                ClientBufferVersion[1] = (byte) minor;
                ClientBufferVersion[2] = (byte) build;
                ClientBufferVersion[3] = (byte) extra;

                ClientVersion = (ClientVersions) (((major & 0xFF) << 24) | ((minor & 0xFF) << 16) | ((build & 0xFF) << 8) | (extra & 0xFF));
                Log.Message(LogTypes.Trace, $"Client version: {Engine.GlobalSettings.ClientVersion} - {ClientVersion}");
            }
        }

        public static byte[] ClientBufferVersion { get; } = new byte[4];

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
        public static SoundsLoader Sounds { get; private set; }
        public static MultiMapLoader Multimap { get; private set; }
        public static ProfessionLoader Profession { get; private set; }

        public static void LoadFiles()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Animations = new AnimationsLoader();
            Animations.Load();

            AnimData = new AnimDataLoader();
            AnimData.Load();

            Art = new ArtLoader();
            Art.Load();

            Map = new MapLoader();
            Map.Load();

            Cliloc = new ClilocLoader();
            Cliloc.Load(Engine.GlobalSettings.ClilocFile);

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

            Sounds = new SoundsLoader();
            Sounds.Load();

            Multimap = new MultiMapLoader();
            Multimap.Load();

            Profession = new ProfessionLoader();
            Profession.Load();


            var verdata = Verdata.File;

            if (verdata != null && Verdata.Patches.Length != 0)
            {
                for (int i = 0; i < Verdata.Patches.Length; i++)
                {
                    UOFileIndex5D vh = Verdata.Patches[i];

                    if (vh.FileID == 0)
                        Map.PatchMapBlock(vh.BlockID, vh.Position);
                    else if (vh.FileID == 4)
                    {
                        if (vh.BlockID >= Constants.MAX_LAND_DATA_INDEX_COUNT)
                        {
                            ushort id = (ushort) (vh.BlockID - Constants.MAX_LAND_DATA_INDEX_COUNT);
                        }
                    }
                    else if (vh.FileID == 12)
                    {
                    }
                    else if (vh.FileID == 14 && vh.BlockID < Multi.Count)
                    {
                    }
                    else if (vh.FileID == 16 && vh.BlockID < Skills.SkillsCount)
                    {
                    }
                    else if (vh.FileID == 30)
                    {
                    }
                    else if (vh.FileID == 32)
                    {
                    }
                }
            }

            SkillsGroupManager.LoadDefault();

            Log.Message(LogTypes.Trace, $"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();
        }

        internal static void MapLoaderReLoad(MapLoader newloader)
        {
            Map?.Dispose();
            Map = newloader;
        }
    }
}