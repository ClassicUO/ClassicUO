using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct TerrainPlugin : IPlugin
{
    private struct OnNewChunkRequest { public int Map; public int RangeStartX, RangeStartY, RangeEndX, RangeEndY; }
    private struct Terrain { }
    private struct TerrainChunk { public int X, Y; }

    internal sealed class ChunksLoadedMap : Dictionary<uint, (uint Time, ulong entity)> { }
    internal sealed class LastPosition { public int Map = -1; public ushort? LastX, LastY; public (int, int)? LastCameraBounds; public float LastCameraZoom; }

    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<OnNewChunkRequest>();
        scheduler.AddResource(new ChunksLoadedMap());
        scheduler.AddResource(new LastPosition());

        var enqueueChunksRequestsFn = EnqueueChunksRequests;
        scheduler.OnUpdate(enqueueChunksRequestsFn, ThreadingMode.Single)
            .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery) => playerQuery.Count() > 0);

        var loadChunksFn = LoadChunks;
        scheduler.OnUpdate(loadChunksFn, ThreadingMode.Single)
            .RunIf((EventReader<OnNewChunkRequest> reader) => !reader.IsEmpty);

        var checkChunksFn = CheckChunk;
        scheduler.OnUpdate(checkChunksFn, ThreadingMode.Single)
            .RunIf((Query<Data<TerrainChunk>> query, SchedulerState state)
                => query.Count() > 0 && state.InState(GameState.GameScreen));

        var removeEntitiesOutOfRangeFn = RemoveEntitiesOutOfRange;
        scheduler.OnUpdate(removeEntitiesOutOfRangeFn, ThreadingMode.Single)
            .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery, Local<float> timeUpdate, Time time) =>
            {
                if (timeUpdate.Value > time.Total)
                    return false;
                timeUpdate.Value = time.Total + 3000;
                return playerQuery.Count() > 0;
            });

        scheduler.OnExit(GameState.GameScreen, (
            Res<ChunksLoadedMap> chunksLoaded,
            Res<GameContext> gameCtx,
            Res<LastPosition> lastPos,
            EventReader<OnNewChunkRequest> reader,
            Query<Data<WorldPosition>, With<Terrain>> query
        ) =>
        {
            foreach ((var ent, _) in query)
            {
                ent.Ref.Delete();
            }

            chunksLoaded.Value.Clear();
            reader.Clear();
            lastPos.Value.LastX = null;
            lastPos.Value.LastY = null;
            lastPos.Value.LastCameraBounds = null;
            lastPos.Value.LastCameraZoom = 0f;
        }, ThreadingMode.Single);
    }

    private static int GetCameraOffset(Camera camera)
    {
        var cameraBounds = camera.Bounds;
        var maxSize = Math.Max(cameraBounds.Width, cameraBounds.Height);
        maxSize /= 60;
        return (int)(maxSize * camera.Zoom);
    }

    private static void CheckChunk(
        Time time,
        Res<Camera> camera,
        Res<GameContext> gameCtx,
        Res<UOFileManager> fileManager,
        Res<ChunksLoadedMap> chunksLoaded,
        Single<Data<WorldPosition>, With<Player>> playerQuery,
        Query<Data<TerrainChunk>> query)
    {
        (_, var pos) = playerQuery.Get();

        var offset = GetCameraOffset(camera.Value);
        var startTileX = Math.Max(0, pos.Ref.X - offset) / 8;
        var startTileY = Math.Max(0, pos.Ref.Y - offset) / 8;
        var endTileX = Math.Min(fileManager.Value.Maps.MapsDefaultSize[gameCtx.Value.Map, 0], pos.Ref.X + offset) / 8;
        var endTileY = Math.Min(fileManager.Value.Maps.MapsDefaultSize[gameCtx.Value.Map, 1], pos.Ref.Y + offset) / 8;

        foreach ((_, var chunk) in query)
        {
            var chunkX = chunk.Ref.X;
            var chunkY = chunk.Ref.Y;

            if (chunkX >= startTileX && chunkX <= endTileX && chunkY >= startTileY && chunkY <= endTileY)
            {
                var key = CreateChunkKey(chunk.Ref.X, chunk.Ref.Y);
                ref var res = ref CollectionsMarshal.GetValueRefOrNullRef(chunksLoaded.Value, key);
                if (!Unsafe.IsNullRef(ref res))
                {
                    res.Time = (uint)time.Total + 5000;
                }
            }
        }
    }

    private static void EnqueueChunksRequests
    (
        Res<GameContext> gameCtx,
        Res<LastPosition> lastPos,
        Res<Camera> camera,
        Res<UOFileManager> fileManager,
        Single<Data<WorldPosition>, With<Player>> playerQuery,
        EventWriter<OnNewChunkRequest> chunkRequests
    )
    {
        if (gameCtx.Value.Map == -1)
        {
            return;
        }

        (_, var pos) = playerQuery.Get();

        if (lastPos.Value.LastCameraBounds.HasValue && lastPos.Value.LastX.HasValue && lastPos.Value.LastY.HasValue)
            if (lastPos.Value.Map != gameCtx.Value.Map &&
                lastPos.Value.LastX == pos.Ref.X && lastPos.Value.LastY == pos.Ref.Y &&
                lastPos.Value.LastCameraBounds.Value.Item1 == camera.Value.Bounds.Width &&
                lastPos.Value.LastCameraBounds.Value.Item2 == camera.Value.Bounds.Height &&
                lastPos.Value.LastCameraZoom == camera.Value.Zoom)
                return;

        lastPos.Value.Map = gameCtx.Value.Map;
        lastPos.Value.LastX = pos.Ref.X;
        lastPos.Value.LastY = pos.Ref.Y;
        lastPos.Value.LastCameraBounds = (camera.Value.Bounds.Width, camera.Value.Bounds.Height);
        lastPos.Value.LastCameraZoom = camera.Value.Zoom;

        var offset = GetCameraOffset(camera.Value);
        var startTileX = Math.Max(0, pos.Ref.X - offset);
        var startTileY = Math.Max(0, pos.Ref.Y - offset);
        var endTileX = Math.Min(fileManager.Value.Maps.MapsDefaultSize[gameCtx.Value.Map, 0], pos.Ref.X + offset);
        var endTileY = Math.Min(fileManager.Value.Maps.MapsDefaultSize[gameCtx.Value.Map, 1], pos.Ref.Y + offset);

        chunkRequests.Enqueue(new()
        {
            Map = gameCtx.Value.Map,
            RangeStartX = startTileX / 8,
            RangeStartY = startTileY / 8,
            RangeEndX = endTileX / 8,
            RangeEndY = endTileY / 8,
        });
    }

    private static void LoadChunks
    (
        World world,
        Time time,
        Res<UOFileManager> fileManager,
        Res<ChunksLoadedMap> chunksLoaded,
        Local<StaticsBlock[]> staticsBlockBuffer,
        EventReader<OnNewChunkRequest> chunkRequests
    )
    {
        staticsBlockBuffer.Value ??= new StaticsBlock[64];

        foreach (var chunkEv in chunkRequests)
        {
            for (int chunkX = chunkEv.RangeStartX; chunkX <= chunkEv.RangeEndX; chunkX += 1)
            {
                for (int chunkY = chunkEv.RangeStartY; chunkY <= chunkEv.RangeEndY; chunkY += 1)
                {
                    ref var im = ref fileManager.Value.Maps.GetIndex(chunkEv.Map, chunkX, chunkY);

                    if (im.MapAddress == 0)
                        continue;

                    var key = CreateChunkKey(chunkX, chunkY);
                    if (chunksLoaded.Value.ContainsKey(key))
                        continue;

                    var chunk = world.Entity()
                        .Set(new TerrainChunk() { X = chunkX, Y = chunkY });

                    chunksLoaded.Value.Add(key, ((uint)time.Total + 5000, chunk.ID));

                    im.MapFile.Seek((long)im.MapAddress, System.IO.SeekOrigin.Begin);
                    var cells = im.MapFile.Read<Assets.MapBlock>().Cells;

                    var bx = chunkX << 3;
                    var by = chunkY << 3;

                    for (int y = 0; y < 8; ++y)
                    {
                        var pos = y << 3;
                        var tileY = (ushort)(by + y);

                        for (int x = 0; x < 8; ++x, ++pos)
                        {
                            var tileID = (ushort)(cells[pos].TileID & 0x3FFF);
                            var z = cells[pos].Z;
                            var tileX = (ushort)(bx + x);

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
                                var e = world.Entity()
                                    .Set(new TileStretched()
                                    {
                                        NormalTop = normalTop,
                                        NormalRight = normalRight,
                                        NormalBottom = normalBottom,
                                        NormalLeft = normalLeft,
                                        AvgZ = avgZ,
                                        MinZ = minZ,
                                        Offset = offsets
                                    })
                                    .Set(new WorldPosition() { X = tileX, Y = tileY, Z = z })
                                    .Set(new Graphic() { Value = tileID })
                                    .Add<IsTile>()
                                    .Add<Terrain>();

                                chunk.AddChild(e);
                            }
                            else
                            {
                                var e = world.Entity()
                                    .Set(new WorldPosition() { X = tileX, Y = tileY, Z = z })
                                    .Set(new Graphic() { Value = tileID })
                                    .Add<IsTile>()
                                    .Add<Terrain>();

                                chunk.AddChild(e);
                            }
                        }
                    }

                    if (im.StaticAddress != 0)
                    {
                        staticsBlockBuffer.Value ??= new StaticsBlock[im.StaticCount];
                        if (staticsBlockBuffer.Value.Length < im.StaticCount)
                            staticsBlockBuffer.Value = new StaticsBlock[im.StaticCount];

                        var staticsSpan = staticsBlockBuffer.Value.AsSpan(0, (int)im.StaticCount);
                        im.StaticFile.Seek((long)im.StaticAddress, System.IO.SeekOrigin.Begin);
                        im.StaticFile.Read(MemoryMarshal.AsBytes(staticsSpan));

                        foreach (ref readonly var sb in staticsSpan)
                        {
                            if (sb.Color == 0 || sb.Color == 0xFFFF)
                            {
                                continue;
                            }

                            int pos = (sb.Y << 3) + sb.X;

                            if (pos >= 64)
                            {
                                continue;
                            }

                            var staX = (ushort)(bx + sb.X);
                            var staY = (ushort)(by + sb.Y);

                            var e = world.Entity()
                                .Set(new WorldPosition() { X = staX, Y = staY, Z = sb.Z })
                                .Set(new Graphic() { Value = sb.Color })
                                .Set(new Hue() { Value = sb.Hue })
                                .Add<IsStatic>()
                                .Add<Terrain>();

                            chunk.AddChild(e);
                        }
                    }
                }
            }
        }
    }

    private static void RemoveEntitiesOutOfRange
    (
       Commands commands,
       Time time,
       Res<GameContext> gameCtx,
       Res<ChunksLoadedMap> chunksLoaded,
       Res<Camera> camera,
       Local<List<uint>> toRemove,
       Res<MultiCache> multiCache,
       Query<Data<WorldPosition>, Filter<With<NetworkSerial>, Without<IsMulti>, Without<Player>, Without<ContainedInto>>> queryAll,
       Query<Data<WorldPosition, Graphic>, Filter<With<NetworkSerial>, With<IsMulti>, Without<Player>, Without<ContainedInto>>> queryMultis,
       Single<Data<WorldPosition>, With<Player>> playerQuery
    )
    {
        toRemove.Value ??= new();

        (_, var pos) = playerQuery.Get();

        foreach ((var key, (var lastAccess, var entity)) in chunksLoaded.Value)
        {
            if (time.Total > lastAccess)
            {
                toRemove.Value.Add(key);
                commands.Entity(entity).Delete();
            }
        }

        foreach (var p in toRemove.Value)
        {
            chunksLoaded.Value.Remove(p);
        }

        toRemove.Value.Clear();

        foreach ((var entity, var mobPos) in queryAll)
        {
            var dist2 = GetDist(pos.Ref.X, pos.Ref.Y, mobPos.Ref.X, mobPos.Ref.Y);
            if (dist2 > gameCtx.Value.MaxObjectsDistance)
            {
                entity.Ref.Delete();
            }
        }

        foreach ((var entity, var mobPos, var graphic) in queryMultis)
        {
            var bounds = multiCache.Value.GetMulti(graphic.Ref.Value).Bounds;
            var dist2 = GetDist(pos.Ref.X, pos.Ref.Y, mobPos.Ref.X + bounds.X, mobPos.Ref.Y + bounds.Y);
            var dist22 = GetDist(pos.Ref.X, pos.Ref.Y, mobPos.Ref.X + bounds.Width, mobPos.Ref.Y + bounds.Height);

            if (dist2 > gameCtx.Value.MaxObjectsDistance * 2 && dist22 > gameCtx.Value.MaxObjectsDistance * 2)
            {
                entity.Ref.Delete();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDist(int x0, int y0, int x1, int y1)
    {
        return Math.Max(Math.Abs(x0 - x1), Math.Abs(y0 - y1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CreateChunkKey(int x, int y) => (uint)(((x & 0xFFFF) << 16) | (y & 0xFFFF));

    private static bool ApplyStretch(
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
            avgZ = (sbyte)((zTop + zBottom) >> 1);
        }
        else
        {
            avgZ = (sbyte)((zLeft + zRight) >> 1);
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

    private static sbyte GetTileZ(MapLoader mapLoader, int mapIndex, int x, int y)
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


struct TileStretched
{
    public sbyte AvgZ, MinZ;
    public Renderer.UltimaBatcher2D.YOffsets Offset;
    public Vector3 NormalTop, NormalRight, NormalLeft, NormalBottom;
}
