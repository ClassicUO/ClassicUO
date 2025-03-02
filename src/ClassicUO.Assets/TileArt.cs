// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Assets;


// ( •_•)>⌐■-■
// https://github.com/cbnolok/UOETE/blob/master/src/uotileart.cpp
public sealed class TileArtLoader : UOFileLoader
{
    private readonly Dictionary<uint, TileArtInfo> _tileArtInfos = [];
    private UOFileUop _file;

    public TileArtLoader(UOFileManager fileManager) : base(fileManager)
    {

    }


    public bool TryGetTileArtInfo(uint graphic, out TileArtInfo tileArtInfo)
    {
        if (_tileArtInfos.TryGetValue(graphic, out tileArtInfo))
            return true;

        if (LoadEntry(graphic, out tileArtInfo))
        {
            _tileArtInfos.Add(graphic, tileArtInfo);
            return true;
        }

        return false;
    }

    private bool LoadEntry(uint graphic, out TileArtInfo tileArtInfo)
    {
        tileArtInfo = null;
        if (_file == null)
            return false;

        ref var entry = ref _file.GetValidRefEntry((int)graphic);
        if (entry.Length == 0)
            return false;

        var buf = ArrayPool<byte>.Shared.Rent(entry.Length);
        var dbuf = ArrayPool<byte>.Shared.Rent(entry.DecompressedLength);

        try
        {
            var bufSpan = buf.AsSpan(0, entry.Length);
            var dbufSpan = dbuf.AsSpan(0, entry.DecompressedLength);

            _file.Seek(entry.Offset, SeekOrigin.Begin);
            _file.Read(bufSpan);

            var result = ZLib.Decompress(bufSpan, dbufSpan);
            if (result != ZLib.ZLibError.Ok)
            {
                return false;
            }

            var reader = new StackDataReader(dbufSpan);
            tileArtInfo = new(ref reader);

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
            ArrayPool<byte>.Shared.Return(dbuf);
        }
    }

    public override void Load()
    {
        var path = FileManager.GetUOFilePath("tileart.uop");
        if (!File.Exists(path))
            return;

        _file = new UOFileUop(path, "build/tileart/{0:D8}.bin");
        _file.FillEntries();
    }
}

[Flags]
public enum TAEFlag : ulong
{
    None = 0x0,  //0uL,
    Background = 0x1,  //1uL,
    Weapon = 0x2,  //2uL,
    Transparent = 0x4,  //4uL,
    Translucent = 0x8,  //8uL,
    Wall = 0x10, //16uL,
    Damaging = 0x20, //32uL,
    Impassable = 0x40, //64uL,
    Wet = 0x80, //128uL,
    Ignored = 0x100,    //256uL,
    Surface = 0x200,    //512uL,
    Bridge = 0x400,    //1024uL,
    Generic = 0x800,    //2048uL,
    Window = 0x1000,   //4096uL,
    NoShoot = 0x2000,   //8192uL,
    ArticleA = 0x4000,   //16384uL,
    ArticleAn = 0x8000,   //32768uL,
    ArticleThe = ArticleA | ArticleAn,  //49152uL,
    Mongen = 0x10000,  //65536uL,
    Foliage = 0x20000,  //131072uL,
    PartialHue = 0x40000,  //262144uL,
    UseNewArt = 0x80000,      //524288uL,
    Map = 0x100000,     //1048576uL,
    Container = 0x200000,     //2097152uL,
    Wearable = 0x400000,     //4194304uL,
    LightSource = 0x800000,     //8388608uL,
    Animation = 0x1000000,    //16777216uL,
    HoverOver = 0x2000000,    //33554432uL,
    ArtUsed = 0x4000000,    //67108864uL,
    Armor = 0x8000000,    //134217728uL,
    Roof = 0x10000000,   //268435456uL,
    Door = 0x20000000,   //536870912uL,
    StairBack = 0x40000000,   //1073741824uL,
    StairRight = 0x80000000,   //2147483648uL,
    NoHouse = 0x100000000,  //4294967296uL,
    NoDraw = 0x200000000,  //8589934592uL,
    Unused1 = 0x400000000,  //17179869184uL,
    AlphaBlend = 0x800000000,  //34359738368uL,
    NoShadow = 0x1000000000, //68719476736uL,
    PixelBleed = 0x2000000000, //137438953472uL,
    Unused2 = 0x4000000000, //274877906944uL,
    PlayAnimOnce = 0x8000000000, //549755813888uL,
    MultiMovable = 0x10000000000 //1099511627776uL
};

public enum TAEPropID : byte
{
    Weight = 0,
    Quality,
    Quantity,
    Height,
    Value,
    AcVc,
    Slot,
    Off_C8,
    Appearance,
    Race,
    Gender,
    Paperdoll
}

