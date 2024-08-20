﻿#region license

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

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class UOFileManager : IDisposable
    {
        private readonly UOFilesOverrideMap _overrideMap;

        public UOFileManager(ClientVersion clientVersion, string uoPath)
        {
            Version = clientVersion;
            BasePath = uoPath;

            Animations = new AnimationsLoader(this);
            AnimData = new AnimDataLoader(this);
            Arts = new ArtLoader(this);
            Maps = new MapLoader(this);
            Clilocs = new ClilocLoader(this);
            Gumps = new GumpsLoader(this);
            Fonts = new FontsLoader(this);
            Hues = new HuesLoader(this);
            TileData = new TileDataLoader(this);
            Multis = new MultiLoader(this);
            Skills = new SkillsLoader(this);
            Texmaps = new TexmapsLoader(this);
            Speeches = new SpeechesLoader(this);
            Lights = new LightsLoader(this);
            Sounds = new SoundsLoader(this);
            MultiMaps = new MultiMapLoader(this);
            Verdata = new VerdataLoader(this);
            Professions = new ProfessionLoader(this);

            _overrideMap = new UOFilesOverrideMap();
        }

        public ClientVersion Version { get; }
        public string BasePath { get; }
        public bool IsUOPInstallation { get; private set; }

        public AnimationsLoader Animations { get; }
        public AnimDataLoader AnimData { get; }
        public ArtLoader Arts { get; }
        public MapLoader Maps { get; set; }
        public ClilocLoader Clilocs { get; }
        public GumpsLoader Gumps { get; }
        public FontsLoader Fonts { get; }
        public HuesLoader Hues { get; }
        public TileDataLoader TileData { get; }
        public MultiLoader Multis { get; }
        public SkillsLoader Skills { get; }
        public TexmapsLoader Texmaps { get; }
        public SpeechesLoader Speeches { get; }
        public LightsLoader Lights { get; }
        public SoundsLoader Sounds { get; }
        public MultiMapLoader MultiMaps { get; }
        public VerdataLoader Verdata { get; }
        public ProfessionLoader Professions { get; }



        public void Dispose()
        {
            Animations.Dispose();
            AnimData.Dispose();
            Arts.Dispose();
            Maps.Dispose();
            Clilocs.Dispose();
            Gumps.Dispose();
            Fonts.Dispose();
            Hues.Dispose();
            TileData.Dispose();
            Multis.Dispose();
            Skills.Dispose();
            Texmaps.Dispose();
            Speeches.Dispose();
            Lights.Dispose();
            Sounds.Dispose();
            MultiMaps.Dispose();
            Verdata.Dispose();
            Professions.Dispose();
        }

        public string GetUOFilePath(string file)
        {
            if (!_overrideMap.TryGetValue(file.ToLowerInvariant(), out string uoFilePath))
            {
                uoFilePath = Path.Combine(BasePath, file);
            }

            //If the file with the given name doesn't exist, check for it with alternative casing if not on windows
            if (!PlatformHelper.IsWindows && !File.Exists(uoFilePath))
            {
                FileInfo finfo = new FileInfo(uoFilePath);
                var dir = Path.GetFullPath(finfo.DirectoryName ?? BasePath);

                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir);
                    var matches = 0;

                    foreach (var f in files)
                    {
                        if (string.Equals(f, uoFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            matches++;
                            uoFilePath = f;
                        }
                    }

                    if (matches > 1)
                    {
                        Log.Warn($"Multiple files with ambiguous case found for {file}, using {Path.GetFileName(uoFilePath)}. Check your data directory for duplicate files.");
                    }
                }
            }

            return uoFilePath;
        }

        public void Load(bool useVerdata, string lang, string mapsLayouts = "")
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            _overrideMap.Load(); // need to load this first so that it manages can perform the file overrides if needed

            IsUOPInstallation = Version >= ClientVersion.CV_7000 && File.Exists(GetUOFilePath("MainMisc.uop"));

            Maps.MapsLayouts = mapsLayouts;

            List<Task> tasks = new List<Task>
            {
                Animations.Load(),
                AnimData.Load(),
                Arts.Load(),
                Maps.Load(),
                Clilocs.Load(lang),
                Gumps.Load(),
                Fonts.Load(),
                Hues.Load(),
                TileData.Load(),
                Multis.Load(),
                Skills.Load().ContinueWith(t => Professions.Load()),
                Texmaps.Load(),
                Speeches.Load(),
                Lights.Load(),
                Sounds.Load(),
                MultiMaps.Load()
            };

            if (!Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10)))
            {
                Log.Panic("Loading files timeout.");
            }

            ReadArtDefFile();

            var verdata = Verdata.File;
            bool forceVerdata = Version < ClientVersion.CV_500A || verdata != null && verdata.Length != 0 && Verdata.Patches.Length != 0;

            if (!useVerdata && forceVerdata)
            {
                useVerdata = true;
            }

            Log.Trace($"Use verdata.mul: {(useVerdata ? "Yes" : "No")}");

            if (useVerdata)
            {
                if (verdata != null && Verdata.Patches.Length != 0)
                {
                    Log.Info(">> PATCHING WITH VERDATA.MUL");
                    var reader = verdata.GetReader();

                    for (int i = 0; i < Verdata.Patches.Length; i++)
                    {
                        ref UOFileIndex5D vh = ref Verdata.Patches[i];
                        Log.Info($">>> patching  FileID: {vh.FileID}  -  BlockID: {vh.BlockID}");

                        if (vh.FileID == 0)
                        {
                            Maps.PatchMapBlock(vh.BlockID, vh.Position);
                        }
                        else if (vh.FileID == 2)
                        {
                            Maps.PatchStaticBlock(vh.BlockID, ((ulong)reader.StartAddress.ToInt64() + vh.Position), vh.Length);
                        }
                        else if (vh.FileID == 4)
                        {
                            if (vh.BlockID < Arts.File.Entries.Length)
                            {
                                Arts.File.Entries[vh.BlockID] = new UOFileIndex
                                (
                                    reader.StartAddress,
                                    (uint) verdata.Length,
                                    vh.Position,
                                    (int) vh.Length,
                                    0
                                );
                            }
                        }
                        else if (vh.FileID == 12)
                        {
                            Gumps.File.Entries[vh.BlockID] = new UOFileIndex
                            (
                                reader.StartAddress,
                                (uint) verdata.Length,
                                vh.Position,
                                (int) vh.Length,
                                0,
                                0,
                                (short) (vh.GumpData >> 16),
                                (short) (vh.GumpData & 0xFFFF)
                            );
                        }
                        else if (vh.FileID == 14 && vh.BlockID < Multis.Count)
                        {
                            Multis.File.Entries[vh.BlockID] = new UOFileIndex
                            (
                                reader.StartAddress,
                                (uint) verdata.Length,
                                vh.Position,
                                (int) vh.Length,
                                0
                            );
                        }
                        else if (vh.FileID == 16 && vh.BlockID < Skills.SkillsCount)
                        {
                            var skill = Skills.Skills[(int) vh.BlockID];

                            if (skill != null)
                            {
                                unsafe
                                {
                                    skill.HasAction = reader.ReadUInt8() != 0;
                                    skill.Name = reader.ReadASCII((int)(vh.Length - 1));

                                    reader.Release();
                                }
                            }
                        }
                        else if (vh.FileID == 30)
                        {
                            reader.Seek(0);
                            reader.Skip((int) vh.Position);

                            if (vh.Length == 836)
                            {
                                int offset = (int) (vh.BlockID * 32);

                                if (offset + 32 > TileData.LandData.Length)
                                {
                                    continue;
                                }

                                reader.ReadUInt32LE();

                                for (int j = 0; j < 32; j++)
                                {
                                    ulong flags;

                                    if (Version < ClientVersion.CV_7090)
                                    {
                                        flags = reader.ReadUInt32LE();
                                    }
                                    else
                                    {
                                        flags = reader.ReadUInt64LE();
                                    }

                                    TileData.LandData[offset + j] = new LandTiles(flags, reader.ReadUInt16LE(), reader.ReadASCII(20));
                                }
                            }
                            else if (vh.Length == 1188)
                            {
                                int offset = (int) ((vh.BlockID - 0x0200) * 32);

                                if (offset + 32 > TileData.StaticData.Length)
                                {
                                    continue;
                                }

                                reader.ReadUInt32LE();

                                for (int j = 0; j < 32; j++)
                                {
                                    ulong flags;

                                    if (Version < ClientVersion.CV_7090)
                                    {
                                        flags = reader.ReadUInt32LE();
                                    }
                                    else
                                    {
                                        flags = reader.ReadUInt64LE();
                                    }

                                    TileData.StaticData[offset + j] = new StaticTiles
                                    (
                                        flags,
                                        reader.ReadUInt8(),
                                        reader.ReadUInt8(),
                                        reader.ReadInt32LE(),
                                        reader.ReadUInt16LE(),
                                        reader.ReadUInt16LE(),
                                        reader.ReadUInt16LE(),
                                        reader.ReadUInt8(),
                                        reader.ReadASCII(20)
                                    );
                                }
                            }
                        }
                        else if (vh.FileID == 32)
                        {
                            if (vh.BlockID < Hues.HuesCount)
                            {
                                VerdataHuesGroup group = Marshal.PtrToStructure<VerdataHuesGroup>(reader.StartAddress + (int) vh.Position);

                                HuesGroup[] hues = Hues.HuesRange;

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

        private void ReadArtDefFile()
        {
            string pathdef = GetUOFilePath("art.def");

            if (!File.Exists(pathdef))
            {
                return;
            }

            using (var reader = new DefReader(pathdef, 1))
            {
                while (reader.Next())
                {
                    int index = reader.ReadInt();

                    if (index < 0 || index >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT + TileData.StaticData.Length)
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

                        if (checkIndex < 0 || checkIndex >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT + TileData.StaticData.Length)
                        {
                            continue;
                        }

                        if (index < Arts.File.Entries.Length && checkIndex < Arts.File.Entries.Length)
                        {
                            ref UOFileIndex currentEntry = ref Arts.File.GetValidRefEntry(index);
                            ref UOFileIndex checkEntry = ref Arts.File.GetValidRefEntry(checkIndex);

                            if (currentEntry.Equals(UOFileIndex.Invalid) && !checkEntry.Equals(UOFileIndex.Invalid))
                            {
                                Arts.File.Entries[index] = Arts.File.Entries[checkIndex];
                            }
                        }

                        if (index < ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            checkIndex < ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            checkIndex < TileData.LandData.Length &&
                            index < TileData.LandData.Length &&
                            !TileData.LandData[checkIndex].Equals(default) &&
                            TileData.LandData[index].Equals(default))
                        {
                            TileData.LandData[index] = TileData.LandData[checkIndex];

                            break;
                        }

                        if (index >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT && checkIndex >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            index < TileData.StaticData.Length && checkIndex < TileData.StaticData.Length &&
                            TileData.StaticData[index].Equals(default) && !TileData.StaticData[checkIndex].Equals(default))
                        {
                            TileData.StaticData[index] = TileData.StaticData[checkIndex];

                            break;
                        }
                    }
                }
            }
        }
    }
}
