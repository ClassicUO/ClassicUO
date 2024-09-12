using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

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
    public int Index;
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
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new PlayerStepsContext());
        scheduler.AddEvent<RejectedStep>();
        scheduler.AddEvent<AcceptedStep>();

        var enqueuePlayerStepsFn = EnqueuePlayerSteps;
        scheduler.AddSystem(enqueuePlayerStepsFn, threadingType: ThreadingMode.Single)
            .RunIf((Res<MouseContext> mouseCtx, Res<PlayerStepsContext> playerRequestedSteps, Time time)
                => mouseCtx.Value.NewState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                   playerRequestedSteps.Value.LastStep < time.Total && playerRequestedSteps.Value.Index < 5);

        var parseAcceptedStepsFn = ParseAcceptedSteps;
        scheduler.AddSystem(parseAcceptedStepsFn, threadingType: ThreadingMode.Single)
            .RunIf((EventReader<AcceptedStep> responses) => !responses.IsEmpty);

        var parseDeniedStepsFn = ParseDeniedSteps;
        scheduler.AddSystem(parseDeniedStepsFn, threadingType: ThreadingMode.Single)
            .RunIf((EventReader<RejectedStep> responses) => !responses.IsEmpty);
    }

    void EnqueuePlayerSteps
    (
        Local<List<TerrainInfo>> terrainList,
        Res<UOFileManager> fileManager,
        Res<GraphicsDevice> device,
        Res<MouseContext> mouseCtx,
        Res<NetClient> network,
        Res<PlayerStepsContext> playerRequestedSteps,
        Query<(WorldPosition, Facing, MobileSteps, MobAnimation), With<Player>> playerQuery,
        Query<(WorldPosition, Graphic, Optional<TileStretched>), With<IsTile>> tilesQuery,
        Query<(WorldPosition, Graphic), (Without<IsTile>, Without<MobAnimation>)> staticsQuery,
        Time time
    )
    {
        terrainList.Value ??= new();
        Span<sbyte> diag = stackalloc sbyte[2] { 1, -1 };

        // TODO: we grab the center of the screen atm for convenience. But it will be necessary to use the game window bounds
        // var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        var center = new Vector2(device.Value.PresentationParameters.BackBufferWidth, device.Value.PresentationParameters.BackBufferHeight);
        center.X -= device.Value.PresentationParameters.BackBufferWidth / 2f;
        center.Y -= device.Value.PresentationParameters.BackBufferHeight / 2f;

        var mouseDir = (Direction) ClassicUO.Game.GameCursor.GetMouseDirection((int)center.X, (int)center.Y, mouseCtx.Value.NewState.X, mouseCtx.Value.NewState.Y, 1);
        var mouseRange = Utility.MathHelper.Hypotenuse(center.X - mouseCtx.Value.NewState.X, center.Y - mouseCtx.Value.NewState.Y);
        var facing = mouseDir == Direction.North ? Direction.Mask : mouseDir - 1;
        var run = mouseRange >= 190 || false;

        ref var mobSteps = ref playerQuery.Single<MobileSteps>();
        ref readonly var worldPos = ref playerQuery.Single<WorldPosition>();
        var playerDir = playerQuery.Single<Facing>().Value;
        var hasNoSteps = mobSteps.Count == 0;
        var playerX = worldPos.X;
        var playerY = worldPos.Y;
        var playerZ = worldPos.Z;

        if (!hasNoSteps)
        {
            ref var lastStep = ref mobSteps[Math.Max(0, mobSteps.Count - 1)];
            playerX = (ushort) lastStep.X;
            playerY = (ushort) lastStep.Y;
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
        var canMove = CheckMovement(terrainList, tilesQuery, staticsQuery, fileManager.Value.TileData, facing, ref newX, ref newY, ref newZ);
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
                    canMove = CheckMovement(terrainList, tilesQuery, staticsQuery, fileManager.Value.TileData, testDir, ref testX, ref testY, ref testZ);
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
                    canMove = CheckMovement(terrainList, tilesQuery, staticsQuery, fileManager.Value.TileData, newFacing, ref newX, ref newY, ref newZ);
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
            // ref var playerFlags = ref playerQuery.Single<MobileFlags>();
            var playerFlags = Flags.None;
            ref var animation = ref playerQuery.Single<MobAnimation>();
            var isMountedOrFlying = animation.MountAction != 0xFF || playerFlags.HasFlag(Flags.Flying);
            var stepTime = sameDir ? MovementSpeed.TimeToCompleteMovement(run, isMountedOrFlying) : Constants.TURN_DELAY;
            ref var requestedStep = ref playerRequestedSteps.Value.Steps[playerRequestedSteps.Value.Index];
            requestedStep.Sequence = playerRequestedSteps.Value.Sequence;
            requestedStep.X = playerX;
            requestedStep.Y = playerY;
            requestedStep.Z = playerZ;
            requestedStep.Direction = playerDir;

            network.Value.Send_WalkRequest(requestedStep.Direction, requestedStep.Sequence, run, 0);

            playerRequestedSteps.Value.Index = Math.Min(5, playerRequestedSteps.Value.Index + 1);
            playerRequestedSteps.Value.Sequence = (byte)((playerRequestedSteps.Value.Sequence % byte.MaxValue) + 1);
            playerRequestedSteps.Value.LastStep = time.Total + stepTime;
            if (run)
                requestedStep.Direction |= Direction.Running;

            ref var step = ref mobSteps[mobSteps.Count];
            step.X = playerX;
            step.Y = playerY;
            step.Z = playerZ;
            step.Direction = (byte)playerDir;
            step.Run = run;
            mobSteps.Count = Math.Min(MobileSteps.COUNT - 1, mobSteps.Count + 1);

            if (hasNoSteps)
                mobSteps.Time = time.Total;
        }
    }

    void ParseAcceptedSteps(EventReader<AcceptedStep> acceptedSteps, Res<PlayerStepsContext> playerRequestedSteps, Res<NetClient> network)
    {
        foreach (var response in acceptedSteps)
        {
            // TODO: query for the player notoriety
            var notoriety = response.Notoriety;

            var stepIndex = 0;
            for (var i = 0; i < playerRequestedSteps.Value.Index; i++)
            {
                ref readonly var step = ref playerRequestedSteps.Value.Steps[i];

                if (step.Sequence == response.Sequence)
                {
                    break;
                }

                stepIndex += 1;
            }

            var isBadStep = stepIndex == playerRequestedSteps.Value.Index;

            if (!isBadStep)
            {
                Console.WriteLine("step accepted");
                for (var i = 1; i < playerRequestedSteps.Value.Index; i++)
                {
                    playerRequestedSteps.Value.Steps[i - 1] = playerRequestedSteps.Value.Steps[i];
                }

                playerRequestedSteps.Value.Index = Math.Max(0, playerRequestedSteps.Value.Index - 1);
            }

            if (isBadStep)
            {
                Console.WriteLine("bad step found");
                if (!playerRequestedSteps.Value.ResyncSent)
                {
                    Console.WriteLine("sending resync");
                    network.Value.Send_Resync();
                    playerRequestedSteps.Value.ResyncSent = true;
                }

                playerRequestedSteps.Value.Index = 0;
                playerRequestedSteps.Value.Sequence = 0;
            }
        }
    }

    void ParseDeniedSteps(EventReader<RejectedStep> rejectedSteps, Res<PlayerStepsContext> playerRequestedSteps, Query<(WorldPosition, Facing, MobileSteps), With<Player>> playerQuery)
    {
        Console.WriteLine("step denied");
        var player = playerQuery.Single();
        foreach (var response in rejectedSteps)
        {
            player.Set
            (
                new WorldPosition()
                {
                    X = response.X,
                    Y = response.Y,
                    Z = response.Z
                }
            );

            player.Set
            (
                new Facing()
                {
                    Value = response.Direction
                }
            );

            player.Get<MobileSteps>().Count = 0;
        }

        playerRequestedSteps.Value.Index = 0;
        playerRequestedSteps.Value.Sequence = 0;
        playerRequestedSteps.Value.LastStep = 0;
        playerRequestedSteps.Value.ResyncSent = false;
    }

    private static void FillListOfItemsAtPosition(List<TerrainInfo> list, Query tileQuery, Query staticsQuery, TileDataLoader tileData, int x, int y)
    {
        list.Clear();

        tileQuery.Each(
            (ref WorldPosition pos, ref Graphic graphic, ref TileStretched stretched) =>
            {
                if (pos.X != x || pos.Y != y)
                    return;

                if (!((graphic.Value < 0x01AE && graphic.Value != 2) || (graphic.Value > 0x01B5 && graphic.Value != 0x1DB)))
                    return;

                ref var landData = ref tileData.LandData[graphic.Value];
                var flags = TerrainFlags.ImpassableOrSurface;

                if (!landData.IsImpassable)
                {
                    flags = TerrainFlags.ImpassableOrSurface |
                            TerrainFlags.Surface |
                            TerrainFlags.Bridge;
                }

                TerrainInfo tinfo;
                if (Unsafe.IsNullRef(ref stretched))
                {
                    tinfo = new()
                    {
                        Flags = flags,
                        Z = pos.Z,
                        AvgZ = pos.Z,
                        Height = pos.Z,
                        LandStretched = !Unsafe.IsNullRef(ref stretched),
                        LandBounds = default,
                        RealZ = pos.Z
                    };
                }
                else
                {
                    tinfo = new TerrainInfo()
                    {
                        Flags = flags,
                        Z = stretched.MinZ,
                        AvgZ = stretched.AvgZ,
                        Height = stretched.AvgZ - stretched.MinZ,
                        LandStretched = !Unsafe.IsNullRef(ref stretched),
                        LandBounds = stretched.Offset,
                        RealZ = pos.Z
                    };
                }

                list.Add(tinfo);
            });

        staticsQuery.Each(
            (ref WorldPosition pos, ref Graphic graphic) =>
            {
                if (pos.X != x || pos.Y != y)
                    return;

                ref var staticData = ref tileData.StaticData[graphic.Value];
                TerrainFlags flags = 0;

                if (staticData.IsImpassable || staticData.IsSurface)
                {
                    flags = TerrainFlags.ImpassableOrSurface;
                }

                if (!staticData.IsImpassable)
                {
                    if (staticData.IsSurface)
                    {
                        flags |= TerrainFlags.Surface;
                    }

                    if (staticData.IsBridge)
                    {
                        flags |= TerrainFlags.Bridge;
                    }
                }

                if (flags != 0)
                {
                    var tinfo = new TerrainInfo()
                    {
                        Flags = flags,
                        Z = pos.Z,
                        AvgZ = pos.Z + (staticData.IsBridge ? staticData.Height / 2 : staticData.Height),
                        Height = staticData.Height
                    };

                    list.Add(tinfo);
                }
            });
    }

    private static void GetMinMaxZ(List<TerrainInfo> list, Query tileQuery, Query staticsQuery, TileDataLoader tileData, Direction facing, int playerX, int playerY, int playerZ, out int minZ, out int maxZ)
    {
        Span<int> offX = stackalloc int[]
        {
            0,
            1,
            1,
            1,
            0,
            -1,
            -1,
            -1,
            0,
            1
        };
        Span<int> offY = stackalloc int[]
        {
            -1,
            -1,
            0,
            1,
            1,
            1,
            0,
            -1,
            -1,
            -1
        };
        var newDir = (byte)facing;
        newDir &= 7;
        var newX = (ushort)(playerX + offX[newDir ^ 4]);
        var newY = (ushort)(playerY + offY[newDir ^ 4]);
        FillListOfItemsAtPosition(list, tileQuery, staticsQuery, tileData, newX, newY);

        maxZ = playerZ;
        minZ = -128;
        foreach (ref readonly var tinfo in CollectionsMarshal.AsSpan(list))
        {
            if (tinfo.AvgZ <= playerZ && tinfo.LandStretched)
            {
                var avgZ = GetAvgZ(newDir, tinfo.LandBounds, tinfo.RealZ);
                if (minZ < avgZ)
                    minZ = avgZ;
                if (maxZ < avgZ)
                    maxZ = avgZ;

                static int GetAvgZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ)
                {
                    int res = GetDirZ(((byte)(dir >> 1) + 1) & 3, offs, curZ);
                    if ((dir & 1) != 0) return res;
                    return (res + GetDirZ(dir >> 1, offs, curZ)) >> 1;

                    static int GetDirZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ) => dir switch
                    {
                        1 => offs.Right >> 2,
                        2 => offs.Bottom >> 2,
                        3 => offs.Left >> 2,
                        _ => curZ
                    };
                }
            }
            else
            {
                if (tinfo.Flags.HasFlag(TerrainFlags.ImpassableOrSurface) && tinfo.AvgZ <= playerZ && minZ < tinfo.AvgZ)
                {
                    minZ = tinfo.AvgZ;
                }

                if (tinfo.Flags.HasFlag(TerrainFlags.Bridge) && playerZ == tinfo.AvgZ)
                {
                    var height = tinfo.Z + tinfo.Height;

                    if (maxZ < height)
                    {
                        maxZ = height;
                    }

                    if (minZ > tinfo.Z)
                    {
                        minZ = tinfo.Z;
                    }
                }
            }
        }

        maxZ += 2;
    }

    private static bool CheckMovement(List<TerrainInfo> list, Query tileQuery, Query staticsQuery, TileDataLoader tileData, Direction facing, ref ushort playerX, ref ushort playerY, ref sbyte playerZ)
    {
        GetNewXY(facing, out var offsetX, out var offsetY);
        var newX = (ushort)(playerX + offsetX);
        var newY = (ushort)(playerY + offsetY);
        GetMinMaxZ(list, tileQuery, staticsQuery, tileData, facing, newX, newY, playerZ, out var minZ, out var maxZ);

        FillListOfItemsAtPosition(list, tileQuery, staticsQuery, tileData, newX, newY);
        if (list.Count == 0)
            return false;

        list.Sort();

        list.Add(new TerrainInfo()
        {
            Flags = TerrainFlags.ImpassableOrSurface,
            Z = 128,
            AvgZ = 128,
            Height = 128
        });

        var result = -128;

        if (playerZ < minZ)
        {
            playerZ = (sbyte) minZ;
        }

        var currentTempZ = 1000000;
        var currentZ = -128;

        for (int i = 0; i < list.Count; ++i)
        {
            var tinfo = list[i];

            if (tinfo.Flags.HasFlag(TerrainFlags.ImpassableOrSurface))
            {
                if (tinfo.Z - minZ >= ClassicUO.Game.Constants.DEFAULT_BLOCK_HEIGHT)
                {
                    for (int j = i - 1; j >= 0; --j)
                    {
                        var temptInfo = list[j];

                        if ((temptInfo.Flags & (TerrainFlags.Surface | TerrainFlags.Bridge)) != 0)
                        {
                            if (temptInfo.AvgZ >= currentZ && tinfo.Z - temptInfo.AvgZ >= ClassicUO.Game.Constants.DEFAULT_BLOCK_HEIGHT &&
                                (temptInfo.AvgZ <= maxZ && temptInfo.Flags.HasFlag(TerrainFlags.Surface) || temptInfo.Flags.HasFlag(TerrainFlags.Bridge) && temptInfo.Z <= maxZ))
                            {
                                var delta = Math.Abs(playerZ - temptInfo.AvgZ);
                                if (delta < currentTempZ)
                                {
                                    currentTempZ = delta;
                                    result = temptInfo.AvgZ;
                                }
                            }
                        }
                    }
                }

                if (minZ < tinfo.AvgZ)
                {
                    minZ = tinfo.AvgZ;
                }

                if (currentZ < tinfo.AvgZ)
                {
                    currentZ = tinfo.AvgZ;
                }
            }
        }

        var canMove = result != -128;
        playerX = (ushort)(playerX + offsetX);
        playerY = (ushort)(playerY + offsetY);
        playerZ = (sbyte)result;

        return canMove;
    }

    private static void GetNewXY(Direction direction, out int offsetX, out int offsetY)
    {
        direction &= Direction.Mask;
        offsetX = 0; offsetY = 0;

        switch (direction)
        {
            case Direction.North:
                offsetY--;
                break;
            case Direction.Right:
                offsetX++;
                offsetY--;
                break;
            case Direction.East:
                offsetX++;
                break;
            case Direction.Down:
                offsetX++;
                offsetY++;
                break;
            case Direction.South:
                offsetY++;
                break;
            case Direction.Left:
                offsetX--;
                offsetY++;
                break;
            case Direction.West:
                offsetX--;
                break;
            case Direction.Up:
                offsetX--;
                offsetY--;
                break;
        }
    }

    [Flags]
    private enum TerrainFlags : uint
    {
        ImpassableOrSurface = 0x01,
        Surface = 0x02,
        Bridge = 0x04,
        NoDiagonal = 0x08
    }

    private struct TerrainInfo : IComparable<TerrainInfo>
    {
        public TerrainFlags Flags;
        public int Z;
        public int AvgZ;
        public int Height;
        public bool LandStretched;
        public int LandAvgZ;
        public UltimaBatcher2D.YOffsets LandBounds;
        public int RealZ;

        public readonly int CompareTo(TerrainInfo other)
        {
            var comparision = Z - other.Z;

            if (comparision == 0)
            {
                comparision = Height - other.Height;
            }

            return comparision;
        }
    }
}
