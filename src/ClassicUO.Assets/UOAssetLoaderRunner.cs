// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Concrete <see cref="IUOAssetLoaderRunner"/>. Sequentially loads every
    /// loader hanging off the facade (animations, art, maps, gumps, hues,
    /// fonts, tiledata, multis, skills, professions, texmaps, speeches,
    /// lights, sounds, multimaps, tileart, string dictionary, verdata), then
    /// dispatches the per-block verdata patches into the appropriate target
    /// table. Also reads <c>art.def</c> via <see cref="IArtDefReader"/>.
    /// </summary>
    internal sealed class UOAssetLoaderRunner : IUOAssetLoaderRunner
    {
        private readonly IArtDefReader _artDef;

        public UOAssetLoaderRunner(IArtDefReader artDef)
        {
            _artDef = artDef;
        }

        public void Run(UOFileManager manager, bool useVerdata, string lang, string mapsLayouts)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            manager.Maps.MapsLayouts = mapsLayouts;

            manager.Animations.Load();
            manager.AnimData.Load();
            manager.Arts.Load();
            manager.Maps.Load();
            manager.Clilocs.Load(lang);
            manager.Gumps.Load();
            manager.Fonts.Load();
            manager.Hues.Load();
            manager.TileData.Load();
            manager.Multis.Load();
            manager.Skills.Load();
            manager.Professions.Load();
            manager.Texmaps.Load();
            manager.Speeches.Load();
            manager.Lights.Load();
            manager.Sounds.Load();
            manager.MultiMaps.Load();
            manager.TileArt.Load();
            manager.StringDictionary.Load();
            manager.Verdata.Load();

            _artDef.Read(manager.PathResolver, manager.Arts, manager.TileData);

            ApplyVerdataPatches(manager, useVerdata);

            Log.Trace($"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();
        }

        private static void ApplyVerdataPatches(UOFileManager manager, bool useVerdata)
        {
            var verdata = manager.Verdata.File;
            bool forceVerdata = manager.Version < ClientVersion.CV_500A
                || verdata != null && verdata.Length != 0 && manager.Verdata.Patches.Length != 0;

            if (!useVerdata && forceVerdata)
            {
                useVerdata = true;
            }

            Log.Trace($"Use verdata.mul: {(useVerdata ? "Yes" : "No")}");

            if (!useVerdata)
            {
                return;
            }

            if (verdata == null || manager.Verdata.Patches.Length == 0)
            {
                return;
            }

            Log.Info(">> PATCHING WITH VERDATA.MUL");

            var buf = new byte[256];
            Span<VerdataHuesGroup> group = stackalloc VerdataHuesGroup[1];

            for (int i = 0; i < manager.Verdata.Patches.Length; i++)
            {
                ref var vh = ref manager.Verdata.Patches[i];
                Log.Info($">>> patching  FileID: {vh.FileID}  -  BlockID: {vh.BlockID}");

                if (vh.FileID == 0)
                {
                    manager.Maps.PatchMapBlock(verdata, vh.BlockID, vh.Position);
                }
                else if (vh.FileID == 2)
                {
                    manager.Maps.PatchStaticBlock(verdata, vh.BlockID, vh.Position, vh.Length);
                }
                else if (vh.FileID == 4)
                {
                    if (vh.BlockID < manager.Arts.File.Entries.Length)
                    {
                        manager.Arts.File.Entries[vh.BlockID] = new UOFileIndex
                        (
                            verdata,
                            vh.Position,
                            (int)vh.Length,
                            0
                        );
                    }
                }
                else if (vh.FileID == 12)
                {
                    manager.Gumps.File.Entries[vh.BlockID] = new UOFileIndex
                    (
                        verdata,
                        vh.Position,
                        (int)vh.Length,
                        0,
                        0,
                        (short)(vh.GumpData >> 16),
                        (short)(vh.GumpData & 0xFFFF)
                    );
                }
                else if (vh.FileID == 14 && vh.BlockID < manager.Multis.File.Entries.Length)
                {
                    manager.Multis.File.Entries[vh.BlockID] = new UOFileIndex
                    (
                        verdata,
                        vh.Position,
                        (int)vh.Length,
                        0
                    );
                }
                else if (vh.FileID == 16 && vh.BlockID < manager.Skills.SkillsCount)
                {
                    var skill = manager.Skills.Skills[(int)vh.BlockID];

                    if (skill != null)
                    {
                        skill.HasAction = verdata.ReadUInt8() != 0;
                        if (buf.Length < vh.Length)
                            buf = new byte[vh.Length];

                        skill.Name = Encoding.ASCII.GetString(buf.AsSpan(0, (int)(vh.Length - 1)));
                    }
                }
                else if (vh.FileID == 30)
                {
                    verdata.Seek(vh.Position, SeekOrigin.Begin);

                    if (vh.Length == 836)
                    {
                        int offset = (int)(vh.BlockID * 32);

                        if (offset + 32 > manager.TileData.LandData.Length)
                        {
                            continue;
                        }

                        verdata.ReadUInt32();

                        for (int j = 0; j < 32; j++)
                        {
                            ulong flags;

                            if (manager.Version < ClientVersion.CV_7090)
                            {
                                flags = verdata.ReadUInt32();
                            }
                            else
                            {
                                flags = verdata.ReadUInt64();
                            }

                            var textId = verdata.ReadUInt16();
                            var str = Encoding.ASCII.GetString(buf.AsSpan(0, 20));
                            manager.TileData.LandData[offset + j] = new LandTiles(flags, textId, str);
                        }
                    }
                    else if (vh.Length == 1188)
                    {
                        int offset = (int)((vh.BlockID - 0x0200) * 32);

                        if (offset + 32 > manager.TileData.StaticData.Length)
                        {
                            continue;
                        }

                        verdata.ReadUInt32();

                        for (int j = 0; j < 32; j++)
                        {
                            ulong flags;

                            if (manager.Version < ClientVersion.CV_7090)
                            {
                                flags = verdata.ReadUInt32();
                            }
                            else
                            {
                                flags = verdata.ReadUInt64();
                            }

                            var weight = verdata.ReadUInt8();
                            var layer = verdata.ReadUInt8();
                            var count = verdata.ReadInt32();
                            var animId = verdata.ReadUInt16();
                            var hue = verdata.ReadUInt16();
                            var lightIdx = verdata.ReadUInt16();
                            var height = verdata.ReadUInt8();
                            var str = Encoding.ASCII.GetString(buf.AsSpan(0, 20));

                            manager.TileData.StaticData[offset + j] = new StaticTiles
                            (
                                flags,
                                weight,
                                layer,
                                count,
                                animId,
                                hue,
                                lightIdx,
                                height,
                                str
                            );
                        }
                    }
                }
                else if (vh.FileID == 32)
                {
                    if (vh.BlockID < manager.Hues.HuesCount)
                    {
                        verdata.Seek(vh.Position, SeekOrigin.Begin);
                        verdata.Read(MemoryMarshal.AsBytes(group));

                        HuesGroup[] hues = manager.Hues.HuesRange;
                        hues[vh.BlockID].Header = group[0].Header;

                        for (int j = 0; j < 8; j++)
                        {
                            hues[vh.BlockID].Entries[j].ColorTable = group[0].Entries[j].ColorTable;
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
}
