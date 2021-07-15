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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal static class UOFileManager
    {
        public static string GetUOFilePath(string file)
        {
            if (UOFilesOverrideMap.Instance.TryGetValue(file.ToLowerInvariant(), out string uoFilePath))
            {
                return uoFilePath;
            }

            return Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, file);
        }


        public static void Load()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            UOFilesOverrideMap.Instance.Load(); // need to load this first so that it manages can perform the file overrides if needed

            List<Task> tasks = new List<Task>
            {
                AnimationsLoader.Instance.Load(),
                AnimDataLoader.Instance.Load(),
                ArtLoader.Instance.Load(),
                MapLoader.Instance.Load(),
                ClilocLoader.Instance.Load(Settings.GlobalSettings.Language),
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

            Read_Art_def();

            UOFileMul verdata = Verdata.File;

            bool useVerdata = Client.Version < ClientVersion.CV_500A || verdata != null && verdata.Length != 0 && Verdata.Patches.Length != 0;

            if (!Settings.GlobalSettings.UseVerdata && useVerdata)
            {
                Settings.GlobalSettings.UseVerdata = useVerdata;
            }

            Log.Trace($"Use verdata.mul: {(Settings.GlobalSettings.UseVerdata ? "Yes" : "No")}");

            if (Settings.GlobalSettings.UseVerdata)
            {
                if (verdata != null && Verdata.Patches.Length != 0)
                {
                    Log.Info(">> PATCHING WITH VERDATA.MUL");

                    for (int i = 0; i < Verdata.Patches.Length; i++)
                    {
                        ref UOFileIndex5D vh = ref Verdata.Patches[i];
                        Log.Info($">>> patching  FileID: {vh.FileID}  -  BlockID: {vh.BlockID}");

                        if (vh.FileID == 0)
                        {
                            MapLoader.Instance.PatchMapBlock(vh.BlockID, vh.Position);
                        }
                        else if (vh.FileID == 2)
                        {
                            MapLoader.Instance.PatchStaticBlock(vh.BlockID, ((ulong) verdata.StartAddress.ToInt64() + vh.Position), vh.Length);
                        }
                        else if (vh.FileID == 4)
                        {
                            if (vh.BlockID < ArtLoader.Instance.Entries.Length)
                            {
                                ArtLoader.Instance.Entries[vh.BlockID] = new UOFileIndex
                                (
                                    verdata.StartAddress,
                                    (uint) verdata.Length,
                                    vh.Position,
                                    (int) vh.Length,
                                    0
                                );
                            }
                        }
                        else if (vh.FileID == 12)
                        {
                            GumpsLoader.Instance.Entries[vh.BlockID] = new UOFileIndex
                            (
                                verdata.StartAddress,
                                (uint) verdata.Length,
                                vh.Position,
                                (int) vh.Length,
                                0,
                                (short) (vh.GumpData >> 16),
                                (short) (vh.GumpData & 0xFFFF)
                            );
                        }
                        else if (vh.FileID == 14 && vh.BlockID < MultiLoader.Instance.Count)
                        {
                            MultiLoader.Instance.Entries[vh.BlockID] = new UOFileIndex
                            (
                                verdata.StartAddress,
                                (uint) verdata.Length,
                                vh.Position,
                                (int) vh.Length,
                                0
                            );
                        }
                        else if (vh.FileID == 16 && vh.BlockID < SkillsLoader.Instance.SkillsCount)
                        {
                            SkillEntry skill = SkillsLoader.Instance.Skills[(int) vh.BlockID];

                            if (skill != null)
                            {
                                unsafe
                                {
                                    StackDataReader reader = new StackDataReader(new ReadOnlySpan<byte>((byte*)verdata.StartAddress, (int) verdata.Length));

                                    skill.HasAction = reader.ReadUInt8() != 0;
                                    skill.Name = reader.ReadASCII((int)(vh.Length - 1));

                                    reader.Release();
                                }
                            }
                        }
                        else if (vh.FileID == 30)
                        {
                            verdata.Seek(0);
                            verdata.Skip((int) vh.Position);

                            if (vh.Length == 836)
                            {
                                int offset = (int) (vh.BlockID * 32);

                                if (offset + 32 > TileDataLoader.Instance.LandData.Length)
                                {
                                    continue;
                                }

                                verdata.ReadUInt();

                                for (int j = 0; j < 32; j++)
                                {
                                    ulong flags;

                                    if (Client.Version < ClientVersion.CV_7090)
                                    {
                                        flags = verdata.ReadUInt();
                                    }
                                    else
                                    {
                                        flags = verdata.ReadULong();
                                    }

                                    TileDataLoader.Instance.LandData[offset + j] = new LandTiles(flags, verdata.ReadUShort(), verdata.ReadASCII(20));
                                }
                            }
                            else if (vh.Length == 1188)
                            {
                                int offset = (int) ((vh.BlockID - 0x0200) * 32);

                                if (offset + 32 > TileDataLoader.Instance.StaticData.Length)
                                {
                                    continue;
                                }

                                verdata.ReadUInt();

                                for (int j = 0; j < 32; j++)
                                {
                                    ulong flags;

                                    if (Client.Version < ClientVersion.CV_7090)
                                    {
                                        flags = verdata.ReadUInt();
                                    }
                                    else
                                    {
                                        flags = verdata.ReadULong();
                                    }

                                    TileDataLoader.Instance.StaticData[offset + j] = new StaticTiles
                                    (
                                        flags,
                                        verdata.ReadByte(),
                                        verdata.ReadByte(),
                                        verdata.ReadInt(),
                                        verdata.ReadUShort(),
                                        verdata.ReadUShort(),
                                        verdata.ReadUShort(),
                                        verdata.ReadByte(),
                                        verdata.ReadASCII(20)
                                    );
                                }
                            }
                        }
                        else if (vh.FileID == 32)
                        {
                            if (vh.BlockID < HuesLoader.Instance.HuesCount)
                            {
                                VerdataHuesGroup group = Marshal.PtrToStructure<VerdataHuesGroup>(verdata.StartAddress + (int) vh.Position);

                                HuesGroup[] hues = HuesLoader.Instance.HuesRange;

                                hues[vh.BlockID].Header = group.Header;

                                for (int j = 0; j < 8; j++)
                                {
                                    Array.Copy(group.Entries[j].ColorTable, hues[vh.BlockID].Entries[j].ColorTable, 32);
                                }
                            }
                        }
                        else if (vh.FileID != 5 && vh.FileID != 6)
                        {
                            Log.Warn($"Unused verdata block\tFileID: {vh.FileID}\tBlockID: {vh.BlockID}");
                        }
                    }

                    Log.Info("<< PATCHED.");
                }
            }


            Log.Trace($"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();
        }

        internal static void MapLoaderReLoad(MapLoader newloader)
        {
            MapLoader.Instance?.Dispose();
            MapLoader.Instance = newloader;
        }

        private static void Read_Art_def()
        {
            string pathdef = GetUOFilePath("art.def");

            if (File.Exists(pathdef))
            {
                TileDataLoader tiledataLoader =  TileDataLoader.Instance;
                ArtLoader artLoader = ArtLoader.Instance;
                
                using (DefReader reader = new DefReader(pathdef, 1))
                {
                    while (reader.Next())
                    {
                        int index = reader.ReadInt();

                        if (index < 0 || index >= Constants.MAX_LAND_DATA_INDEX_COUNT + tiledataLoader.StaticData.Length)
                        {
                            continue;
                        }

                        int[] group = reader.ReadGroup();

                        if (group == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < group.Length; i++)
                        {
                            int checkIndex = group[i];

                            if (checkIndex < 0 || checkIndex >= Constants.MAX_LAND_DATA_INDEX_COUNT + tiledataLoader.StaticData.Length)
                            {
                                continue;
                            }

                            if (index < artLoader.Entries.Length && checkIndex < artLoader.Entries.Length)
                            {
                                ref UOFileIndex currentEntry = ref artLoader.GetValidRefEntry(index);
                                ref UOFileIndex checkEntry = ref artLoader.GetValidRefEntry(checkIndex);

                                if (currentEntry.Equals(UOFileIndex.Invalid) && !checkEntry.Equals(UOFileIndex.Invalid))
                                {
                                    artLoader.Entries[index] = artLoader.Entries[checkIndex];
                                }
                            }

                            if (index < Constants.MAX_LAND_DATA_INDEX_COUNT &&
                                checkIndex < Constants.MAX_LAND_DATA_INDEX_COUNT && 
                                checkIndex < tiledataLoader.LandData.Length && 
                                index < tiledataLoader.LandData.Length &&
                                !tiledataLoader.LandData[checkIndex].Equals(default) &&
                                tiledataLoader.LandData[index].Equals(default))
                            {
                                tiledataLoader.LandData[index] = tiledataLoader.LandData[checkIndex];

                                break;
                            }

                            if (index >= Constants.MAX_LAND_DATA_INDEX_COUNT && checkIndex >= Constants.MAX_LAND_DATA_INDEX_COUNT &&
                                tiledataLoader.StaticData[index].Equals(default) && !tiledataLoader.StaticData[checkIndex].Equals(default))
                            {
                                tiledataLoader.StaticData[index] = tiledataLoader.StaticData[checkIndex];

                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}