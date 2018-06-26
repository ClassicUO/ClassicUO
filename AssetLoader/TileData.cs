using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace ClassicUO.Assets
{
    public static class TileData
    {

        private static LandTiles[] _landData;
        private static StaticTiles[] _staticData;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "tiledata.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            UOFileMul tiledata = new UOFileMul(path);


            bool isold = FileManager.ClientVersion < ClientVersions.CV_7090;

            int staticscount = !isold ? 
                (int)(tiledata.Length - (512 * Marshal.SizeOf<LandGroupNew>())) / Marshal.SizeOf<StaticGroupNew>()
                :
                (int)(tiledata.Length - (512 * Marshal.SizeOf<LandGroupOld>())) / Marshal.SizeOf<StaticGroupOld>();

            if (staticscount > 2048)
                staticscount = 2048;

            tiledata.Seek(0);

            _landData = new LandTiles[512 * 32];
            _staticData = new StaticTiles[staticscount * 32];

            for (int i = 0; i < 512; i++)
            {
                tiledata.Skip(4);
                for (int j = 0; j < 32; j++)
                {
                    int idx = (i * 32) + j;
                    _landData[idx].Flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                    _landData[idx].TexID = tiledata.ReadUShort();
                    _landData[idx].Name = Encoding.UTF8.GetString(tiledata.ReadArray<byte>(20));
                }
            }

            for (int i = 0; i < staticscount; i++)
            {
                tiledata.Skip(4);
                for (int j = 0; j < 32; j++)
                {
                    int idx = (i * 32) + j;
                    _staticData[idx].Flags = isold ? tiledata.ReadUInt() : tiledata.ReadULong();
                    _staticData[idx].Weight = tiledata.ReadByte();
                    _staticData[idx].Layer = tiledata.ReadByte();
                    _staticData[idx].Count = tiledata.ReadInt();
                    _staticData[idx].AnimID = tiledata.ReadUShort();
                    _staticData[idx].Hue = tiledata.ReadUShort();
                    _staticData[idx].LightIndex = tiledata.ReadUShort();
                    _staticData[idx].Height = tiledata.ReadByte();
                    _staticData[idx].Name = Encoding.UTF8.GetString(tiledata.ReadArray<byte>(20));
                }
            }
        }

        public static LandTiles[] LandData => _landData;
        public static StaticTiles[] StaticData => _staticData;
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
        StairRight = unchecked(0x80000000)
    }
}
