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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class TileDataLoader : UOFileLoader
    {
        private TileDataLoader()
        {

        }

        private static TileDataLoader _instance;
        public static TileDataLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TileDataLoader();
                }

                return _instance;
            }
        }

        private static StaticTiles[] _staticData;
        private static LandTiles[] _landData;

        public ref readonly LandTiles[] LandData => ref _landData;
        public ref readonly StaticTiles[] StaticData => ref _staticData;

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = UOFileManager.GetUOFilePath("tiledata.mul");

                FileSystemHelper.EnsureFileExists(path);

                UOFileMul tiledata = new UOFileMul(path);
                bool isold = Client.Version < ClientVersion.CV_7090;
                int staticscount = !isold ? (int) (tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupNew>()) / UnsafeMemoryManager.SizeOf<StaticGroupNew>() : (int) (tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupOld>()) / UnsafeMemoryManager.SizeOf<StaticGroupOld>();

                if (staticscount > 2048)
                    staticscount = 2048;
                tiledata.Seek(0);
                _landData = new LandTiles[Constants.MAX_LAND_DATA_INDEX_COUNT];
                _staticData = new StaticTiles[staticscount * 32];
                byte[] bufferString = new byte[20];

                for (int i = 0; i < 512; i++)
                {
                    tiledata.Skip(4);

                    for (int j = 0; j < 32; j++)
                    {
                        if (tiledata.Position + (isold ? 4 : 8) + 2 + 20 > tiledata.Length)
                            goto END;

                        int idx = i * 32 + j;
                        ulong flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                        ushort textId = tiledata.ReadUShort();
                        tiledata.Fill(ref bufferString, 20);
                        string name = string.Intern(Encoding.UTF8.GetString(bufferString).TrimEnd('\0'));
                        LandData[idx] = new LandTiles(flags, textId, name);
                    }
                }

                END:

                for (int i = 0; i < staticscount; i++)
                {
                    if (tiledata.Position >= tiledata.Length)
                        break;

                    tiledata.Skip(4);

                    for (int j = 0; j < 32; j++)
                    {
                        if (tiledata.Position + (isold ? 4 : 8) + 13 + 20 > tiledata.Length)
                            goto END_2;

                        int idx = i * 32 + j;

                        ulong flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                        byte weight = tiledata.ReadByte();
                        byte layer = tiledata.ReadByte();
                        int count = tiledata.ReadInt();
                        ushort animId = tiledata.ReadUShort();
                        ushort hue = tiledata.ReadUShort();
                        ushort lightIndex = tiledata.ReadUShort();
                        byte height = tiledata.ReadByte();
                        tiledata.Fill(ref bufferString, 20);
                        string name = string.Intern(Encoding.UTF8.GetString(bufferString).TrimEnd('\0'));

                        StaticData[idx] = new StaticTiles(flags, weight, layer, count, animId, hue, lightIndex, height, name);
                    }
                }


                //path = Path.Combine(FileManager.UoFolderPath, "tileart.uop");

                //if (File.Exists(path))
                //{
                //    UOFileUop uop = new UOFileUop(path, ".bin");
                //    DataReader reader = new DataReader();
                //    for (int i = 0; i < uop.Entries.Length; i++)
                //    {
                //        long offset = uop.Entries[i].Offset;
                //        int csize = uop.Entries[i].Length;
                //        int dsize = uop.Entries[i].DecompressedLength;

                //        if (offset == 0)
                //            continue;

                //        uop.Seek(offset);
                //        byte[] cdata = uop.ReadArray<byte>(csize);
                //        byte[] ddata = new byte[dsize];

                //        ZLib.Decompress(cdata, 0, ddata, dsize);

                //        reader.SetData(ddata, dsize);

                //        ushort version = reader.ReadUShort();
                //        uint stringDicOffset = reader.ReadUInt();
                //        uint tileID = reader.ReadUInt();

                //        reader.Skip(1 + // bool unk
                //                    1 + // unk
                //                    4 + // float unk
                //                    4 + // float unk
                //                    4 + // fixed zero ?
                //                    4 + // old id ?
                //                    4 + // unk
                //                    4 + // unk
                //                    1 + // unk
                //                    4 + // 3F800000
                //                    4 + // unk
                //                    4 + // float light
                //                    4 + // float light
                //                    4   // unk
                //                    );

                //        ulong flags = reader.ReadULong();
                //        ulong flags2 = reader.ReadULong();

                //        reader.Skip(4); // unk

                //        reader.Skip(24); // EC IMAGE OFFSET
                //        byte[] imageOffset = reader.ReadArray(24); // 2D IMAGE OFFSET


                //        if (tileID + 0x4000 == 0xa28d)
                //        {
                //            TileFlag f = (TileFlag) flags;

                //        }

                //        int count = reader.ReadByte();
                //        for (int j = 0; j < count; j++)
                //        {
                //            byte prop = reader.ReadByte();
                //            uint value = reader.ReadUInt();
                //        }

                //        count = reader.ReadByte();
                //        for (int j = 0; j < count; j++)
                //        {
                //            byte prop = reader.ReadByte();
                //            uint value = reader.ReadUInt();
                //        }

                //        count = reader.ReadInt(); // Gold Silver
                //        for (int j = 0; j < count; j++)
                //        {
                //            uint amount = reader.ReadUInt();
                //            uint id = reader.ReadUInt();
                //        }

                //        count = reader.ReadInt();

                //        for (int j = 0; j < count; j++)
                //        {
                //            byte val = reader.ReadByte();

                //            if (val != 0)
                //            {
                //                if (val == 1)
                //                {
                //                    byte unk = reader.ReadByte();
                //                    uint unk1 = reader.ReadUInt();
                //                }

                //            }
                //            else
                //            {
                //                int subCount = reader.ReadInt();

                //                for (int k = 0; k < subCount; k++)
                //                {
                //                    uint unk = reader.ReadUInt();
                //                    uint unk1 = reader.ReadUInt();
                //                }
                //            }
                //        }

                //        count = reader.ReadByte();

                //        if (count != 0)
                //        {
                //            uint unk = reader.ReadUInt();
                //            uint unk1 = reader.ReadUInt();
                //            uint unk2 = reader.ReadUInt();
                //            uint unk3 = reader.ReadUInt();
                //        }


                //        if (StaticData[tileID].AnimID == 0)
                //        {
                //            //StaticData[tileID] = new StaticTiles(flags, 0, 0, 0, );
                //        }


                //    }

                //    uop.Dispose();
                //    reader.ReleaseData();
                //}

                string pathdef = UOFileManager.GetUOFilePath("art.def");

                if (File.Exists(pathdef))
                {
                    using (DefReader reader = new DefReader(pathdef, 1))
                    {
                        while (reader.Next())
                        {
                            int index = reader.ReadInt();

                            if (index < 0 || index >= Constants.MAX_LAND_DATA_INDEX_COUNT + StaticData.Length)
                                continue;

                            int[] group = reader.ReadGroup();

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkIndex = group[i];

                                if (checkIndex < 0 || checkIndex >= Constants.MAX_LAND_DATA_INDEX_COUNT + StaticData.Length)
                                    continue;

                                if (index < Constants.MAX_LAND_DATA_INDEX_COUNT && checkIndex < Constants.MAX_LAND_DATA_INDEX_COUNT && checkIndex < LandData.Length && index < LandData.Length && !LandData[checkIndex].Equals(default) && LandData[index].Equals(default))
                                {
                                    LandData[index] = LandData[checkIndex];

                                    break;
                                }

                                if (index >= Constants.MAX_LAND_DATA_INDEX_COUNT && checkIndex >= Constants.MAX_LAND_DATA_INDEX_COUNT)
                                {
                                    checkIndex -= Constants.MAX_LAND_DATA_INDEX_COUNT;
                                    checkIndex &= 0x3FFF;
                                    index -= Constants.MAX_LAND_DATA_INDEX_COUNT;

                                    if (StaticData[index].Equals(default) && !StaticData[checkIndex].Equals(default))
                                    {
                                        StaticData[index] = StaticData[checkIndex];

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                END_2:
                tiledata.Dispose();
            });
        }

        public override void CleanResources()
        {
            // nothing
        }
    }

    internal readonly struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = (TileFlag) flags;
            TexID = textId;
            Name = name;

            IsWet = (Flags & TileFlag.Wet) != 0;
            IsImpassable = (Flags & TileFlag.Impassable) != 0;
            IsNoDiagonal = (Flags & TileFlag.NoDiagonal) != 0;
        }

        public readonly TileFlag Flags;
        public readonly ushort TexID;
        public readonly string Name;

        public readonly bool IsWet;
        public readonly bool IsImpassable;
        public readonly bool IsNoDiagonal;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct LandGroup
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTiles[] Tiles;
    }

    internal readonly struct StaticTiles
    {
        public StaticTiles(ulong flags, byte weight, byte layer, int count, ushort animId, ushort hue, ushort lightIndex, byte height, string name)
        {
            Flags = (TileFlag) flags;
            Weight = weight;
            Layer = layer;
            Count = count;
            AnimID = animId;
            Hue = hue;
            LightIndex = lightIndex;
            Height = height;
            Name = name;

            IsAnimated = (Flags & TileFlag.Animation) != 0;
            IsBridge = (Flags & TileFlag.Bridge) != 0;
            IsImpassable = (Flags & TileFlag.Impassable) != 0;
            IsSurface = (Flags & TileFlag.Surface) != 0;
            IsWearable = (Flags & TileFlag.Wearable) != 0;
            IsInternal = (Flags & TileFlag.Internal) != 0;
            IsBackground = (Flags & TileFlag.Background) != 0;
            IsNoDiagonal = (Flags & TileFlag.NoDiagonal) != 0;
            IsWet = (Flags & TileFlag.Wet) != 0;
            IsFoliage = (Flags & TileFlag.Foliage) != 0;
            IsRoof = (Flags & TileFlag.Roof) != 0;
            IsTranslucent = (Flags & TileFlag.Translucent) != 0;
            IsPartialHue = (Flags & TileFlag.PartialHue) != 0;
            IsStackable = (Flags & TileFlag.Generic) != 0;
            IsTransparent = (Flags & TileFlag.Transparent) != 0;
            IsContainer = (Flags & TileFlag.Container) != 0;
            IsDoor = (Flags & TileFlag.Door) != 0;
            IsWall = (Flags & TileFlag.Wall) != 0;
            IsLight = (Flags & TileFlag.LightSource) != 0;
            IsNoShoot = (Flags & TileFlag.NoShoot) != 0;
        }

        public readonly TileFlag Flags;
        public readonly byte Weight;
        public readonly byte Layer;
        public readonly int Count;
        public readonly ushort AnimID;
        public readonly ushort Hue;
        public readonly ushort LightIndex;
        public readonly byte Height;
        public readonly string Name;

        public readonly bool IsAnimated;
        public readonly bool IsBridge;
        public readonly bool IsImpassable;
        public readonly bool IsSurface;
        public readonly bool IsWearable;
        public readonly bool IsInternal;
        public readonly bool IsBackground;
        public readonly bool IsNoDiagonal;
        public readonly bool IsWet;
        public readonly bool IsFoliage;
        public readonly bool IsRoof;
        public readonly bool IsTranslucent;
        public readonly bool IsPartialHue;
        public readonly bool IsStackable;
        public readonly bool IsTransparent;
        public readonly bool IsContainer;
        public readonly bool IsDoor;
        public readonly bool IsWall;
        public readonly bool IsLight;
        public readonly bool IsNoShoot;
    }

    // old

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct LandGroupOld
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct LandTilesOld
    {
        public readonly uint Flags;
        public readonly ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaticGroupOld
    {
        public readonly uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly StaticTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaticTilesOld
    {
        public readonly uint Flags;
        public readonly byte Weight;
        public readonly byte Layer;
        public readonly int Count;
        public readonly ushort AnimID;
        public readonly ushort Hue;
        public readonly ushort LightIndex;
        public readonly byte Height;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    // new 

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct LandGroupNew
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct LandTilesNew
    {
        public readonly TileFlag Flags;
        public readonly ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaticGroupNew
    {
        public readonly uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly StaticTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaticTilesNew
    {
        public readonly TileFlag Flags;
        public readonly byte Weight;
        public readonly byte Layer;
        public readonly int Count;
        public readonly ushort AnimID;
        public readonly ushort Hue;
        public readonly ushort LightIndex;
        public readonly byte Height;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    [Flags]
    enum TileFlag : ulong
    {
        /// <summary>
        ///     Nothing is flagged.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Background = 0x00000001,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Weapon = 0x00000002,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Transparent = 0x00000004,
        /// <summary>
        ///     The tile is rendered with partial alpha-transparency.
        /// </summary>
        Translucent = 0x00000008,
        /// <summary>
        ///     The tile is a wall.
        /// </summary>
        Wall = 0x00000010,
        /// <summary>
        ///     The tile can cause damage when moved over.
        /// </summary>
        Damaging = 0x00000020,
        /// <summary>
        ///     The tile may not be moved over or through.
        /// </summary>
        Impassable = 0x00000040,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Wet = 0x00000080,
        /// <summary>
        ///     Unknown.
        /// </summary>
        Unknown1 = 0x00000100,
        /// <summary>
        ///     The tile is a surface. It may be moved over, but not through.
        /// </summary>
        Surface = 0x00000200,
        /// <summary>
        ///     The tile is a stair, ramp, or ladder.
        /// </summary>
        Bridge = 0x00000400,
        /// <summary>
        ///     The tile is stackable
        /// </summary>
        Generic = 0x00000800,
        /// <summary>
        ///     The tile is a window. Like <see cref="TileFlag.NoShoot" />, tiles with this flag block line of sight.
        /// </summary>
        Window = 0x00001000,
        /// <summary>
        ///     The tile blocks line of sight.
        /// </summary>
        NoShoot = 0x00002000,
        /// <summary>
        ///     For single-amount tiles, the string "a " should be prepended to the tile name.
        /// </summary>
        ArticleA = 0x00004000,
        /// <summary>
        ///     For single-amount tiles, the string "an " should be prepended to the tile name.
        /// </summary>
        ArticleAn = 0x00008000,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Internal = 0x00010000,
        /// <summary>
        ///     The tile becomes translucent when walked behind. Boat masts also have this flag.
        /// </summary>
        Foliage = 0x00020000,
        /// <summary>
        ///     Only gray pixels will be hued
        /// </summary>
        PartialHue = 0x00040000,
        /// <summary>
        ///     Unknown.
        /// </summary>
        NoHouse = 0x00080000,
        /// <summary>
        ///     The tile is a map--in the cartography sense. Unknown usage.
        /// </summary>
        Map = 0x00100000,
        /// <summary>
        ///     The tile is a container.
        /// </summary>
        Container = 0x00200000,
        /// <summary>
        ///     The tile may be equiped.
        /// </summary>
        Wearable = 0x00400000,
        /// <summary>
        ///     The tile gives off light.
        /// </summary>
        LightSource = 0x00800000,
        /// <summary>
        ///     The tile is animated.
        /// </summary>
        Animation = 0x01000000,
        /// <summary>
        ///     Gargoyles can fly over
        /// </summary>
        NoDiagonal = 0x02000000,
        /// <summary>
        ///     Unknown.
        /// </summary>
        Unknown2 = 0x04000000,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        Armor = 0x08000000,
        /// <summary>
        ///     The tile is a slanted roof.
        /// </summary>
        Roof = 0x10000000,
        /// <summary>
        ///     The tile is a door. Tiles with this flag can be moved through by ghosts and GMs.
        /// </summary>
        Door = 0x20000000,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        StairBack = 0x40000000,
        /// <summary>
        ///     Not yet documented.
        /// </summary>
        StairRight = 0x80000000,
        /// Blend Alphas, tile blending.
        AlphaBlend = 0x0100000000,
        /// Uses new art style?
        UseNewArt = 0x0200000000,
        /// Has art being used?
        ArtUsed = 0x0400000000,
        /// Disallow shadow on this tile, lightsource? lava?
        NoShadow = 0x1000000000,
        /// Let pixels bleed in to other tiles? Is this Disabling Texture Clamp?
        PixelBleed = 0x2000000000,
        /// Play tile animation once.
        PlayAnimOnce = 0x4000000000,
        /// Movable multi? Cool ships and vehicles etc?
        MultiMovable = 0x10000000000
    }
}