public sealed class TileArtInfo
{
    internal TileArtInfo(ref StackDataReader reader)
    {
        var version = reader.ReadUInt16LE();
        if (version != 4)
        {
            Log.Info($"tileart.uop v{version} is not supported.");
            return;
        }

        var stringDictOffset = reader.ReadUInt32LE();
        TileId = version >= 4 ? reader.ReadUInt32LE() : reader.ReadUInt16LE();
        var unkBool1 = reader.ReadBool();
        var unkBool2 = reader.ReadBool();
        var unkFloat1 = reader.ReadUInt32LE();
        var unkFloat2 = reader.ReadUInt32LE();
        var fixedZero = reader.ReadUInt32LE();
        var oldId = reader.ReadUInt32LE();
        var unkFloat3 = reader.ReadUInt32LE();
        BodyType = reader.ReadUInt32LE();
        var unkByte = reader.ReadUInt8();
        var unkDw1 = reader.ReadUInt32LE();
        var unkDw2 = reader.ReadUInt32LE();
        Lights[0] = reader.ReadUInt32LE();
        Lights[1] = reader.ReadUInt32LE();
        var unkDw3 = reader.ReadUInt32LE();
        Flags[0] = (TAEFlag)reader.ReadUInt64LE();
        Flags[1] = (TAEFlag)reader.ReadUInt64LE();
        var facing = reader.ReadUInt32LE();
        (var startX, var startY,
        var endX, var endY,
        var offX, var offY) = (
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE()
        );
        (var startX2, var startY2,
        var endX2, var endY2,
        var offX2, var offY2) = (
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE()
        );

        var propCount = reader.ReadUInt8();
        for (var j = 0; j < propCount; ++j)
        {
            var propId = (TAEPropID)reader.ReadUInt8();
            var propVal = reader.ReadUInt32LE();

            Props[0].Add((propId, propVal));
        }

        var propCount2 = reader.ReadUInt8();
        for (var j = 0; j < propCount2; ++j)
        {
            var propId = (TAEPropID)reader.ReadUInt8();
            var propVal = reader.ReadUInt32LE();

            Props[1].Add((propId, propVal));
        }

        var stackAliasCount = reader.ReadUInt32LE();
        for (var j = 0; j < stackAliasCount; ++j)
        {
            var amount = reader.ReadUInt32LE();
            var amountId = reader.ReadUInt32LE();

            StackAliases.Add((amount, amountId));
        }

        var appearanceCount = reader.ReadUInt32LE();

        for (var j = 0; j < appearanceCount; ++j)
        {
            var subType = reader.ReadUInt8();
            if (subType == 1)
            {
                var unk1 = reader.ReadUInt8();
                var unk2 = reader.ReadUInt32LE();
            }
            else
            {
                var subCount = reader.ReadUInt32LE();

                if (!Appearances.TryGetValue(subType, out var dict))
                {
                    dict = [];
                    Appearances.Add(subType, dict);
                }

                for (var k = 0; k < subCount; ++k)
                {
                    var val = reader.ReadUInt32LE();
                    var animId = reader.ReadUInt32LE();

                    uint offset = val / 1000;
                    uint body = val % 1000;

                    if (!dict.TryAdd(body, animId + offset))
                    {

                    }
                }
            }
        }

        var hasSitting = reader.ReadBool();
        if (hasSitting)
        {
            var unk1 = reader.ReadUInt32LE();
            var unk2 = reader.ReadUInt32LE();
            var unk3 = reader.ReadUInt32LE();
            var unk4 = reader.ReadUInt32LE();
        }

        var radColor = reader.ReadUInt32LE();

        for (var i = 0; i < 4; ++i)
        {
            var hasTexture = reader.ReadInt8();
            if (hasTexture != 0)
            {
                if (hasTexture != 1)
                {
                    // ???
                    break;
                }

                var unk1 = reader.ReadUInt8();
                var typeStringOffset = reader.ReadUInt32LE();
                var textureItemsCount = reader.ReadUInt8();
                for (var j = 0; j < textureItemsCount; ++j)
                {
                    var nameStringOff = reader.ReadUInt32LE();
                    var unk2 = reader.ReadUInt8();
                    var unk3 = reader.ReadInt32LE();
                    var unk4 = reader.ReadInt32LE();
                    var unk5 = reader.ReadUInt32LE();
                }

                var unk6Count = reader.ReadUInt32LE();
                for (var j = 0; j < unk6Count; ++j)
                {
                    var unk9 = reader.ReadUInt32LE();
                }

                var unk10Count = reader.ReadUInt32LE();
                for (var j = 0; j < unk6Count; ++j)
                {
                    var unk11 = reader.ReadUInt32LE();
                }
            }
        }

        var unk12 = reader.ReadUInt8();
    }


    public uint TileId { get; }
    public uint BodyType { get; }
    public uint[] Lights { get; } = [0, 0];
    public TAEFlag[] Flags { get; } = [0, 0];
    public List<(TAEPropID PropType, uint Value)>[] Props { get; } = [[], []];
    public List<(uint, uint)> StackAliases { get; } = [];
    public Dictionary<byte, Dictionary<uint, uint>> Appearances { get; } = [];


    public bool TryGetAppearance(uint mobGraphic, out uint appearanceId)
    {
        appearanceId = 0;

        // get in account only type 0 for some unknown reason :D
        // added the Appearances.Count > 1 because seems like the conversion should happen only when there is more than 1 appearance (?)
        return Appearances.Count > 1 && Appearances.TryGetValue(0, out var appearanceDict) &&
            appearanceDict.TryGetValue(mobGraphic, out appearanceId);
    }
}
