using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Ask the pathfinder to auto-walk the player to a map coordinate (world map
// "Walk To Here", future follow/goto). Bypasses the EnablePathfind gate —
// it's an explicit command, not a double-click gesture.
internal struct PathfindRequest
{
    public int X, Y, Z;
    public int Distance;
}

// Legacy Pathfinder.PathNode. Parent chains point at closed-list copies, so
// path reconstruction survives open-slot reuse.
internal sealed class PathNode
{
    public int X, Y, Z;
    public int Direction;
    public bool Used;
    public int Cost;
    public int DistFromStartCost;
    public int DistFromGoalCost;
    public PathNode Parent;

    public void Reset()
    {
        Parent = null;
        Used = false;
        X = Y = Z = Direction = Cost = DistFromGoalCost = DistFromStartCost = 0;
    }
}

// A* search state + walkability snapshot. Land + statics come straight from
// the map files (lazily, chunk by chunk, as the search explores), so the
// search isn't blind beyond the streamed ECS chunks — only dynamics (mobiles,
// ground items, house multis) are snapshotted from the live world, since
// those exist nowhere else. The search is time-sliced: ProcessSearch expands
// a bounded number of nodes per frame until the goal (or the budget) is hit,
// and only then does the player start walking — long routes are studied in
// full before the first step.
internal sealed class PathfindState
{
    public const int MaxNodes = 200_000;
    // Node expansions per frame while a search is in flight. Each expansion
    // is ~8 walkability probes; this keeps the frame-time hit bounded.
    public const int ExpandPerFrame = 1500;
    // Replans are local re-routing around a live obstacle / from a segment's
    // end — cap them well below the initial route study.
    public const int ReplanMaxClosed = 50_000;

    public readonly PathNode[] Open = new PathNode[MaxNodes];
    public readonly PathNode[] Closed = new PathNode[MaxNodes];
    public readonly PathNode[] Path = new PathNode[MaxNodes];

    // Membership maps replace legacy's full-array scans
    // (DoesNotExistOnOpenList / DoesNotExistOnClosedList).
    public readonly Dictionary<(int x, int y, int z), int> OpenMap = new();
    public readonly Dictionary<(int x, int y, int z), int> ClosedMap = new();
    public readonly Stack<int> FreeOpen = new();
    // Min-heap on Cost with lazy deletion (stale entries skipped on pop) —
    // a linear cheapest-node scan collapses at this node budget. Open slots
    // are never reused mid-search so a stale heap entry can't alias a new
    // node.
    public readonly PriorityQueue<int, int> OpenPq = new();

    // Static world data (map files), filled per chunk on first touch.
    public readonly Dictionary<(int x, int y), List<TerrainInfo>> Grid = new();
    public readonly HashSet<(int cx, int cy)> GridChunks = new();
    // Live-world overlay (mobiles / items / multis), filled eagerly by BuildGrid.
    public readonly Dictionary<(int x, int y), List<TerrainInfo>> Dynamics = new();
    public readonly List<TerrainInfo> Scratch = new();
    public StaticsBlock[] StaticsScratch = new StaticsBlock[64];

    // Lazy-fill context for the current search (set by BuildGrid).
    public MapLoader Maps;
    public TexmapsLoader Texmaps;
    public TileDataLoader TileData;
    public MovementState Move;
    public int GridMap;
    public int GridMinX, GridMaxX, GridMinY, GridMaxY;

    public bool AutoWalking;
    public bool CanBeCancelled;
    public bool Run;
    public int PointIndex;
    public int PathSize;
    public int GoalNode;
    public bool GoalFound;
    public int ClosedCount;
    public int PathfindDistance;
    public Point Start, End;

    // In-flight time-sliced search (ProcessSearch drives it to completion).
    public bool Searching;
    public int CurNode;
    public int MaxClosedThisSearch;

    // Segmented walking: a goal beyond the loaded chunks can't be reached in
    // one search — the snapshot only covers what's spawned. The search then
    // yields a best-effort path to the explored node nearest the goal
    // (Partial), and ProcessAutoWalk re-plans from the segment's end once the
    // player has walked there (new chunks stream in along the way).
    public bool Partial;
    public int TargetX, TargetY, TargetZ, TargetDistance;
    public int SegmentStartX, SegmentStartY;
    // Anti-wander guard: re-plans only keep going while the player keeps
    // getting closer to the target. Three re-plans without a new best
    // distance = the rest of the route is unreachable; stop.
    public int ReplanStrikes;
    public int BestReplanGoalDist;

    public void StopAutoWalk()
    {
        AutoWalking = false;
        Searching = false;
        Run = false;
        PathSize = 0;
    }

    public void ResetSearch()
    {
        OpenMap.Clear();
        ClosedMap.Clear();
        OpenPq.Clear();
        FreeOpen.Clear();
        for (var i = MaxNodes - 1; i >= 0; --i)
            FreeOpen.Push(i);

        ClosedCount = 0;
        GoalNode = 0;
        GoalFound = false;
        PathSize = 0;
        PointIndex = 0;
        CurNode = 0;
    }

    // The walkability snapshot is only needed while a search runs (TryStep
    // live-checks during the walk; replans rebuild it). Explored grids for
    // long routes get big — release them once the path is reconstructed.
    public void ReleaseSnapshot()
    {
        Grid.Clear();
        GridChunks.Clear();
        Dynamics.Clear();
        OpenMap.Clear();
        ClosedMap.Clear();
        OpenPq.Clear();
        FreeOpen.Clear();
    }
}

// Legacy Pathfinder: right double-click on the world A*-walks the player to
// the target, avoiding impassable statics, characters, closed doors (unless
// SmoothDoors / dead / GM) — diagonal steps require both adjacent cardinals
// passable, exactly like a manual step.
internal readonly struct PathfinderPlugin : IPlugin
{
    private static readonly int[] _offsetX = { 0, 1, 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly int[] _offsetY = { -1, -1, 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly sbyte[] _dirOffset = { 1, -1 };

    public void Build(App app)
    {
        var triggerFn = TriggerPathfind;
        var processFn = ProcessAutoWalk;
        var requestsFn = HandlePathfindRequests;
        var searchFn = ProcessSearch;

        app
            .AddResource(new PathfindState())

            .AddSystem((ResMut<PathfindState> state) => state.Value.StopAutoWalk())
            .OnEnter(GameState.GameScreen)
            .Build()

            // Manual movement input cancels an in-flight auto-walk (legacy
            // StopAutoWalk from MoveCharacterByMouseInput — which only ever
            // ran with the cursor over the game scene, so a right-click on a
            // gump must NOT cancel). The second press of the triggering
            // double-click is exempt.
            .AddSystem((
                Res<MouseContext> mouseCtx,
                ResMut<PathfindState> state,
                Res<Camera> camera,
                Res<AssetsServer> assets,
                Query<Data<TinyEcs.Bevy.UI.ComputedNode, TinyEcs.Bevy.UI.Node, TinyEcs.Bevy.UI.UiCustom, TinyEcs.Bevy.UI.BackgroundColor, TinyEcs.Bevy.UI.Text>,
                    Filter<Optional<TinyEcs.Bevy.UI.UiCustom>, Optional<TinyEcs.Bevy.UI.BackgroundColor>, Optional<TinyEcs.Bevy.UI.Text>>> rendered,
                Query<Data<GameScreenPlugin.GameWindowUI>> gameWindowQ,
                Query<Data<TinyEcs.Parent>> parents,
                Query<Data<TinyEcs.Bevy.UI.UiContainsByBounds>> boundsQ) =>
            {
                if (!state.Value.AutoWalking || !state.Value.CanBeCancelled)
                    return;

                if (!mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right) ||
                    mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Right))
                    return;

                // Only a press on the game world is a movement gesture.
                var pos = mouseCtx.Value.Position;
                if (!camera.Value.Bounds.Contains((int)pos.X, (int)pos.Y))
                    return;

                // The viewport node itself is a UI hit (it carries a
                // background) — a hit on IT still counts as "over world";
                // anything else on top is a gump (GameCursorPlugin pattern).
                var hit = UiPick.Topmost(pos, assets.Value, rendered, parents, boundsQ);
                if (hit.Found && !gameWindowQ.Contains(hit.Entity))
                    return;

                state.Value.StopAutoWalk();
            })
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(triggerFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<Profile> profile) => profile.Value.EnablePathfind)
            .RunIf((Res<MouseContext> mouseCtx, Res<Camera> camera) =>
                mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Right) &&
                camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y))
            .Build()

            .AddSystem(requestsFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((EventReader<PathfindRequest> requests) => requests.HasEvents)
            .RunIf((Query<Data<WorldPosition>, With<Player>> q) => q.Count() > 0)
            .Build()

            .AddSystem(searchFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<PathfindState> state) => state.Value.Searching)
            .Build()

            .AddSystem(processFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<PathfindState> state) => state.Value.AutoWalking && !state.Value.Searching)
            .RunIf((Res<PlayerStepsContext> steps, Res<Time> time) =>
                steps.Value.Count < Constants.MAX_STEP_COUNT && steps.Value.LastStep <= time.Value.Total)
            // The animation queue (MobileSteps) silently clamps at COUNT —
            // issuing into a full queue would overwrite the newest step and
            // lose it (player ends short of the path's end). Wait for room.
            .RunIf((Query<Data<MobileSteps>, With<Player>> q) =>
            {
                foreach ((_, var steps) in q)
                    return steps.Ref.Index < MobileSteps.COUNT - 1;
                return false;
            })
            .Build();
    }

    static void TriggerPathfind(
        ResMut<PathfindState> state,
        Res<MovementState> moveState,
        Res<Profile> profile,
        Res<KeyboardContext> keyboard,
        Res<SelectedEntity> selectedEntity,
        Res<UOFileManager> fileManager,
        Res<MultiCache> multiCache,
        Res<GameContext> gameCtx,
        EventWriter<TextOverheadEvent> overhead,
        Res<Time> time,
        Query<Data<WorldPosition>> posQuery,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Single<Data<WorldPosition>, With<Player>> playerQuery)
    {
        if (state.Value.AutoWalking)
            return;

        if (profile.Value.UseShiftToPathfind &&
            !keyboard.Value.IsPressed(Keys.LeftShift) && !keyboard.Value.IsPressed(Keys.RightShift))
            return;

        // Legacy WalkTo bails when paralyzed.
        if (moveState.Value.IsParalyzed)
            return;

        if (!posQuery.TryGet(selectedEntity.Value.Entity, out var targetRow))
            return;

        (_, var targetPos) = targetRow;
        int targetX = targetPos.Ref.X;
        int targetY = targetPos.Ref.Y;
        int targetZ = targetPos.Ref.Z;

        (var playerEnt, var playerPos) = playerQuery.Get();

        StartPathfind(
            state.Value, moveState.Value, fileManager.Value, multiCache.Value,
            qHouseRevision, othersQuery, gameCtx.Value.Map,
            playerPos.Ref.X, playerPos.Ref.Y, playerPos.Ref.Z,
            targetX, targetY, targetZ, 0,
            overhead, gameCtx.Value.PlayerSerial, time.Value.Total);
    }

    // Explicit walk-to commands (world map context menu, future goto/follow).
    static void HandlePathfindRequests(
        ResMut<PathfindState> state,
        Res<MovementState> moveState,
        Res<UOFileManager> fileManager,
        Res<MultiCache> multiCache,
        Res<GameContext> gameCtx,
        Res<Time> time,
        EventWriter<TextOverheadEvent> overhead,
        EventReader<PathfindRequest> requests,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Single<Data<WorldPosition>, With<Player>> playerQuery)
    {
        if (moveState.Value.IsParalyzed)
            return;

        (_, var playerPos) = playerQuery.Get();

        foreach (var request in requests.Read())
        {
            StartPathfind(
                state.Value, moveState.Value, fileManager.Value, multiCache.Value,
                qHouseRevision, othersQuery, gameCtx.Value.Map,
                playerPos.Ref.X, playerPos.Ref.Y, playerPos.Ref.Z,
                request.X, request.Y, request.Z, request.Distance,
                overhead, gameCtx.Value.PlayerSerial, time.Value.Total);
        }
    }

    private static void StartPathfind(
        PathfindState state,
        MovementState moveState,
        UOFileManager fileManager,
        MultiCache multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        int mapIndex,
        int playerX, int playerY, sbyte playerZ,
        int targetX, int targetY, int targetZ, int distance,
        EventWriter<TextOverheadEvent> overhead,
        uint playerSerial,
        float timeTotal)
    {
        state.ReplanStrikes = 0;
        state.BestReplanGoalDist = int.MaxValue;

        BuildGrid(
            state, moveState, fileManager, multiCache,
            qHouseRevision, othersQuery,
            mapIndex, playerX, playerY, targetX, targetY);

        var started = WalkTo(
            state, moveState,
            targetX, targetY, targetZ, distance,
            playerX, playerY, playerZ);

        if (started)
        {
            // Legacy "Pathfinding" label over the player (font 3, ascii).
            overhead.Send(new TextOverheadEvent
            {
                Serial = playerSerial,
                Text = "Pathfinding",
                Name = string.Empty,
                Hue = 0,
                Font = 3,
                IsUnicode = false,
                MessageType = MessageType.Label,
                Time = timeTotal
            });
        }
    }

    static void ProcessAutoWalk(
        ResMut<PathfindState> state,
        Res<MovementState> moveState,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        Res<Time> time,
        Res<NetClient> network,
        Res<Profile> profile,
        Res<GameContext> gameCtx,
        Res<UOFileManager> fileManager,
        Res<TerrainPlugin.ChunksLoadedMap> chunksLoaded,
        Res<MultiCache> multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tilesQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        Single<Data<WorldPosition, Facing, MobileSteps, MobAnimation, ServerFlags>, With<Player>> playerQuery)
    {
        // Re-plan toward the final target from the player's end position.
        bool Replan()
        {
            (var endPos, _, var endSteps, _, _) = playerQuery.Get();
            int sx = endPos.Ref.X, sy = endPos.Ref.Y;
            var sz = endPos.Ref.Z;
            if (endSteps.Ref.Index >= 0)
            {
                ref var lastStep = ref endSteps.Ref.CurrentStep();
                sx = lastStep.X;
                sy = lastStep.Y;
                sz = lastStep.Z;
            }

            var goalDist = Math.Max(Math.Abs(sx - state.Value.TargetX), Math.Abs(sy - state.Value.TargetY));
            if (goalDist < state.Value.BestReplanGoalDist)
            {
                state.Value.BestReplanGoalDist = goalDist;
                state.Value.ReplanStrikes = 0;
            }
            else if (++state.Value.ReplanStrikes > 3)
            {
                return false;
            }

            BuildGrid(
                state.Value, moveState.Value, fileManager.Value, multiCache.Value,
                qHouseRevision, othersQuery,
                gameCtx.Value.Map, sx, sy, state.Value.TargetX, state.Value.TargetY);

            return WalkTo(
                state.Value, moveState.Value,
                state.Value.TargetX, state.Value.TargetY, state.Value.TargetZ, state.Value.TargetDistance,
                sx, sy, sz,
                PathfindState.ReplanMaxClosed);
        }

        if (state.Value.PointIndex < 0 || state.Value.PointIndex >= state.Value.PathSize)
        {
            // Segment exhausted. A partial path means the real goal is still
            // ahead — re-plan from the segment's end (the walk streamed new
            // chunks in).
            if (!state.Value.Partial || !Replan())
                state.Value.StopAutoWalk();
            return;
        }

        var p = state.Value.Path[state.Value.PointIndex];

        // End direction: last queued step wins (legacy GetEndPosition).
        (_, var dir, var mobSteps, _, _) = playerQuery.Get();
        var endDir = mobSteps.Ref.Index >= 0
            ? (Direction)mobSteps.Ref.CurrentStep().Direction
            : dir.Ref.Value;
        endDir &= Direction.Mask;

        if (endDir == (Direction)p.Direction)
        {
            state.Value.PointIndex++;
        }

        var stepped = PlayerMovementPlugin.TryStep(
            (Direction)p.Direction,
            state.Value.Run,
            moveState.Value,
            chunksLoaded.Value,
            multiCache.Value,
            qHouseRevision,
            childrenQuery,
            othersQuery,
            tilesQuery,
            staticsQuery,
            fileManager.Value.TileData,
            network.Value,
            playerRequestedSteps,
            time.Value.Total,
            profile.Value,
            playerQuery
        );

        if (!stepped)
        {
            // The live world disagrees with the snapshot (a mobile moved in,
            // a door closed). Re-plan around it; the strike guard stops us
            // once re-routing stops making progress.
            if (!Replan())
                state.Value.StopAutoWalk();
        }
    }

    // ------------------------------------------------------------------
    // Snapshot grid
    // ------------------------------------------------------------------

    private static void BuildGrid(
        PathfindState state,
        MovementState moveState,
        UOFileManager fileManager,
        MultiCache multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        int mapIndex, int startX, int startY, int endX, int endY)
    {
        // Bound the search to the start↔end corridor (+margin for detours).
        // Land/statics inside the box come from the map files on demand, so
        // the box can span well past the streamed chunks. Long routes need
        // proportionally wider detour room (coastlines, mountain ranges) —
        // lazy fill means a wide box only costs what the search explores.
        var span = Math.Max(Math.Abs(startX - endX), Math.Abs(startY - endY));
        var margin = Math.Clamp(span / 4, 64, 768);
        state.GridMinX = Math.Min(startX, endX) - margin;
        state.GridMaxX = Math.Max(startX, endX) + margin;
        state.GridMinY = Math.Min(startY, endY) - margin;
        state.GridMaxY = Math.Max(startY, endY) + margin;

        state.Grid.Clear();
        state.GridChunks.Clear();
        state.Dynamics.Clear();
        state.Maps = fileManager.Maps;
        state.Texmaps = fileManager.Texmaps;
        state.TileData = fileManager.TileData;
        state.Move = moveState;
        state.GridMap = mapIndex;

        var tileData = fileManager.TileData;

        // Dynamics only exist as live entities — snapshot them eagerly.
        foreach ((var ent, var pos, var graphic, var anim) in othersQuery)
        {
            int x = pos.Ref.X, y = pos.Ref.Y;
            if (x < state.GridMinX || x > state.GridMaxX || y < state.GridMinY || y > state.GridMaxY)
                continue;

            if (anim.IsValid())
            {
                if (!moveState.IgnoreCharacters && !Walkability.IsDeadBody(graphic.Ref.Value))
                    Bucket(state.Dynamics, x, y).Add(Walkability.MakeMobile(pos.Ref.Z));
                continue;
            }

            var graphicValue = graphic.Ref.Value;
            if (qHouseRevision.Contains(ent.Ref))
            {
                var multiInfo = multiCache.GetMulti(graphicValue);
                if (multiInfo.Id != 0)
                    graphicValue = multiInfo.Id;
            }

            ref var staticData = ref tileData.StaticData[graphicValue];
            var flags = Walkability.StaticFlags(graphicValue, ref staticData, moveState.StepState, moveState.SmoothDoors, moveState.IsGm);

            if (flags != 0)
                Bucket(state.Dynamics, x, y).Add(Walkability.MakeStatic(flags, pos.Ref.Z, in staticData));
        }
    }

    private static List<TerrainInfo> Bucket(Dictionary<(int x, int y), List<TerrainInfo>> grid, int x, int y)
    {
        if (!grid.TryGetValue((x, y), out var list))
        {
            list = new List<TerrainInfo>(4);
            grid[(x, y)] = list;
        }
        return list;
    }

    // Read one map chunk's land + statics out of the UO files into the grid —
    // same data TerrainPlugin.LoadChunks spawns as entities, minus rendering.
    private static void FillChunkFromFiles(PathfindState state, int chunkX, int chunkY)
    {
        ref var im = ref state.Maps.GetIndex(state.GridMap, chunkX, chunkY);
        if (!im.IsValid())
            return;

        im.MapFile.Seek((long)im.MapAddress, System.IO.SeekOrigin.Begin);
        var cells = im.MapFile.Read<MapBlock>().Cells;

        var bx = chunkX << 3;
        var by = chunkY << 3;

        for (var y = 0; y < 8; ++y)
        {
            var pos = y << 3;
            for (var x = 0; x < 8; ++x, ++pos)
            {
                var tileID = (ushort)(cells[pos].TileID & 0x3FFF);
                var z = cells[pos].Z;

                if (!Walkability.LandPasses(tileID))
                    continue;

                ref var landData = ref state.TileData.LandData[tileID];
                var flags = Walkability.LandFlags(in landData, state.Move.StepState);

                var isStretched = landData.TexID == 0 && landData.IsWet;
                isStretched = TerrainPlugin.ApplyStretch(
                    state.Maps, state.Texmaps, state.GridMap, landData.TexID,
                    bx + x, by + y, z, isStretched,
                    out var avgZ, out var minZ, out var offsets,
                    out _, out _, out _, out _);

                var stretched = isStretched
                    ? new TileStretched { AvgZ = avgZ, MinZ = minZ, Offset = offsets }
                    : default;
                Bucket(state.Grid, bx + x, by + y).Add(Walkability.MakeLand(flags, z, isStretched, in stretched));
            }
        }

        if (im.StaticAddress != 0 && im.StaticCount > 0)
        {
            if (state.StaticsScratch.Length < im.StaticCount)
                state.StaticsScratch = new StaticsBlock[im.StaticCount];

            var staticsSpan = state.StaticsScratch.AsSpan(0, (int)im.StaticCount);
            im.StaticFile.Seek((long)im.StaticAddress, System.IO.SeekOrigin.Begin);
            im.StaticFile.Read(System.Runtime.InteropServices.MemoryMarshal.AsBytes(staticsSpan));

            foreach (ref readonly var sb in staticsSpan)
            {
                if (sb.Color == 0 || sb.Color == 0xFFFF)
                    continue;

                if ((sb.Y << 3) + sb.X >= 64)
                    continue;

                ref var staticData = ref state.TileData.StaticData[sb.Color];
                var flags = Walkability.StaticFlags(sb.Color, in staticData, state.Move.StepState, state.Move.SmoothDoors, state.Move.IsGm);

                if (flags != 0)
                    Bucket(state.Grid, bx + sb.X, by + sb.Y).Add(Walkability.MakeStatic(flags, sb.Z, in staticData));
            }
        }
    }

    private static void FillFromGrid(PathfindState state, int x, int y)
    {
        state.Scratch.Clear();
        if (x < state.GridMinX || x > state.GridMaxX || y < state.GridMinY || y > state.GridMaxY)
            return;

        if (state.GridChunks.Add((x >> 3, y >> 3)))
            FillChunkFromFiles(state, x >> 3, y >> 3);

        if (state.Grid.TryGetValue((x, y), out var list))
            state.Scratch.AddRange(list);
        if (state.Dynamics.TryGetValue((x, y), out var dyn))
            state.Scratch.AddRange(dyn);
    }

    // Legacy CalculateNewZ against the snapshot.
    private static bool CalculateNewZ(PathfindState state, MovementState moveState, int x, int y, ref sbyte z, int direction)
    {
        var dirIndex = direction & 7;
        var back = dirIndex ^ 4;
        FillFromGrid(state, x + _offsetX[back], y + _offsetY[back]);
        Walkability.GetMinMaxZCore(state.Scratch, dirIndex, z, out var minZ, out var maxZ);

        FillFromGrid(state, x, y);
        return Walkability.CalculateNewZCore(state.Scratch, moveState.StepState, minZ, maxZ, ref z);
    }

    // Legacy CanWalk: a diagonal passes only when both adjacent cardinals
    // pass; a blocked diagonal degrades into the nearest passable cardinal
    // (direction change).
    private static bool CanWalk(PathfindState state, MovementState moveState, ref Direction direction, ref int x, ref int y, ref sbyte z)
    {
        var newX = x;
        var newY = y;
        var newZ = z;
        var newDirection = (byte)direction;
        GetNewXY((byte)direction, ref newX, ref newY);
        var passed = CalculateNewZ(state, moveState, newX, newY, ref newZ, (byte)direction);

        if ((sbyte)direction % 2 != 0)
        {
            if (passed)
            {
                for (var i = 0; i < 2 && passed; i++)
                {
                    var testX = x;
                    var testY = y;
                    var testZ = z;
                    var testDir = (byte)(((byte)direction + _dirOffset[i]) % 8);
                    GetNewXY(testDir, ref testX, ref testY);
                    passed = CalculateNewZ(state, moveState, testX, testY, ref testZ, testDir);
                }
            }

            if (!passed)
            {
                for (var i = 0; i < 2 && !passed; i++)
                {
                    newX = x;
                    newY = y;
                    newZ = z;
                    newDirection = (byte)(((byte)direction + _dirOffset[i]) % 8);
                    GetNewXY(newDirection, ref newX, ref newY);
                    passed = CalculateNewZ(state, moveState, newX, newY, ref newZ, newDirection);
                }
            }
        }

        if (passed)
        {
            x = newX;
            y = newY;
            z = newZ;
            direction = (Direction)newDirection;
        }

        return passed;
    }

    private static void GetNewXY(byte direction, ref int x, ref int y)
    {
        Walkability.GetNewXY((Direction)(direction & 7), out var ox, out var oy);
        x += ox;
        y += oy;
    }

    // ------------------------------------------------------------------
    // A*
    // ------------------------------------------------------------------

    // Manhattan, not legacy's Chebyshev: step costs are 1 straight / 2
    // diagonal, so the true remaining cost is the Manhattan distance.
    // Chebyshev halves the estimate on diagonals, degrading A* toward
    // Dijkstra — fatal at long range. Manhattan stays admissible and keeps
    // the explored set hugging the corridor. (Goal detection still uses
    // Chebyshev against PathfindDistance, matching legacy.)
    private static int GetGoalDistCost(PathfindState state, int x, int y)
        => Math.Abs(state.End.X - x) + Math.Abs(state.End.Y - y);

    private static int AddToOpenList(PathfindState state, int direction, int x, int y, int z, PathNode parent, int cost)
    {
        var key = (x, y, z);

        // Already expanded — legacy returns 0 here (treated as success by
        // OpenNodes), not -1.
        if (state.ClosedMap.ContainsKey(key))
            return 0;

        if (state.OpenMap.TryGetValue(key, out var existing))
        {
            var node = state.Open[existing];
            var startCost = parent.DistFromStartCost + cost;

            if (node.DistFromStartCost > startCost)
            {
                node.Parent = parent;
                // Legacy adds `cost` twice on this relaxation
                // (DistFromStartCost = startCost + cost) — fixed so the
                // reconstructed path stays optimal.
                node.DistFromStartCost = startCost;
                node.Cost = node.DistFromGoalCost + node.DistFromStartCost;
                // Duplicate heap entry; the old one goes stale and is
                // skipped on pop.
                state.OpenPq.Enqueue(existing, node.Cost);
            }

            return existing;
        }

        if (state.FreeOpen.Count == 0)
            return -1;

        var i = state.FreeOpen.Pop();
        var slot = state.Open[i] ??= new PathNode();
        slot.Used = true;
        slot.Direction = direction;
        slot.X = x;
        slot.Y = y;
        slot.Z = z;
        slot.DistFromGoalCost = GetGoalDistCost(state, x, y);
        slot.DistFromStartCost = parent.DistFromStartCost + cost;
        slot.Cost = slot.DistFromGoalCost + slot.DistFromStartCost;
        slot.Parent = parent;

        state.OpenMap[key] = i;
        state.OpenPq.Enqueue(i, slot.Cost);

        if (Math.Max(Math.Abs(state.End.X - x), Math.Abs(state.End.Y - y)) <= state.PathfindDistance)
        {
            state.GoalFound = true;
            state.GoalNode = i;
        }

        return i;
    }

    private static bool OpenNodes(PathfindState state, MovementState moveState, PathNode node)
    {
        var found = false;

        for (var i = 0; i < 8; i++)
        {
            var direction = (Direction)i;
            var x = node.X;
            var y = node.Y;
            var z = (sbyte)node.Z;
            var oldDirection = direction;

            if (CanWalk(state, moveState, ref direction, ref x, ref y, ref z))
            {
                // A degraded diagonal is not this direction's move.
                if (direction != oldDirection)
                    continue;

                var diagonal = i % 2;

                if (diagonal != 0)
                {
                    var wantX = node.X;
                    var wantY = node.Y;
                    GetNewXY((byte)i, ref wantX, ref wantY);

                    if (x != wantX || y != wantY)
                        diagonal = -1;
                }

                if (diagonal >= 0 && AddToOpenList(state, (int)direction, x, y, z, node, diagonal == 0 ? 1 : 2) != -1)
                    found = true;
            }
        }

        return found;
    }

    // Pop the cheapest open node into the closed list; returns its closed
    // index (legacy FindCheapestNode + AddNodeToList(list=1)). Open slots are
    // deliberately NOT returned to FreeOpen — a stale heap entry must keep
    // pointing at the node it was pushed for.
    private static int PopCheapest(PathfindState state)
    {
        while (state.OpenPq.TryDequeue(out var idx, out _))
        {
            var open = state.Open[idx];
            if (!open.Used)
                continue; // stale entry (already closed / relaxed duplicate)

            open.Used = false;
            state.OpenMap.Remove((open.X, open.Y, open.Z));

            if (state.ClosedCount >= PathfindState.MaxNodes)
                return -1;

            var ci = state.ClosedCount++;
            var closed = state.Closed[ci] ??= new PathNode();
            closed.Used = true;
            closed.DistFromGoalCost = open.DistFromGoalCost;
            closed.DistFromStartCost = open.DistFromStartCost;
            closed.Cost = closed.DistFromGoalCost + closed.DistFromStartCost;
            closed.Direction = open.Direction;
            closed.X = open.X;
            closed.Y = open.Y;
            closed.Z = open.Z;
            closed.Parent = open.Parent;
            state.ClosedMap[(open.X, open.Y, open.Z)] = ci;

            return ci;
        }

        return -1;
    }

    private static void SeedSearch(PathfindState state, sbyte playerZ)
    {
        var c0 = state.Closed[0] ??= new PathNode();
        c0.Reset();
        c0.Used = true;
        c0.X = state.Start.X;
        c0.Y = state.Start.Y;
        c0.Z = playerZ;
        c0.Parent = null;
        c0.DistFromGoalCost = GetGoalDistCost(state, state.Start.X, state.Start.Y);
        c0.Cost = c0.DistFromGoalCost;
        state.ClosedCount = 1;
        state.ClosedMap[(c0.X, c0.Y, c0.Z)] = 0;
        state.CurNode = 0;

        if (c0.DistFromGoalCost > 14)
            state.Run = true;
    }

    // One time-slice of the search. true = goal found, false = exhausted
    // (budget or frontier), null = budget for this frame spent, still going.
    private static bool? StepSearch(PathfindState state, MovementState moveState, int expansions)
    {
        while (expansions-- > 0)
        {
            OpenNodes(state, moveState, state.Closed[state.CurNode]);

            if (state.GoalFound)
                return true;

            var next = PopCheapest(state);

            if (next == -1)
                return false;

            state.CurNode = next;

            if (state.ClosedCount >= state.MaxClosedThisSearch)
                return false;
        }

        return null;
    }

    private static void FinishSearch(PathfindState state, bool goalFound)
    {
        state.Searching = false;

        if (goalFound)
        {
            ReconstructPath(state, state.Open[state.GoalNode]);
            state.Partial = false;
            state.PointIndex = 1;
            state.ReleaseSnapshot();
            return;
        }

        // Goal unreachable within the budget. Walk toward it: best explored
        // node by goal distance, then re-plan from there (ProcessAutoWalk).
        var c0 = state.Closed[0];
        PathNode best = null;
        var bestGoal = c0.DistFromGoalCost; // must actually get closer than start

        for (var i = 1; i < state.ClosedCount; i++)
        {
            var n = state.Closed[i];
            if (n.DistFromGoalCost < bestGoal ||
                (best != null && n.DistFromGoalCost == bestGoal && n.DistFromStartCost < best.DistFromStartCost))
            {
                best = n;
                bestGoal = n.DistFromGoalCost;
            }
        }

        // The open frontier is usually the closest of all — include it.
        foreach (var idx in state.OpenMap.Values)
        {
            var n = state.Open[idx];
            if (!n.Used)
                continue;
            if (n.DistFromGoalCost < bestGoal ||
                (best != null && n.DistFromGoalCost == bestGoal && n.DistFromStartCost < best.DistFromStartCost))
            {
                best = n;
                bestGoal = n.DistFromGoalCost;
            }
        }

        if (best == null)
        {
            state.StopAutoWalk();
            state.ReleaseSnapshot();
            return;
        }

        ReconstructPath(state, best);
        state.Partial = true;
        state.PointIndex = 1;
        state.ReleaseSnapshot();
    }

    private static void ReconstructPath(PathfindState state, PathNode endNode)
    {
        var totalNodes = 0;
        var node = endNode;

        while (node.Parent != null && node != node.Parent)
        {
            node = node.Parent;
            totalNodes++;
        }

        totalNodes++;
        state.PathSize = totalNodes;
        node = endNode;

        while (totalNodes != 0)
        {
            totalNodes--;
            state.Path[totalNodes] = node;
            node = node.Parent;
        }
    }

    private static bool IsBlocked(PathfindState state, MovementState moveState, int x, int y, int z)
    {
        var tempZ = (sbyte)z;
        return !CalculateNewZ(state, moveState, x, y, ref tempZ, (byte)Direction.North);
    }

    // Legacy Pathfinder.WalkTo, made asynchronous: seeds the search and lets
    // ProcessSearch run it to completion over the next frames. The player
    // stands still (with the "Pathfinding" label up) until the route is
    // known. Grid must be built first.
    private static bool WalkTo(
        PathfindState state, MovementState moveState,
        int x, int y, int z, int distance,
        int playerX, int playerY, sbyte playerZ,
        int maxClosed = PathfindState.MaxNodes)
    {
        state.ResetSearch();
        state.Start = new Point(playerX, playerY);
        state.End = new Point(x, y);
        state.PathfindDistance = distance;
        state.TargetX = x;
        state.TargetY = y;
        state.TargetZ = z;
        state.TargetDistance = distance;
        state.SegmentStartX = playerX;
        state.SegmentStartY = playerY;
        state.Partial = false;

        // Unreachable final tile (tree, chest, rock): walk next to it
        // instead of exhausting the node budget.
        if (distance == 0 && IsBlocked(state, moveState, x, y, z))
        {
            state.PathfindDistance = 1;
        }

        state.StopAutoWalk();
        state.CanBeCancelled = true;
        state.AutoWalking = true;
        state.Run = false;
        state.MaxClosedThisSearch = maxClosed;

        SeedSearch(state, playerZ);
        state.Searching = true;

        return true;
    }

    // Drives an in-flight search a bounded slice per frame; finalizes the
    // path (or gives up) when the search ends.
    static void ProcessSearch(ResMut<PathfindState> state, Res<MovementState> moveState)
    {
        var result = StepSearch(state.Value, moveState.Value, PathfindState.ExpandPerFrame);

        if (result is null)
            return;

        FinishSearch(state.Value, result.Value);
    }
}
