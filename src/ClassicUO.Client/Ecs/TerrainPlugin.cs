using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;


struct OnNewChunkRequest { public int Map; public int RangeStartX, RangeStartY, RangeEndX, RangeEndY; }

struct TileStretched
{
    public sbyte AvgZ, MinZ;
    public Renderer.UltimaBatcher2D.YOffsets Offset;
    public Vector3 NormalTop, NormalRight, NormalLeft, NormalBottom;
}

readonly struct TerrainPlugin : IPlugin
{
    public unsafe void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<OnNewChunkRequest>();
        scheduler.AddResource(new HashSet<(int chunkX, int chunkY, int mapIndex)>());

        scheduler.AddSystem((
            Res<GameContext> gameCtx,
            Query<WorldPosition, With<Player>> playerQuery,
            EventWriter<OnNewChunkRequest> chunkRequests,
            Local<(ushort? LastX, ushort? LastY)> lastPos
        ) =>
        {
            ref var pos = ref playerQuery.Single<WorldPosition>();

            if (lastPos.Value.LastX.HasValue && lastPos.Value.LastY.HasValue)
                if (lastPos.Value.LastX == pos.X && lastPos.Value.LastY == pos.Y)
                    return;

            if (gameCtx.Value.Map == -1)
                return;

            lastPos.Value.LastX = pos.X;
            lastPos.Value.LastY = pos.Y;

            var offset = 8;
            chunkRequests.Enqueue(new() {
                Map = gameCtx.Value.Map,
                RangeStartX = Math.Max(0, pos.X / 8 - offset),
                RangeStartY = Math.Max(0, pos.Y / 8 - offset),
                RangeEndX = Math.Min(gameCtx.Value.MaxMapWidth / 8, pos.X / 8 + offset),
                RangeEndY = Math.Min(gameCtx.Value.MaxMapHeight / 8, pos.Y / 8 + offset),
            });
        }).RunIf((Query<WorldPosition, With<Player>> playerQuery) => playerQuery.Count() > 0);

        scheduler.AddSystem(static (
            TinyEcs.World world,
            Res<UOFileManager> fileManager,
            Res<AssetsServer> assetsServer,
            Res<HashSet<(int ChunkX, int ChunkY, int MapIndex)>> chunksLoaded,
            EventReader<OnNewChunkRequest> chunkRequests
        ) => {
            foreach (var chunkEv in chunkRequests)
            {
                for (int chunkX = chunkEv.RangeStartX; chunkX <= chunkEv.RangeEndX; chunkX += 1)
                for (int chunkY = chunkEv.RangeStartY; chunkY <= chunkEv.RangeEndY; chunkY += 1)
                {
                    ref var im = ref fileManager.Value.Maps.GetIndex(chunkEv.Map, chunkX, chunkY);

                    if (im.MapAddress == 0)
                        continue;

                    if (!chunksLoaded.Value.Add((chunkX, chunkY, chunkEv.Map)))
                        continue;

                    im.MapFile.Seek((long)im.MapAddress, System.IO.SeekOrigin.Begin);
                    var cells = im.MapFile.Read<Assets.MapBlock>().Cells;

                    var bx = chunkX << 3;
                    var by = chunkY << 3;

                    for (int y = 0; y < 8; ++y)
                    {
                        var pos = y << 3;
                        var tileY = (ushort) (by + y);

                        for (int x = 0; x < 8; ++x, ++pos)
                        {
                            var tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                            var z = cells[pos].Z;
                            var tileX = (ushort) (bx + x);

                            var isStretched = fileManager.Value.TileData.LandData[tileID].TexID == 0 &&
                                fileManager.Value.TileData.LandData[tileID].IsWet;

                            isStretched = ApplyStretch(
                                fileManager.Value.Maps, fileManager.Value.Texmaps,
                                chunkEv.Map, fileManager.Value.TileData.LandData[tileID].TexID,
                                tileX, tileY, z,
                                isStretched,
                                out var avgZ, out var minZ,
                                out var offsets,
                                out var normalTop,
                                out var normalRight,
                                out var normalBottom,
                                out var normalLeft
                            );

                            if (isStretched)
                            {
                                ref readonly var textmapInfo = ref assetsServer.Value.Texmaps.GetTexmap(fileManager.Value.TileData.LandData[tileID].TexID);

                                var position = Isometric.IsoToScreen(tileX, tileY, z);
                                position.Y += z << 2;

                                world.Entity()
                                    .Set(new Renderable() {
                                        Texture = textmapInfo.Texture,
                                        UV = textmapInfo.UV,
                                        Color = new Vector3(0, Renderer.ShaderHueTranslator.SHADER_LAND, 1f),
                                        Position = position,
                                        Z = Isometric.GetDepthZ(tileX, tileY, avgZ - 2)
                                    })
                                    .Set(new TileStretched() {
                                        NormalTop = normalTop,
                                        NormalRight = normalRight,
                                        NormalBottom = normalBottom,
                                        NormalLeft = normalLeft,
                                        AvgZ = avgZ,
                                        MinZ = minZ,
                                        Offset = offsets
                                    });
                            }
                            else
                            {
                                ref readonly var artInfo = ref assetsServer.Value.Arts.GetLand(tileID);

                                world.Entity()
                                    .Set(new Renderable() {
                                        Texture = artInfo.Texture,
                                        UV = artInfo.UV,
                                        Color = Vector3.UnitZ,
                                        Position = Isometric.IsoToScreen(tileX, tileY, z),
                                        Z = Isometric.GetDepthZ(tileX, tileY, z - 2)
                                    });
                            }
                        }
                    }

                    if (im.StaticAddress != 0)
                    {
                        im.StaticFile.Seek((long)im.StaticAddress, System.IO.SeekOrigin.Begin);

                        for (int i = 0, count = (int)im.StaticCount; i < count; ++i)
                        {
                            var sb = im.StaticFile.Read<Assets.StaticsBlock>();

                            if (sb.Color != 0 && sb.Color != 0xFFFF)
                            {
                                int pos = (sb.Y << 3) + sb.X;

                                if (pos >= 64)
                                {
                                    continue;
                                }

                                var staX = (ushort)(bx + sb.X);
                                var staY = (ushort)(by + sb.Y);

                                ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(sb.Color);

                                var priorityZ = sb.Z;

                                if (fileManager.Value.TileData.StaticData[sb.Color].IsBackground)
                                {
                                    priorityZ -= 1;
                                }

                                if (fileManager.Value.TileData.StaticData[sb.Color].Height != 0)
                                {
                                    priorityZ += 1;
                                }

                                if (fileManager.Value.TileData.StaticData[sb.Color].IsMultiMovable)
                                {
                                    priorityZ += 1;
                                }

                                var posVec = Isometric.IsoToScreen(staX, staY, sb.Z);
                                posVec.X -= (short)((artInfo.UV.Width >> 1) - 22);
                                posVec.Y -= (short)(artInfo.UV.Height - 44);
                                world.Entity()
                                    .Set(new Renderable()
                                    {
                                        Texture = artInfo.Texture,
                                        UV = artInfo.UV,
                                        Color = Renderer.ShaderHueTranslator.GetHueVector(sb.Hue, fileManager.Value.TileData.StaticData[sb.Color].IsPartialHue, 1f),
                                        Position = posVec,
                                        Z = Isometric.GetDepthZ(staX, staY, priorityZ)
                                    });
                            }
                        }
                    }
                }
            }
        }, threadingType: ThreadingMode.Single)
        .RunIf((EventReader<OnNewChunkRequest> reader) => !reader.IsEmpty);
    }



    static bool ApplyStretch(
        MapLoader mapLoader,
        TexmapsLoader texmapsLoader,
        int mapIndex,
        ushort texId, int x, int y, sbyte z,
        bool isStretched, out sbyte avgZ, out sbyte minZ,
        out Renderer.UltimaBatcher2D.YOffsets offsets,
        out Vector3 normalTop,
        out Vector3 normalRight,
        out Vector3 normalBottom,
        out Vector3 normalLeft)
    {
        if (isStretched || texmapsLoader.File.GetValidRefEntry(texId).Length <= 0)
        {
            isStretched = false;
            avgZ = z;
            minZ = z;
            offsets = new Renderer.UltimaBatcher2D.YOffsets();
            normalTop = normalRight = normalBottom = normalLeft = Vector3.Zero;

            return false;
        }

        /* _____ _____
        * | top | rig |
        * |_____|_____|
        * | lef | bot |
        * |_____|_____|
        */
        var zTop = z;
        var zRight = GetTileZ(mapLoader, mapIndex, x + 1, y);
        var zLeft = GetTileZ(mapLoader, mapIndex, x, y + 1);
        sbyte zBottom = GetTileZ(mapLoader, mapIndex, x + 1, y + 1);

        offsets.Top = zTop * 4;
        offsets.Right = zRight * 4;
        offsets.Left = zLeft * 4;
        offsets.Bottom = zBottom * 4;

        if (Math.Abs(zTop - zBottom) <= Math.Abs(zLeft - zRight))
        {
            avgZ = (sbyte) ((zTop + zBottom) >> 1);
        }
        else
        {
            avgZ = (sbyte) ((zLeft + zRight) >> 1);
        }

        minZ = Math.Min(zTop, Math.Min(zRight, Math.Min(zLeft, zBottom)));


        /* _____ _____ _____ _____
        * |     | t10 | t20 |     |
        * |_____|_____|_____|_____|
        * | t01 |  z  | t21 | t31 |
        * |_____|_____|_____|_____|
        * | t02 | t12 | t22 | t32 |
        * |_____|_____|_____|_____|
        * |     | t13 | t23 |     |
        * |_____|_____|_____|_____|
        */
        var t10 = GetTileZ(mapLoader, mapIndex, x, y - 1);
        var t20 = GetTileZ(mapLoader, mapIndex, x + 1, y - 1);
        var t01 = GetTileZ(mapLoader, mapIndex, x - 1, y);
        var t21 = zRight;
        var t31 = GetTileZ(mapLoader, mapIndex, x + 2, y);
        var t02 = GetTileZ(mapLoader, mapIndex, x - 1, y + 1);
        var t12 = zLeft;
        var t22 = zBottom;
        var t32 = GetTileZ(mapLoader, mapIndex, x + 2, y + 1);
        var t13 = GetTileZ(mapLoader, mapIndex, x, y + 2);
        var t23 = GetTileZ(mapLoader, mapIndex, x + 1, y + 2);


        isStretched |= CalculateNormal(z, t10, t21, t12, t01, out normalTop);
        isStretched |= CalculateNormal(t21, t20, t31, t22, z, out normalRight);
        isStretched |= CalculateNormal(t22, t21, t32, t23, t12, out normalBottom);
        isStretched |= CalculateNormal(t12, z, t22, t13, t02, out normalLeft);

        return isStretched;
    }

    private unsafe static sbyte GetTileZ(MapLoader mapLoader, int mapIndex, int x, int y)
    {
        static ref Assets.IndexMap getIndex(MapLoader mapLoader, int mapIndex, int x, int y)
        {
            var block = getBlock(mapLoader, mapIndex, x, y);
            mapLoader.SanitizeMapIndex(ref mapIndex);
            var list = mapLoader.BlockData[mapIndex];

            return ref block >= list.Length ? ref Assets.IndexMap.Invalid : ref list[block];

            static int getBlock(MapLoader mapLoader, int mapIndex, int blockX, int blockY)
                => blockX * mapLoader.MapBlocksSize[mapIndex, 1] + blockY;
        }


        if (x < 0 || y < 0)
        {
            return -125;
        }

        ref var blockIndex = ref getIndex(mapLoader, mapIndex, x >> 3, y >> 3);

        if (blockIndex.MapAddress == 0)
        {
            return -125;
        }

        int mx = x % 8;
        int my = y % 8;

        blockIndex.MapFile.Seek((long)blockIndex.MapAddress, System.IO.SeekOrigin.Begin);
        return blockIndex.MapFile.Read<MapBlock>().Cells[(my << 3) + mx].Z;
    }

    private static bool CalculateNormal(sbyte tile, sbyte top, sbyte right, sbyte bottom, sbyte left, out Vector3 normal)
    {
        if (tile == top && tile == right && tile == bottom && tile == left)
        {
            normal.X = 0;
            normal.Y = 0;
            normal.Z = 1f;

            return false;
        }

        Vector3 u = new Vector3();
        Vector3 v = new Vector3();
        Vector3 ret = new Vector3();


        // ==========================
        u.X = -22;
        u.Y = -22;
        u.Z = (left - tile) * 4;

        v.X = -22;
        v.Y = 22;
        v.Z = (bottom - tile) * 4;

        Vector3.Cross(ref v, ref u, out ret);
        // ==========================


        // ==========================
        u.X = -22;
        u.Y = 22;
        u.Z = (bottom - tile) * 4;

        v.X = 22;
        v.Y = 22;
        v.Z = (right - tile) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ==========================


        // ==========================
        u.X = 22;
        u.Y = 22;
        u.Z = (right - tile) * 4;

        v.X = 22;
        v.Y = -22;
        v.Z = (top - tile) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ==========================


        // ==========================
        u.X = 22;
        u.Y = -22;
        u.Z = (top - tile) * 4;

        v.X = -22;
        v.Y = -22;
        v.Z = (left - tile) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ==========================


        Vector3.Normalize(ref ret, out normal);

        return true;
    }

}