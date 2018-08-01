using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public static class TileData
    {
        public static LandTiles[] LandData { get; private set; }
        public static StaticTiles[] StaticData { get; private set; }

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "tiledata.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            UOFileMul tiledata = new UOFileMul(path);


            bool isold = FileManager.ClientVersion < ClientVersions.CV_7090;

            int staticscount = !isold ? (int) (tiledata.Length - 512 * Marshal.SizeOf<LandGroupNew>()) / Marshal.SizeOf<StaticGroupNew>() : (int) (tiledata.Length - 512 * Marshal.SizeOf<LandGroupOld>()) / Marshal.SizeOf<StaticGroupOld>();

            if (staticscount > 2048)
                staticscount = 2048;

            tiledata.Seek(0);

            LandData = new LandTiles[512 * 32];
            StaticData = new StaticTiles[staticscount * 32];

            byte[] bufferString = new byte[20];

            for (int i = 0; i < 512; i++)
            {
                tiledata.Skip(4);
                for (int j = 0; j < 32; j++)
                {
                    int idx = i * 32 + j;
                    LandData[idx].Flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                    LandData[idx].TexID = tiledata.ReadUShort();

                    tiledata.Fill(bufferString, 20);
                    LandData[idx].Name = Encoding.UTF8.GetString(bufferString).TrimEnd('\0');
                }
            }

            for (int i = 0; i < staticscount; i++)
            {
                tiledata.Skip(4);
                for (int j = 0; j < 32; j++)
                {
                    int idx = i * 32 + j;
                    StaticData[idx].Flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                    StaticData[idx].Weight = tiledata.ReadByte();
                    StaticData[idx].Layer = tiledata.ReadByte();
                    StaticData[idx].Count = tiledata.ReadInt();
                    StaticData[idx].AnimID = tiledata.ReadUShort();
                    StaticData[idx].Hue = tiledata.ReadUShort();
                    StaticData[idx].LightIndex = tiledata.ReadUShort();
                    StaticData[idx].Height = tiledata.ReadByte();

                    tiledata.Fill(bufferString, 20);
                    StaticData[idx].Name = Encoding.UTF8.GetString(bufferString).TrimEnd('\0');
                }
            }
        }


        public static bool IsBackground(in long flags)
        {
            return (flags & 0x00000001) != 0;
        }

        public static bool IsWeapon(in long flags)
        {
            return (flags & 0x00000002) != 0;
        }

        public static bool IsTransparent(in long flags)
        {
            return (flags & 0x00000004) != 0;
        }

        public static bool IsTranslucent(in long flags)
        {
            return (flags & 0x00000008) != 0;
        }

        public static bool IsWall(in long flags)
        {
            return (flags & 0x00000010) != 0;
        }

        public static bool IsDamaging(in long flags)
        {
            return (flags & 0x00000020) != 0;
        }

        public static bool IsImpassable(in long flags)
        {
            return (flags & 0x00000040) != 0;
        }

        public static bool IsWet(in long flags)
        {
            return (flags & 0x00000080) != 0;
        }

        public static bool IsUnknown(in long flags)
        {
            return (flags & 0x00000100) != 0;
        }

        public static bool IsSurface(in long flags)
        {
            return (flags & 0x00000200) != 0;
        }

        public static bool IsBridge(in long flags)
        {
            return (flags & 0x00000400) != 0;
        }

        public static bool IsStackable(in long flags)
        {
            return (flags & 0x00000800) != 0;
        }

        public static bool IsWindow(in long flags)
        {
            return (flags & 0x00001000) != 0;
        }

        public static bool IsNoShoot(in long flags)
        {
            return (flags & 0x00002000) != 0;
        }

        public static bool IsPrefixA(in long flags)
        {
            return (flags & 0x00004000) != 0;
        }

        public static bool IsPrefixAn(in long flags)
        {
            return (flags & 0x00008000) != 0;
        }

        public static bool IsInternal(in long flags)
        {
            return (flags & 0x00010000) != 0;
        }

        public static bool IsFoliage(in long flags)
        {
            return (flags & 0x00020000) != 0;
        }

        public static bool IsPartialHue(in long flags)
        {
            return (flags & 0x00040000) != 0;
        }

        public static bool IsUnknown1(in long flags)
        {
            return (flags & 0x00080000) != 0;
        }

        public static bool IsMap(in long flags)
        {
            return (flags & 0x00100000) != 0;
        }

        public static bool IsContainer(in long flags)
        {
            return (flags & 0x00200000) != 0;
        }

        public static bool IsWearable(in long flags)
        {
            return (flags & 0x00400000) != 0;
        }

        public static bool IsLightSource(in long flags)
        {
            return (flags & 0x00800000) != 0;
        }

        public static bool IsAnimated(in long flags)
        {
            return (flags & 0x01000000) != 0;
        }

        public static bool IsNoDiagonal(in long flags)
        {
            return (flags & 0x02000000) != 0;
        }

        public static bool IsUnknown2(in long flags)
        {
            return (flags & 0x04000000) != 0;
        }

        public static bool IsArmor(in long flags)
        {
            return (flags & 0x08000000) != 0;
        }

        public static bool IsRoof(in long flags)
        {
            return (flags & 0x10000000) != 0;
        }

        public static bool IsDoor(in long flags)
        {
            return (flags & 0x20000000) != 0;
        }

        public static bool IsStairBack(in long flags)
        {
            return (flags & 0x40000000) != 0;
        }

        public static bool IsStairRight(in long flags)
        {
            return (flags & 0x80000000) != 0;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTiles
    {
        public ulong Flags;
        public ushort TexID;
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroup
    {
        public uint Unknown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTiles[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticTiles
    {
        public ulong Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        public string Name;
    }

    // old

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupOld
    {
        public uint Unknown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesOld
    {
        public uint Flags;
        public ushort TexID;

        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupOld
    {
        public uint Unk;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public StaticTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticTilesOld
    {
        public uint Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;

        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }


    // new 

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupNew
    {
        public uint Unknown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesNew
    {
        public TileFlag Flags;
        public ushort TexID;

        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupNew
    {
        public uint Unk;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public StaticTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticTilesNew
    {
        public TileFlag Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;

        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
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
        Unknown2 = 0x00080000,

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
        Unknown3 = 0x04000000,

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
        StairRight = 0x80000000
    }
}