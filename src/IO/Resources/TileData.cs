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
using System.Runtime.InteropServices;
using System.Text;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    public static class TileData
    {
        private const int MAX_LAND_DATA_INDEX_COUNT = 0x4000;
        private const int MAX_STATIC_DATA_INDEX_COUNT = 0x10000;

        public static LandTiles[] LandData { get; private set; }

        public static StaticTiles[] StaticData { get; private set; }

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "tiledata.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul tiledata = new UOFileMul(path, false);
            bool isold = FileManager.ClientVersion < ClientVersions.CV_7090;
            int staticscount = !isold ? (int) (tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupNew>()) / UnsafeMemoryManager.SizeOf<StaticGroupNew>() : (int) (tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupOld>()) / UnsafeMemoryManager.SizeOf<StaticGroupOld>();

            if (staticscount > 2048)
                staticscount = 2048;
            tiledata.Seek(0);
            LandData = new LandTiles[MAX_LAND_DATA_INDEX_COUNT];
            StaticData = new StaticTiles[staticscount * 32];
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
                    tiledata.Fill(bufferString, 20);
                    string name = string.Intern(Encoding.UTF8.GetString(bufferString).TrimEnd('\0'));
                    LandData[idx] = new LandTiles(flags, textId, name);
                }
            }

            END:

            for (int i = 0; i < staticscount; i++)
            {
                if (tiledata.Position >= tiledata.Length)
                    goto END_2;
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
                    tiledata.Fill(bufferString, 20);
                    string name = string.Intern(Encoding.UTF8.GetString(bufferString).TrimEnd('\0'));

                    StaticData[idx] = new StaticTiles(flags, weight, layer, count, animId, hue, lightIndex, height, name);
                }
            }

            END_2:
            tiledata.Dispose();

            //string pathdef = Path.Combine(FileManager.UoFolderPath, "art.def");
            //if (!File.Exists(pathdef))
            //    return;

            //using (StreamReader reader = new StreamReader(File.OpenRead(pathdef)))
            //{
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        line = line.Trim();
            //        if (line.Length <= 0 || line[0] == '#')
            //            continue;
            //        string[] defs = line.Split(new[] { '\t', ' ', '#' }, StringSplitOptions.RemoveEmptyEntries);
            //        if (defs.Length < 2)
            //            continue;

            //        int index = int.Parse(defs[0]);

            //        if (index < 0 || index >= MAX_LAND_DATA_INDEX_COUNT + StaticData.Length)
            //            continue;

            //        int first = defs[1].IndexOf("{");
            //        int last = defs[1].IndexOf("}");

            //        string[] newdef = defs[1].Substring(first + 1, last - 1).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            //        foreach (string s in newdef)
            //        {
            //            int checkindex = int.Parse(s);

            //            if (checkindex < 0 || checkindex >= MAX_LAND_DATA_INDEX_COUNT + StaticData.Length)
            //                continue;

            //            //_file.Entries[index] = _file.Entries[checkindex];

            //            if (index < MAX_LAND_DATA_INDEX_COUNT && checkindex < MAX_LAND_DATA_INDEX_COUNT && LandData.Length > checkindex && !LandData[checkindex].Equals(default) && (LandData.Length <= index  || LandData[index].Equals(default)))
            //            {
            //                LandData[index] = LandData[checkindex];
            //            }
            //            else if (index >= MAX_LAND_DATA_INDEX_COUNT && checkindex >= MAX_LAND_DATA_INDEX_COUNT)
            //            {
            //                checkindex -= MAX_LAND_DATA_INDEX_COUNT;
            //                checkindex &= 0x3FFF;
            //                index -= MAX_LAND_DATA_INDEX_COUNT;

            //                if ( (StaticData.Length <= index || StaticData[index].Equals(default)) &&
            //                    StaticData.Length > checkindex && !StaticData[checkindex].Equals(default))
            //                {

            //                    StaticData[index] = StaticData[checkindex];

            //                    break;
            //                }

            //            }
            //        }
            //    }
            //}
        }

        public static bool IsBackground(ulong flags)
        {
            return (flags & 0x00000001) != 0;
        }

        public static bool IsWeapon(ulong flags)
        {
            return (flags & 0x00000002) != 0;
        }

        public static bool IsTransparent(ulong flags)
        {
            return (flags & 0x00000004) != 0;
        }

        public static bool IsTranslucent(ulong flags)
        {
            return (flags & 0x00000008) != 0;
        }

        public static bool IsWall(ulong flags)
        {
            return (flags & 0x00000010) != 0;
        }

        public static bool IsDamaging(ulong flags)
        {
            return (flags & 0x00000020) != 0;
        }

        public static bool IsImpassable(ulong flags)
        {
            return (flags & 0x00000040) != 0;
        }

        public static bool IsWet(ulong flags)
        {
            return (flags & 0x00000080) != 0;
        }

        public static bool IsUnknown(ulong flags)
        {
            return (flags & 0x00000100) != 0;
        }

        public static bool IsSurface(ulong flags)
        {
            return (flags & 0x00000200) != 0;
        }

        public static bool IsBridge(ulong flags)
        {
            return (flags & 0x00000400) != 0;
        }

        public static bool IsStackable(ulong flags)
        {
            return (flags & 0x00000800) != 0;
        }

        public static bool IsWindow(ulong flags)
        {
            return (flags & 0x00001000) != 0;
        }

        public static bool IsNoShoot(ulong flags)
        {
            return (flags & 0x00002000) != 0;
        }

        public static bool IsPrefixA(ulong flags)
        {
            return (flags & 0x00004000) != 0;
        }

        public static bool IsPrefixAn(ulong flags)
        {
            return (flags & 0x00008000) != 0;
        }

        public static bool IsInternal(ulong flags)
        {
            return (flags & 0x00010000) != 0;
        }

        public static bool IsFoliage(ulong flags)
        {
            return (flags & 0x00020000) != 0;
        }

        public static bool IsPartialHue(ulong flags)
        {
            return (flags & 0x00040000) != 0;
        }

        public static bool IsUnknown1(ulong flags)
        {
            return (flags & 0x00080000) != 0;
        }

        public static bool IsMap(ulong flags)
        {
            return (flags & 0x00100000) != 0;
        }

        public static bool IsContainer(ulong flags)
        {
            return (flags & 0x00200000) != 0;
        }

        public static bool IsWearable(ulong flags)
        {
            return (flags & 0x00400000) != 0;
        }

        public static bool IsLightSource(ulong flags)
        {
            return (flags & 0x00800000) != 0;
        }

        public static bool IsAnimated(ulong flags)
        {
            return (flags & 0x01000000) != 0;
        }

        public static bool IsNoDiagonal(ulong flags)
        {
            return (flags & 0x02000000) != 0;
        }

        public static bool IsUnknown2(ulong flags)
        {
            return (flags & 0x04000000) != 0;
        }

        public static bool IsArmor(ulong flags)
        {
            return (flags & 0x08000000) != 0;
        }

        public static bool IsRoof(ulong flags)
        {
            return (flags & 0x10000000) != 0;
        }

        public static bool IsDoor(ulong flags)
        {
            return (flags & 0x20000000) != 0;
        }

        public static bool IsStairBack(ulong flags)
        {
            return (flags & 0x40000000) != 0;
        }

        public static bool IsStairRight(ulong flags)
        {
            return (flags & 0x80000000) != 0;
        }
    }

    public readonly struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = flags;
            TexID = textId;
            Name = name;
        }

        public readonly ulong Flags;
        public readonly ushort TexID;
        public readonly string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct LandGroup
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTiles[] Tiles;
    }

    public readonly struct StaticTiles
    {
        public static readonly StaticTiles Empty = default;

        public StaticTiles(ulong flags, byte weight, byte layer, int count, ushort animId, ushort hue, ushort lightIndex, byte height, string name)
        {
            Flags = flags;
            Weight = weight;
            Layer = layer;
            Count = count;
            AnimID = animId;
            Hue = hue;
            LightIndex = lightIndex;
            Height = height;
            Name = name;
        }

        public readonly ulong Flags;
        public readonly byte Weight;
        public readonly byte Layer;
        public readonly int Count;
        public readonly ushort AnimID;
        public readonly ushort Hue;
        public readonly ushort LightIndex;
        public readonly byte Height;
        public readonly string Name;
    }

    // old

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct LandGroupOld
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct LandTilesOld
    {
        public readonly uint Flags;
        public readonly ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticGroupOld
    {
        public readonly uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly StaticTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticTilesOld
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
    public readonly struct LandGroupNew
    {
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly LandTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct LandTilesNew
    {
        public readonly TileFlag Flags;
        public readonly ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public readonly string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticGroupNew
    {
        public readonly uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly StaticTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticTilesNew
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
    public enum TileFlag : ulong
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
        HoverOver = 0x02000000,
        /// <summary>
        ///     Unknown.
        /// </summary>
        NoDiagonal = 0x04000000,
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