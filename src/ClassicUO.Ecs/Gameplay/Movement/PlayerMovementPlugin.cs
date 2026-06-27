using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Ecs.Logging;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

struct PlayerStep
{
    public byte Sequence;
    public Direction Direction;
    public ushort X, Y;
    public sbyte Z;
}

[InlineArray(5)]
struct PlayerStepsArray
{
    private PlayerStep _a;
}

struct PlayerStepsContext
{
    public PlayerStepsArray Steps;
    public float LastStep;
    public int Count;
    public byte Sequence;
    public bool ResyncSent;
}

struct RejectedStep
{
    public byte Sequence;
    public Direction Direction;
    public ushort X, Y;
    public sbyte Z;
}

struct AcceptedStep
{
    public byte Sequence;
    public NotorietyFlag Notoriety;
}

readonly struct PlayerMovementPlugin : IPlugin
{
    public void Build(App app)
    {
        var computeMovementStateFn = ComputeMovementState;
        var enqueuePlayerStepsFn = EnqueuePlayerSteps;
        var enqueueKeyboardStepsFn = EnqueueKeyboardSteps;
        var parseAcceptedStepsFn = ParseAcceptedSteps;
        var parseDeniedStepsFn = ParseDeniedSteps;

        app
            .AddResource(new PlayerStepsContext())
            .AddResource(new MovementState())

            .AddSystem((ResMut<PlayerStepsContext> playerRequestedSteps) =>
            {
                playerRequestedSteps.Value = new PlayerStepsContext();
            })
            .OnEnter(GameState.GameScreen)
            .Build()

            // Step state (dead/GM/sea-horse/flying) + character-blocking rule,
            // shared by manual walking and the pathfinder.
            .AddSystem(computeMovementStateFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Query<Data<Graphic>, With<Player>> q) => q.Count() > 0)
            .Build()

            .AddSystem(enqueuePlayerStepsFn)
            .InStage(Stage.Update)
            .RunIf((Res<MouseContext> mouseCtx, Res<Camera> camera, Local<(float X, float Y)> clickedPos) =>
            {
                if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right))
                {
                    clickedPos.Value = (mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y);
                }

                return camera.Value.Bounds.Contains((int)clickedPos.Value.X, (int)clickedPos.Value.Y);
            })
            .RunIf((
                       Res<MouseContext> mouseCtx,
                       Res<PlayerStepsContext> playerRequestedSteps,
                       Local<bool> autoWalk,
                       Res<Time> time,
                       Res<Profile> profile,
                       Query<Data<WorldPosition, Facing, MobileSteps, MobAnimation>, With<Player>> playerQuery
                   ) =>
                   {
                       if (!autoWalk.Value)
                       {
                           if (!profile.Value.DisableAutoMove &&
                               mouseCtx.Value.IsPressed(Input.MouseButtonType.Right) &&
                               mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                           {
                               autoWalk.Value = true;
                           }
                       }
                       else if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right))
                       {
                           autoWalk.Value = false;
                       }

                       return (autoWalk.Value || mouseCtx.Value.IsPressed(Input.MouseButtonType.Right)) &&
                              playerRequestedSteps.Value.LastStep < time.Value.Total && playerRequestedSteps.Value.Count < 5 &&
                              playerQuery.Count() > 0;
                   })
            // MobileSteps clamps at COUNT and overwrites the newest step when
            // full — don't issue into a full animation queue (see PathfinderPlugin).
            .RunIf((Query<Data<MobileSteps>, With<Player>> q) =>
            {
                foreach ((_, var steps) in q)
                    return steps.Ref.Index < MobileSteps.COUNT - 1;
                return false;
            })
            .Build()

            // Keyboard movement: the four walk hotkeys (default arrows). Held-key
            // sibling of the mouse walk above — same TryStep engine + queue guards,
            // facing from the held-key combo. Suspended while a hotkey is being
            // recorded so binding an arrow doesn't also walk.
            .AddSystem(enqueueKeyboardStepsFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<HotkeyCapture> cap) => cap.Value.Index < 0)
            .RunIf((Res<KeyboardContext> kb, Res<Profile> profile) => AnyWalkHeld(profile.Value, kb.Value))
            .RunIf((Res<PlayerStepsContext> playerRequestedSteps, Res<Time> time, Query<Data<MobileSteps>, With<Player>> q) =>
                playerRequestedSteps.Value.LastStep < time.Value.Total && playerRequestedSteps.Value.Count < 5 && q.Count() > 0)
            .RunIf((Query<Data<MobileSteps>, With<Player>> q) =>
            {
                foreach ((_, var steps) in q)
                    return steps.Ref.Index < MobileSteps.COUNT - 1;
                return false;
            })
            .Build()

            .AddSystem(parseAcceptedStepsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<AcceptedStep> responses) => responses.HasEvents)
            .Build()

            .AddSystem(parseDeniedStepsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<RejectedStep> responses) => responses.HasEvents)
            .Build();

        // Per-packet observers replace the boxed EventReader<IPacket> scan.
        // 0x21 deny / 0x22 confirm fan into the dedicated event writers so
        // the accept/reject systems above still consume them in-order.
        app.AddObserver((
            On<PacketReceived<OnDenyWalkPacket_0x21>> trig,
            EventWriter<RejectedStep> rejectedSteps) =>
        {
            var deny = trig.Event.Packet;
            rejectedSteps.Send(new RejectedStep
            {
                Sequence = deny.Sequence,
                Direction = deny.Direction,
                X = deny.X,
                Y = deny.Y,
                Z = deny.Z
            });
        });

        app.AddObserver((
            On<PacketReceived<OnConfirmWalkPacket_0x22>> trig,
            EventWriter<AcceptedStep> acceptedSteps) =>
        {
            var confirm = trig.Event.Packet;
            acceptedSteps.Send(new AcceptedStep
            {
                Sequence = confirm.Sequence,
                Notoriety = confirm.Notoriety
            });
        });
    }

    // Legacy CalculateNewZ preamble + CreateItemList ignoreGameCharacters:
    // derive how the player steps this frame from its body/flags/mount.
    static void ComputeMovementState(
        ResMut<MovementState> moveState,
        Res<Profile> profile,
        Res<GameContext> gameCtx,
        Query<Data<Graphic, Hue>> qLayers,
        Single<Data<Graphic, ServerFlags, Stamina, EquipmentSlots, PlayerData>,
            Filter<With<Player>, Optional<ServerFlags>, Optional<Stamina>, Optional<EquipmentSlots>, Optional<PlayerData>>> playerQuery)
    {
        (var gfx, var flags, var stamina, var slots, var playerData) = playerQuery.Get();

        var graphic = gfx.Ref.Value;
        var isGm = graphic == 0x03DB;
        var isDead = Walkability.IsDeadBody(graphic);
        var serverFlags = flags.IsValid() ? flags.Ref.Value : Flags.None;

        var stepState = PathStepState.Normal;
        if (isDead || isGm)
        {
            stepState = PathStepState.DeadOrGm;
        }
        else if (playerData.IsValid() && playerData.Ref.Race == RaceType.GARGOYLE && serverFlags.HasFlag(Flags.Flying))
        {
            stepState = PathStepState.Flying;
        }
        else if (slots.IsValid())
        {
            var mount = slots.Ref[Layer.Mount];
            if (mount.IsValid() && qLayers.TryGet(mount, out var mountRow))
            {
                (var mountGfx, _) = mountRow;
                if (mountGfx.Ref.Value == 0x3EB3) // sea horse
                    stepState = PathStepState.OnSeaHorse;
            }
        }

        var staminaFull = !stamina.IsValid() || stamina.Ref.Value >= stamina.Ref.MaxValue;

        moveState.Value.StepState = stepState;
        moveState.Value.IsGm = isGm;
        moveState.Value.SmoothDoors = profile.Value.SmoothDoors;
        moveState.Value.IsParalyzed = serverFlags.HasFlag(Flags.Frozen);
        // Legacy: characters only block on Felucca with depleted stamina.
        moveState.Value.IgnoreCharacters =
            profile.Value.IgnoreStaminaCheck ||
            stepState == PathStepState.DeadOrGm ||
            !(!staminaFull && gameCtx.Value.Map == 0);
    }

    static void EnqueuePlayerSteps(
        Res<MovementState> moveState,
        Res<UOFileManager> fileManager,
        Res<MouseContext> mouseCtx,
        Res<NetClient> network,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        Res<Camera> camera,
        Res<TerrainPlugin.ChunksLoadedMap> chunksLoaded,
        Res<MultiCache> multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Single<Data<WorldPosition, Facing, MobileSteps, MobAnimation, ServerFlags>, With<Player>> playerQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tilesQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        Res<Time> time,
        Res<Profile> profile
    )
    {
        var mousePos = mouseCtx.Value.Position;

        var center = new Vector2(camera.Value.Bounds.Right, camera.Value.Bounds.Bottom);
        center.X -= camera.Value.Bounds.Width / 2f;
        center.Y -= camera.Value.Bounds.Height / 2f;

        var mouseDir = (Direction)ClassicUO.Game.GameCursor.GetMouseDirection((int)center.X, (int)center.Y, (int)mousePos.X, (int)mousePos.Y, 1);
        var mouseRange = Utility.MathHelper.Hypotenuse(center.X - mousePos.X, center.Y - mousePos.Y);
        var facing = mouseDir == Direction.North ? Direction.Mask : mouseDir - 1;
        var run = mouseRange >= Constants.THREESHOLD_MOUSE_WALK_RUN || profile.Value.AlwaysRun;

        TryStep(
            facing,
            run,
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
    }

    // Keyboard walk: facing from the held walk-hotkey combo (legacy
    // GameScene arrow movement via DirectionFromKeyboardArrows). Same TryStep
    // engine + queue guards as the mouse path; run flag from AlwaysRun.
    static void EnqueueKeyboardSteps(
        Res<MovementState> moveState,
        Res<UOFileManager> fileManager,
        Res<KeyboardContext> keyboard,
        Res<NetClient> network,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        Res<TerrainPlugin.ChunksLoadedMap> chunksLoaded,
        Res<MultiCache> multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Single<Data<WorldPosition, Facing, MobileSteps, MobAnimation, ServerFlags>, With<Player>> playerQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tilesQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        Res<Time> time,
        Res<Profile> profile
    )
    {
        bool up = WalkHeld(profile.Value, keyboard.Value, HotkeyAction.WalkNorth);
        bool down = WalkHeld(profile.Value, keyboard.Value, HotkeyAction.WalkSouth);
        bool right = WalkHeld(profile.Value, keyboard.Value, HotkeyAction.WalkEast);
        bool left = WalkHeld(profile.Value, keyboard.Value, HotkeyAction.WalkWest);

        var facing = ClassicUO.Game.Data.DirectionHelper.DirectionFromKeyboardArrows(up, down, left, right);
        if (facing == Direction.NONE)
            return;

        TryStep(
            facing,
            profile.Value.AlwaysRun,
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
    }

    private static bool WalkHeld(Profile profile, KeyboardContext kb, HotkeyAction action)
    {
        var list = profile.Hotkeys;
        if (list == null)
            return false;
        foreach (var hk in list)
        {
            if (hk == null || hk.Action != action || hk.Key == 0)
                continue;
            var k = (Microsoft.Xna.Framework.Input.Keys)hk.Key;
            if (kb.IsPressed(k) || kb.IsPressedOnce(k))
                return true;
        }
        return false;
    }

    private static bool AnyWalkHeld(Profile profile, KeyboardContext kb)
        => WalkHeld(profile, kb, HotkeyAction.WalkNorth)
        || WalkHeld(profile, kb, HotkeyAction.WalkSouth)
        || WalkHeld(profile, kb, HotkeyAction.WalkEast)
        || WalkHeld(profile, kb, HotkeyAction.WalkWest);

    // One walk attempt toward `facing` from the player's end position:
    // walkability + diagonal rules + turn handling + step enqueue + walk
    // request packet. Shared by mouse movement and the pathfinder's
    // auto-walk (legacy PlayerMobile.Walk).
    // Returns true when a step or a turn was enqueued; false when blocked.
    internal static bool TryStep(
        Direction facing,
        bool run,
        MovementState moveState,
        TerrainPlugin.ChunksLoadedMap chunksLoaded,
        MultiCache multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tilesQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        NetClient network,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        float timeTotal,
        Profile profile,
        Single<Data<WorldPosition, Facing, MobileSteps, MobAnimation, ServerFlags>, With<Player>> playerQuery
    )
    {
        Span<sbyte> diag = [1, -1];

        (var worldPos, var dir, var mobSteps, var animation, var flags) = playerQuery.Get();

        // Legacy PlayerMobile.Walk: a hidden player is forced to walk when
        // AlwaysRunUnlessHidden is set.
        if (profile.AlwaysRunUnlessHidden && flags.Ref.Value.HasFlag(Flags.Hidden))
            run = false;

        var hasNoSteps = mobSteps.Ref.Index < 0;
        var playerX = worldPos.Ref.X;
        var playerY = worldPos.Ref.Y;
        var playerZ = worldPos.Ref.Z;
        var playerDir = dir.Ref.Value;

        if (!hasNoSteps)
        {
            ref var lastStep = ref mobSteps.Ref.CurrentStep();
            playerX = (ushort)lastStep.X;
            playerY = (ushort)lastStep.Y;
            playerZ = lastStep.Z;
            playerDir = (Direction)lastStep.Direction;
        }

        playerDir &= ~Direction.Running;
        facing &= ~Direction.Running;

        var newX = playerX;
        var newY = playerY;
        var newZ = playerZ;
        var newFacing = facing;

        var sameDir = playerDir == facing;
        var canMove = CheckMovement(
            moveState,
            chunksLoaded,
            multiCache,
            qHouseRevision,
            childrenQuery,
            othersQuery,
            tilesQuery,
            staticsQuery,
            tileData,
            facing,
            ref newX,
            ref newY,
            ref newZ
        );
        var isDiagonal = (byte)facing % 2 != 0;

        if (isDiagonal)
        {
            if (canMove)
            {
                for (var i = 0; i < 2 && canMove; i++)
                {
                    var testDir = (Direction)(((byte)facing + diag[i]) % 8);
                    var testX = playerX;
                    var testY = playerY;
                    var testZ = playerZ;
                    canMove = CheckMovement(
                        moveState,
                        chunksLoaded,
                        multiCache,
                        qHouseRevision,
                        childrenQuery,
                        othersQuery,
                        tilesQuery,
                        staticsQuery,
                        tileData,
                        testDir,
                        ref testX,
                        ref testY,
                        ref testZ
                    );
                }
            }

            if (!canMove)
            {
                for (var i = 0; i < 2 && !canMove; i++)
                {
                    newFacing = (Direction)(((byte)facing + diag[i]) % 8);
                    newX = playerX;
                    newY = playerY;
                    newZ = playerZ;
                    canMove = CheckMovement(
                        moveState,
                        chunksLoaded,
                        multiCache,
                        qHouseRevision,
                        childrenQuery,
                        othersQuery,
                        tilesQuery,
                        staticsQuery,
                        tileData,
                        newFacing,
                        ref newX,
                        ref newY,
                        ref newZ
                    );
                }
            }
        }

        if (canMove)
        {
            sameDir = playerDir == newFacing;

            if (sameDir)
            {
                playerX = newX;
                playerY = newY;
                playerZ = newZ;
            }

            playerDir = newFacing;
        }
        else if (!sameDir)
        {
            playerDir = facing;
        }

        if (canMove || !sameDir)
        {
            var isMountedOrFlying = animation.Ref.MountAction != 0xFF || flags.Ref.Value.HasFlag(Flags.Flying);
            var stepTime = sameDir
                ? MovementSpeed.TimeToCompleteMovement(run, isMountedOrFlying)
                : (profile.FastRotation ? Constants.TURN_DELAY_FAST : Constants.TURN_DELAY);
            ref var requestedStep = ref playerRequestedSteps.Value.Steps[playerRequestedSteps.Value.Count];
            requestedStep.Sequence = playerRequestedSteps.Value.Sequence;
            requestedStep.X = playerX;
            requestedStep.Y = playerY;
            requestedStep.Z = playerZ;
            requestedStep.Direction = playerDir;

            network.Send_WalkRequest(requestedStep.Direction, requestedStep.Sequence, run, 0);

            playerRequestedSteps.Value.Count = Math.Min(5, playerRequestedSteps.Value.Count + 1);
            playerRequestedSteps.Value.Sequence = (byte)((playerRequestedSteps.Value.Sequence % byte.MaxValue) + 1);
            playerRequestedSteps.Value.LastStep = timeTotal + stepTime;
            if (run)
                requestedStep.Direction |= Direction.Running;

            ref var step = ref mobSteps.Ref.NextStep();
            step.X = playerX;
            step.Y = playerY;
            step.Z = playerZ;
            step.Direction = (byte)playerDir;
            step.Run = run;

            if (hasNoSteps)
                mobSteps.Ref.Time = timeTotal;

            // AutoOpenDoors (legacy PlayerMobile.TryOpenDoors): on a real step,
            // if the tile ahead in the facing direction holds a door item, send
            // the open-door command so the next step walks through it.
            if (canMove && profile.AutoOpenDoors)
            {
                Walkability.GetNewXY((Direction)((byte)playerDir & 0x7), out var doorOx, out var doorOy);
                int doorX = playerX + doorOx, doorY = playerY + doorOy;
                foreach (var (_, oPos, oGfx, _) in othersQuery)
                {
                    if (oPos.Ref.X == doorX && oPos.Ref.Y == doorY
                        && Math.Abs(oPos.Ref.Z - playerZ) <= 15
                        && tileData.StaticData[oGfx.Ref.Value].IsDoor)
                    {
                        network.Send_OpenDoor();
                        break;
                    }
                }
            }
        }

        return canMove || !sameDir;
    }

    static void ParseAcceptedSteps(
        EventReader<AcceptedStep> acceptedSteps,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        Res<NetClient> network,
        Res<ILogger> logger
    )
    {
        foreach (var response in acceptedSteps.Read())
        {
            // TODO: query for the player notoriety
            var notoriety = response.Notoriety;

            var stepIndex = 0;
            for (var i = 0; i < playerRequestedSteps.Value.Count; i++)
            {
                ref readonly var step = ref playerRequestedSteps.Value.Steps[i];

                if (step.Sequence == response.Sequence)
                {
                    break;
                }

                stepIndex += 1;
            }

            var isBadStep = stepIndex == playerRequestedSteps.Value.Count;

            if (isBadStep)
            {
                if (response.Sequence < playerRequestedSteps.Value.Sequence)
                {
                    isBadStep = false;
                }
            }

            if (!isBadStep)
            {
                playerRequestedSteps.Value.ResyncSent = false;
                for (var i = 1; i < playerRequestedSteps.Value.Count; i++)
                {
                    playerRequestedSteps.Value.Steps[i - 1] = playerRequestedSteps.Value.Steps[i];
                }

                playerRequestedSteps.Value.Count = Math.Max(0, playerRequestedSteps.Value.Count - 1);
            }

            if (isBadStep)
            {
                logger.Value.LogWarning("bad step found");
                if (!playerRequestedSteps.Value.ResyncSent)
                {
                    logger.Value.LogTrace("sending resync");
                    network.Value.Send_Resync();
                    playerRequestedSteps.Value.ResyncSent = true;
                }

                playerRequestedSteps.Value.Count = 0;
                playerRequestedSteps.Value.Sequence = 0;

                break;
            }
        }
    }

    static void ParseDeniedSteps(
        EventReader<RejectedStep> rejectedSteps,
        ResMut<PlayerStepsContext> playerRequestedSteps,
        ResMut<PathfindState> pathfindState,
        Single<Data<WorldPosition, Facing, MobileSteps>, With<Player>> playerQuery,
        Res<ILogger> logger
    )
    {
        logger.Value.LogWarning("step denied");

        (var pos, var dir, var steps) = playerQuery.Get();
        foreach (var response in rejectedSteps.Read())
        {
            pos.Ref.X = response.X;
            pos.Ref.Y = response.Y;
            pos.Ref.Z = response.Z;
            dir.Ref.Value = response.Direction & Direction.Mask;
            steps.Ref.ClearSteps();
        }

        playerRequestedSteps.Value.Count = 0;
        playerRequestedSteps.Value.Sequence = 0;
        playerRequestedSteps.Value.LastStep = 0;
        playerRequestedSteps.Value.ResyncSent = false;

        // A denied step invalidates the computed path (legacy
        // Pathfinder.StopAutoWalk on Walk failure).
        pathfindState.Value.StopAutoWalk();
    }

    private static void FillListOfItemsAtPosition(
        MovementState moveState,
        TerrainPlugin.ChunksLoadedMap chunksLoaded,
        MultiCache multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tileQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        int x, int y
    )
    {
        var list = moveState.Scratch;
        list.Clear();

        if (!chunksLoaded.TryGetValue((x >> 3, y >> 3), out var val))
            return;

        if (childrenQuery.TryGet(val.entity, out var childrenRow))
        {
            (_, var children) = childrenRow;
            foreach (var child in children.Ref)
            {
                if (tileQuery.TryGet(child, out var tileRow))
                {
                    (var pos, var graphic, var stretched) = tileRow;
                    if (pos.Ref.X != x || pos.Ref.Y != y)
                        continue;

                    if (!Walkability.LandPasses(graphic.Ref.Value))
                        continue;

                    ref var landData = ref tileData.LandData[graphic.Ref.Value];
                    var flags = Walkability.LandFlags(in landData, moveState.StepState);
                    var stretchedValid = stretched.IsValid();
                    list.Add(Walkability.MakeLand(flags, pos.Ref.Z, stretchedValid, stretchedValid ? stretched.Ref : default));
                }

                if (staticsQuery.TryGet(child, out var staticRow))
                {
                    (var pos, var graphic) = staticRow;
                    if (pos.Ref.X != x || pos.Ref.Y != y)
                        continue;

                    ref var staticData = ref tileData.StaticData[graphic.Ref.Value];
                    var flags = Walkability.StaticFlags(graphic.Ref.Value, in staticData, moveState.StepState, moveState.SmoothDoors, moveState.IsGm);

                    if (flags != 0)
                        list.Add(Walkability.MakeStatic(flags, pos.Ref.Z, in staticData));
                }
            }
        }

        foreach ((var ent, var pos, var graphic, var anim) in othersQuery)
        {
            if (pos.Ref.X != x || pos.Ref.Y != y)
                continue;

            // Living characters block (legacy CreateItemList mobile branch).
            if (anim.IsValid())
            {
                if (!moveState.IgnoreCharacters && !Walkability.IsDeadBody(graphic.Ref.Value))
                    list.Add(Walkability.MakeMobile(pos.Ref.Z));
                continue;
            }

            var graphicValue = graphic.Ref.Value;
            // TODO: use Optional<HouseRevision> in query
            if (qHouseRevision.Contains(ent.Ref))
            {
                var multiInfo = multiCache.GetMulti(graphicValue);
                if (multiInfo.Id != 0)
                    graphicValue = multiInfo.Id;
            }

            ref var staticData = ref tileData.StaticData[graphicValue];
            var flags = Walkability.StaticFlags(graphicValue, ref staticData, moveState.StepState, moveState.SmoothDoors, moveState.IsGm);

            if (flags != 0)
                list.Add(Walkability.MakeStatic(flags, pos.Ref.Z, in staticData));
        }
    }

    private static bool CheckMovement(
        MovementState moveState,
        TerrainPlugin.ChunksLoadedMap chunksLoaded,
        MultiCache multiCache,
        Query<Empty, With<HouseRevision>> qHouseRevision,
        Query<Data<Children>> childrenQuery,
        Query<Data<WorldPosition, Graphic, MobAnimation>, Filter<Without<IsTile>, Without<IsStatic>, Optional<MobAnimation>, Without<Player>>> othersQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tileQuery,
        Query<Data<WorldPosition, Graphic>, Filter<With<IsStatic>, Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        Direction facing,
        ref ushort playerX, ref ushort playerY, ref sbyte playerZ
    )
    {
        Walkability.GetNewXY(facing, out var offsetX, out var offsetY);
        var newX = (ushort)(playerX + offsetX);
        var newY = (ushort)(playerY + offsetY);

        // Min/max z from the tile behind the destination (legacy
        // CalculateMinMaxZ: direction ^ 4).
        var dirIndex = (byte)facing & 7;
        Walkability.GetNewXY((Direction)(dirIndex ^ 4), out var backX, out var backY);
        FillListOfItemsAtPosition(
            moveState, chunksLoaded, multiCache, qHouseRevision, childrenQuery,
            othersQuery, tileQuery, staticsQuery, tileData,
            newX + backX, newY + backY);
        Walkability.GetMinMaxZCore(moveState.Scratch, dirIndex, playerZ, out var minZ, out var maxZ);

        FillListOfItemsAtPosition(
            moveState, chunksLoaded, multiCache, qHouseRevision, childrenQuery,
            othersQuery, tileQuery, staticsQuery, tileData,
            newX, newY);

        var z = playerZ;
        var canMove = Walkability.CalculateNewZCore(moveState.Scratch, moveState.StepState, minZ, maxZ, ref z);

        playerX = newX;
        playerY = newY;
        playerZ = z;

        return canMove;
    }
}
