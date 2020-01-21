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
        public static string GetUOFilePath(string file)
        {
            return Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, file);
        }


        public static void Load()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Task> tasks = new List<Task>
            {
                AnimationsLoader.Instance.Load(),
                AnimDataLoader.Instance.Load(),
                ArtLoader.Instance.Load(),
                MapLoader.Instance.Load(),
                ClilocLoader.Instance.Load(Settings.GlobalSettings.ClilocFile),
                GumpsLoader.Instance.Load(),
                FontsLoader.Instance.Load(),
                HuesLoader.Instance.Load(),
                TileDataLoader.Instance.Load(),
                MultiLoader.Instance.Load(),
                SkillsLoader.Instance.Load().ContinueWith(t => ProfessionLoader.Instance.Load()),
                TexmapsLoader.Instance.Load(),
                SpeechesLoader.Instance.Load(),
                LightsLoader.Instance.Load(),
                SoundsLoader.Instance.Load(),
                MultiMapLoader.Instance.Load()
            };

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
                        MapLoader.Instance.PatchMapBlock(vh.BlockID, vh.Position);
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
                    else if (vh.FileID == 14 && vh.BlockID < MultiLoader.Instance.Count)
                    {
                    }
                    else if (vh.FileID == 16 && vh.BlockID < SkillsLoader.Instance.SkillsCount)
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
            MapLoader.Instance?.Dispose();
            MapLoader.Instance = newloader;
        }
    }
}