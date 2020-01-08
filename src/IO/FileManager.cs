#region license

//  Copyright (C) 2020 ClassicUO Development Community on Github
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal static class UOFileManager
    {
        private static string _uofolderpath;

        public static string UoFolderPath
        {
            get => _uofolderpath;
            set
            {
                _uofolderpath = value;
            }
        }

        public static string GetUOFilePath(string file)
        {
            return Path.Combine(UoFolderPath, file);
        }



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

        public static void Load()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Task> tasks = new List<Task>();

            Animations = new AnimationsLoader();
            tasks.Add(Animations.Load());

            AnimData = new AnimDataLoader();
            tasks.Add(AnimData.Load());

            Art = new ArtLoader();
            tasks.Add(Art.Load());

            Map = new MapLoader();
            tasks.Add(Map.Load());

            Cliloc = new ClilocLoader();
            tasks.Add(Cliloc.Load(Settings.GlobalSettings.ClilocFile));

            Gumps = new GumpsLoader();
            tasks.Add(Gumps.Load());

            Fonts = new FontsLoader();
            tasks.Add(Fonts.Load());

            Hues = new HuesLoader();
            tasks.Add(Hues.Load());

            TileData = new TileDataLoader();
            tasks.Add(TileData.Load());

            Multi = new MultiLoader();
            tasks.Add(Multi.Load());

            Skills = new SkillsLoader();
            tasks.Add(Skills.Load());

            Textmaps = new TexmapsLoader();
            tasks.Add(Textmaps.Load());

            Speeches = new SpeechesLoader();
            tasks.Add(Speeches.Load());

            Lights = new LightsLoader();
            tasks.Add(Lights.Load());

            Sounds = new SoundsLoader();
            tasks.Add(Sounds.Load());

            Multimap = new MultiMapLoader();
            tasks.Add(Multimap.Load());

            Profession = new ProfessionLoader();
            tasks.Add(Profession.Load());


            if (!Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10)))
            {
                Log.Panic("Loading files timeout.");
            }

            var verdata = Verdata.File;

            if (verdata != null && Verdata.Patches.Length != 0)
            {
                for (int i = 0; i < Verdata.Patches.Length; i++)
                {
                    ref UOFileIndex5D vh = ref Verdata.Patches[i];

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

            Log.Trace( $"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();
        }

        internal static void MapLoaderReLoad(MapLoader newloader)
        {
            Map?.Dispose();
            Map = newloader;
        }
    }
}