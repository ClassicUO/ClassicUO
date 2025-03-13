// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Sdk.IO;

namespace ClassicUO.Sdk.Assets
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
            string path = FileManager.GetUOFilePath("tiledata.mul");

            var tileData = new UOFileMul(path);

            bool isold = FileManager.Version < ClientVersion.CV_7090;
            const int LAND_SIZE = 512;

            int land_group = isold ? Marshal.SizeOf<LandGroupOld>() : Marshal.SizeOf<LandGroupNew>();
            int static_group = isold ? Marshal.SizeOf<StaticGroupOld>() : Marshal.SizeOf<StaticGroupNew>();
            int staticscount = (int) ((tileData.Length - LAND_SIZE * land_group) / static_group);

            if (staticscount > 2048)
            {
                staticscount = 2048;
            }

            tileData.Seek(0, System.IO.SeekOrigin.Begin);

            _landData = new LandTiles[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _staticData = new StaticTiles[staticscount * 32];

            Span<byte> buf = stackalloc byte[20];

            for (int i = 0; i < 512; i++)
            {
                tileData.ReadUInt32();

                for (int j = 0; j < 32; j++)
                {
                    if (tileData.Position + (isold ? 4 : 8) + 2 + 20 > tileData.Length)
                    {
                        goto END;
                    }

                    int idx = i * 32 + j;
                    ulong flags = isold ? tileData.ReadUInt32() : tileData.ReadUInt64();
                    ushort textId = tileData.ReadUInt16();

                    tileData.Read(buf);
                    var name = string.Intern(Encoding.UTF8.GetString(buf).TrimEnd('\0'));
                    LandData[idx] = new LandTiles(flags, textId, name);
                }
            }

            END:

            for (int i = 0; i < staticscount; i++)
            {
                if (tileData.Position >= tileData.Length)
                {
                    break;
                }

                tileData.ReadUInt32();

                for (int j = 0; j < 32; j++)
                {
                    if (tileData.Position + (isold ? 4 : 8) + 13 + 20 > tileData.Length)
                    {
                        goto END_2;
                    }

                    int idx = i * 32 + j;

                    ulong flags = isold ? tileData.ReadUInt32() : tileData.ReadUInt64();
                    byte weight = tileData.ReadUInt8();
                    byte layer = tileData.ReadUInt8();
                    int count = tileData.ReadInt32();
                    ushort animId = tileData.ReadUInt16();
                    ushort hue = tileData.ReadUInt16();
                    ushort lightIndex = tileData.ReadUInt16();
                    byte height = tileData.ReadUInt8();

                    tileData.Read(buf);
                    var name = string.Intern(Encoding.UTF8.GetString(buf).TrimEnd('\0'));

                    StaticData[idx] = new StaticTiles
                    (
                        flags,
                        weight,
                        layer,
                        count,
                        animId,
                        hue,
                        lightIndex,
                        height,
                        name
                    );
                }
            }

            END_2:
            tileData.Dispose();
        }
    }

    public struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = (TileFlag) flags;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroup
    {
        public uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTiles[] Tiles;
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
            Flags = (TileFlag) flags;
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