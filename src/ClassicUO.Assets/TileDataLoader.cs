// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Assets
{
    public sealed class TileDataLoader : UOFileLoader
    {
        private static StaticTiles[] _staticData;
        private static LandTiles[] _landData;

        public TileDataLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public ref LandTiles[] LandData => ref _landData;
        public ref StaticTiles[] StaticData => ref _staticData;

        public override void Load()
        {
            var path = FileManager.GetUOFilePath("tiledata.mul");
            FileSystemHelper.EnsureFileExists(path);

            using var tileData = new UOFileMul(path);
            tileData.Seek(0, System.IO.SeekOrigin.Begin);

            var landTiles = new List<LandTiles>();
            var staticTiles = new List<StaticTiles>();
            var isOld = FileManager.Version < ClientVersion.CV_7090;

            Span<byte> buf = stackalloc byte[20];

            for (var i = 0; i < 512; ++i)
            {
                var header = tileData.ReadUInt32();

                for (var j = 0; j < 32; ++j)
                {
                    var flags = isOld ? tileData.ReadUInt32() : tileData.ReadUInt64();
                    var id = tileData.ReadUInt16();
                    tileData.Read(buf);
                    var name = Encoding.UTF8.GetString(buf).Trim('\0');

                    landTiles.Add(new LandTiles(flags, id, name));
                }
            }

            var tsize = isOld ? Unsafe.SizeOf<StaticGroupOld>() : Unsafe.SizeOf<StaticGroupNew>();
            var staticCount = (uint)((tileData.Length - tileData.Position) / tsize);

            for (var i = 0; i < staticCount; ++i)
            {
                var header = tileData.ReadUInt32();

                for (var j = 0; j < 32; j++)
                {
                    var flags = isOld ? tileData.ReadUInt32() : tileData.ReadUInt64();
                    var weight = tileData.ReadUInt8();
                    var layer = tileData.ReadUInt8();
                    var count = tileData.ReadInt32();
                    var animId = tileData.ReadUInt16();
                    var hue = tileData.ReadUInt16();
                    var lightIndex = tileData.ReadUInt16();
                    var height = tileData.ReadUInt8();
                    tileData.Read(buf);
                    var name = Encoding.UTF8.GetString(buf).Trim('\0');

                    staticTiles.Add(new StaticTiles(flags, weight, layer, count, animId, hue, lightIndex, height, name));
                }
            }

            _landData = [.. landTiles];
            _staticData = [.. staticTiles];


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
        }
    }

    public struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = (TileFlag)flags;
            TexID = textId;
            Name = name;
        }

        public TileFlag Flags;
        public ushort TexID;
        public string Name;

        public bool IsWet => (Flags & TileFlag.Wet) != 0;
        public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
        public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
    }


    public struct StaticTiles
    {
        public StaticTiles
        (
            ulong flags,
            byte weight,
            byte layer,
            int count,
            ushort animId,
            ushort hue,
            ushort lightIndex,
            byte height,
            string name
        )
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

        public TileFlag Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        public string Name;

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
        public bool IsLight => (Flags & TileFlag.LightSource) != 0;
        public bool IsNoShoot => (Flags & TileFlag.NoShoot) != 0;
        public bool IsWeapon => (Flags & TileFlag.Weapon) != 0;
        public bool IsMultiMovable => (Flags & TileFlag.MultiMovable) != 0;
        public bool IsWindow => (Flags & TileFlag.Window) != 0;


        public static readonly StaticTiles Invalid = new StaticTiles();
    }

    // old

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupOld
    {
        public uint Unknown;
        public LandTilesOldArray32 Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesOld
    {
        public uint Flags;
        public ushort TexID;
        public BufferArray20 Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupOld
    {
        public uint Unk;
        public StaticTilesOldArray32 Tiles;
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
        public BufferArray20 Name;
    }

    // new

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupNew
    {
        public uint Unknown;
        public LandTilesNewArray32 Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesNew
    {
        public TileFlag Flags;
        public ushort TexID;
        public BufferArray20 Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupNew
    {
        public uint Unk;
        public StaticTilesNewArray32 Tiles;
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
        public BufferArray20 Name;
    }


    [InlineArray(32)]
    public struct LandTilesOldArray32
    {
        private LandTilesOld _a;
    }

    [InlineArray(32)]
    public struct LandTilesNewArray32
    {
        private LandTilesNew _a;
    }

    [InlineArray(32)]
    public struct StaticTilesOldArray32
    {
        private StaticTilesOld _a;
    }

    [InlineArray(32)]
    public struct StaticTilesNewArray32
    {
        private StaticTilesNew _a;
    }

    [InlineArray(20)]
    public struct BufferArray20
    {
        private byte _a;
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