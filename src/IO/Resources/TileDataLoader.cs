using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class TileDataLoader : ResourceLoader
    {    
        public LandTiles[] LandData { get; private set; }
        public StaticTiles[] StaticData { get; private set; }


        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "tiledata.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul tiledata = new UOFileMul(path, false);
            bool isold = FileManager.ClientVersion < ClientVersions.CV_7090;
            int staticscount = !isold ? (int)(tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupNew>()) / UnsafeMemoryManager.SizeOf<StaticGroupNew>() : (int)(tiledata.Length - 512 * UnsafeMemoryManager.SizeOf<LandGroupOld>()) / UnsafeMemoryManager.SizeOf<StaticGroupOld>();

            if (staticscount > 2048)
                staticscount = 2048;
            tiledata.Seek(0);
            LandData = new LandTiles[Constants.MAX_LAND_DATA_INDEX_COUNT];
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

            //string pathdef = Path.Combine(FileManager.UoFolderPath, "FileManager.Art.def");
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

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }
    }

    internal readonly struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = (TileFlag)flags;
            TexID = textId;
            Name = name;
        }

        public readonly TileFlag Flags;
        public readonly ushort TexID;
        public readonly string Name;

        public bool IsWet => (Flags & TileFlag.Wet) != 0;
        public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
        public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;

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
        public static readonly StaticTiles Empty = default;

        public StaticTiles(ulong flags, byte weight, byte layer, int count, ushort animId, ushort hue, ushort lightIndex, byte height, string name)
        {
            Flags = (TileFlag)flags;
            Weight = weight;
            Layer = layer;
            Count = count;
            AnimID = animId;
            Hue = hue;
            LightIndex = lightIndex;
            Height = height;
            Name = name;
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

        public bool IsAnimated => (Flags & TileFlag.Animation) != 0;
        public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
        public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
        public bool IsSurface => (Flags & TileFlag.Surface) != 0;
        public bool IsWearable => (Flags & TileFlag.Wearable) != 0;
        public bool IsInternal => (Flags & TileFlag.Internal) != 0;
        public bool IsBackground => (Flags & TileFlag.Background) != 0;
        public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
        public bool IsWet => (Flags & TileFlag.Wet) != 0;
        public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
        public bool IsRoof => (Flags & TileFlag.Roof) != 0;
        public bool IsTranslucent => (Flags & TileFlag.Translucent) != 0;
        public bool IsPartialHue => (Flags & TileFlag.PartialHue) != 0;
        public bool IsStackable => (Flags & TileFlag.Generic) != 0;
        public bool IsTransparent => (Flags & TileFlag.Transparent) != 0;
        public bool IsContainer => (Flags & TileFlag.Container) != 0;
        public bool IsDoor => (Flags & TileFlag.Door) != 0;
        public bool IsWall => (Flags & TileFlag.Wall) != 0;
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
