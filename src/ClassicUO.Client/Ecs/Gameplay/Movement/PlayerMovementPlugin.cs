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


[TinyPlugin]
internal readonly partial struct PlayerMovementPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new PlayerStepsContext());
        scheduler.AddEvent<RejectedStep>();
        scheduler.AddEvent<AcceptedStep>();

        scheduler.OnEnter
        (
            GameState.GameScreen, (Res<PlayerStepsContext> playerRequestedSteps) =>
            {
                playerRequestedSteps.Value = new PlayerStepsContext();
            }, ThreadingMode.Single
        );
    }


    private static bool IsMouseInCameraBounds
    (
        Res<MouseContext> mouseCtx,
        Res<Camera> camera,
        Local<(float X, float Y)> clickedPos
    )
    {
        if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right))
        {
            clickedPos.Value = (mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y);
        }

        return camera.Value.Bounds.Contains((int)clickedPos.Value.X, (int)clickedPos.Value.Y);
    }

    private static bool CanWalk(
        Res<MouseContext> mouseCtx,
        Res<PlayerStepsContext> playerRequestedSteps,
        Local<bool> autoWalk,
        Time time,
        Query<Data<WorldPosition, Facing, MobileSteps, MobAnimation>, With<Player>> playerQuery
    )
    {
        if (!autoWalk)
        {
            if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Right) &&
                mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
            {
                autoWalk.Value = true;
            }
        }
        else if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Right))
        {
            autoWalk.Value = false;
        }

        return (autoWalk || mouseCtx.Value.IsPressed(Input.MouseButtonType.Right)) &&
               playerRequestedSteps.Value.LastStep < time.Total && playerRequestedSteps.Value.Count < 5 &&
               playerQuery.Count() > 0;
    }

    [TinySystem(threadingMode: ThreadingMode.Single)]
    [RunIf(nameof(IsMouseInCameraBounds))]
    [RunIf(nameof(CanWalk))]
    void EnqueuePlayerSteps
    (
        Local<List<TerrainInfo>> terrainList,
        Res<UOFileManager> fileManager,
        Res<GraphicsDevice> device,
        Res<MouseContext> mouseCtx,
        Res<NetClient> network,
        Res<PlayerStepsContext> playerRequestedSteps,
        Res<Camera> camera,
        Single<Data<WorldPosition, Facing, MobileSteps, MobAnimation, ServerFlags>, With<Player>> playerQuery,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tilesQuery,
        Query<Data<WorldPosition, Graphic>, Filter<Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        Time time
    )
    {
        terrainList.Value ??= new();
        Span<sbyte> diag = [1, -1];

        var center = new Vector2(camera.Value.Bounds.Right, camera.Value.Bounds.Bottom);
        center.X -= camera.Value.Bounds.Width / 2f;
        center.Y -= camera.Value.Bounds.Height / 2f;

        var mouseDir = (Direction)ClassicUO.Game.GameCursor.GetMouseDirection((int)center.X, (int)center.Y, (int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y, 1);
        var mouseRange = Utility.MathHelper.Hypotenuse(center.X - mouseCtx.Value.Position.X, center.Y - mouseCtx.Value.Position.Y);
        var facing = mouseDir == Direction.North ? Direction.Mask : mouseDir - 1;
        var run = mouseRange >= 190 || false;

        (var worldPos, var dir, var mobSteps, var animation, var flags) = playerQuery.Get();
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
            var isMountedOrFlying = animation.Ref.MountAction != 0xFF || flags.Ref.Value.HasFlag(Flags.Flying);
            var stepTime = sameDir ? MovementSpeed.TimeToCompleteMovement(run, isMountedOrFlying) : Constants.TURN_DELAY;
            ref var requestedStep = ref playerRequestedSteps.Value.Steps[playerRequestedSteps.Value.Count];
            requestedStep.Sequence = playerRequestedSteps.Value.Sequence;
            requestedStep.X = playerX;
            requestedStep.Y = playerY;
            requestedStep.Z = playerZ;
            requestedStep.Direction = playerDir;

            Console.WriteLine("SEQUENCE: {0}", requestedStep.Sequence);
            network.Value.Send_WalkRequest(requestedStep.Direction, requestedStep.Sequence, run, 0);

            playerRequestedSteps.Value.Count = Math.Min(5, playerRequestedSteps.Value.Count + 1);
            playerRequestedSteps.Value.Sequence = (byte)((playerRequestedSteps.Value.Sequence % byte.MaxValue) + 1);
            playerRequestedSteps.Value.LastStep = time.Total + stepTime;
            if (run)
                requestedStep.Direction |= Direction.Running;

            ref var step = ref mobSteps.Ref.NextStep();
            step.X = playerX;
            step.Y = playerY;
            step.Z = playerZ;
            step.Direction = (byte)playerDir;
            step.Run = run;

            if (hasNoSteps)
                mobSteps.Ref.Time = time.Total;
        }
    }


    private static bool IsAcceptedStepsNotEmpty(EventReader<AcceptedStep> responses) => !responses.IsEmpty;

    [TinySystem(threadingMode: ThreadingMode.Single)]
    [RunIf(nameof(IsAcceptedStepsNotEmpty))]
    void ParseAcceptedSteps(
        EventReader<AcceptedStep> acceptedSteps,
        Res<PlayerStepsContext> playerRequestedSteps,
        Res<NetClient> network
    )
    {
        foreach (var response in acceptedSteps)
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
                // Console.WriteLine("step accepted {0}", playerRequestedSteps.Value.Steps[stepIndex].Sequence);
                for (var i = 1; i < playerRequestedSteps.Value.Count; i++)
                {
                    playerRequestedSteps.Value.Steps[i - 1] = playerRequestedSteps.Value.Steps[i];
                }

                playerRequestedSteps.Value.Count = Math.Max(0, playerRequestedSteps.Value.Count - 1);
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

                playerRequestedSteps.Value.Count = 0;
                playerRequestedSteps.Value.Sequence = 0;

                break;
            }
        }
    }


    private static bool IsRejectedStepsNotEmpty(EventReader<RejectedStep> responses) => !responses.IsEmpty;

    [TinySystem(threadingMode: ThreadingMode.Single)]
    [RunIf(nameof(IsRejectedStepsNotEmpty))]
    void ParseDeniedSteps(
        EventReader<RejectedStep> rejectedSteps,
        Res<PlayerStepsContext> playerRequestedSteps,
        Query<Data<WorldPosition, Facing, MobileSteps>, With<Player>> playerQuery
    )
    {
        Console.WriteLine("step denied");

        (var pos, var dir, var steps) = playerQuery.Single();
        foreach (var response in rejectedSteps)
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
    }

    private static void FillListOfItemsAtPosition
    (
        List<TerrainInfo> list,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tileQuery,
        Query<Data<WorldPosition, Graphic>, Filter<Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        int x, int y
    )
    {
        list.Clear();

        foreach ((var pos, var graphic, var stretched) in tileQuery)
        {
            if (pos.Ref.X != x || pos.Ref.Y != y)
                continue;

            if (!((graphic.Ref.Value < 0x01AE && graphic.Ref.Value != 2) || (graphic.Ref.Value > 0x01B5 && graphic.Ref.Value != 0x1DB)))
                continue;

            ref var landData = ref tileData.LandData[graphic.Ref.Value];
            var flags = TerrainFlags.ImpassableOrSurface;

            if (!landData.IsImpassable)
            {
                flags = TerrainFlags.ImpassableOrSurface |
                        TerrainFlags.Surface |
                        TerrainFlags.Bridge;
            }

            TerrainInfo tinfo;
            if (!stretched.IsValid())
            {
                tinfo = new()
                {
                    Flags = flags,
                    Z = pos.Ref.Z,
                    AvgZ = pos.Ref.Z,
                    Height = pos.Ref.Z,
                    LandStretched = !Unsafe.IsNullRef(ref stretched.Ref),
                    LandBounds = default,
                    RealZ = pos.Ref.Z
                };
            }
            else
            {
                tinfo = new TerrainInfo()
                {
                    Flags = flags,
                    Z = stretched.Ref.MinZ,
                    AvgZ = stretched.Ref.AvgZ,
                    Height = stretched.Ref.AvgZ - stretched.Ref.MinZ,
                    LandStretched = !Unsafe.IsNullRef(ref stretched.Ref),
                    LandBounds = stretched.Ref.Offset,
                    RealZ = pos.Ref.Z
                };
            }

            list.Add(tinfo);
        }


        foreach ((var pos, var graphic) in staticsQuery)
        {
            if (pos.Ref.X != x || pos.Ref.Y != y)
                continue;

            ref var staticData = ref tileData.StaticData[graphic.Ref.Value];
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
                    Z = pos.Ref.Z,
                    AvgZ = pos.Ref.Z + (staticData.IsBridge ? staticData.Height / 2 : staticData.Height),
                    Height = staticData.Height
                };

                list.Add(tinfo);
            }
        }
    }

    private static void GetMinMaxZ
    (
        List<TerrainInfo> list,
        Query<TinyEcs.Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tileQuery,
        Query<TinyEcs.Data<WorldPosition, Graphic>, TinyEcs.Filter<Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        Direction facing,
        int playerX, int playerY, int playerZ,
        out int minZ, out int maxZ
    )
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
                var avgZ = getAvgZ(newDir, tinfo.LandBounds, tinfo.RealZ);
                if (minZ < avgZ)
                    minZ = avgZ;
                if (maxZ < avgZ)
                    maxZ = avgZ;

                static int getAvgZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ)
                {
                    int res = getDirZ(((byte)(dir >> 1) + 1) & 3, offs, curZ);
                    if ((dir & 1) != 0) return res;
                    return (res + getDirZ(dir >> 1, offs, curZ)) >> 1;

                    static int getDirZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ) => dir switch
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

    private static bool CheckMovement
    (
        List<TerrainInfo> list,
        Query<TinyEcs.Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> tileQuery,
        Query<TinyEcs.Data<WorldPosition, Graphic>, TinyEcs.Filter<Without<IsTile>, Without<MobAnimation>>> staticsQuery,
        TileDataLoader tileData,
        Direction facing,
        ref ushort playerX, ref ushort playerY, ref sbyte playerZ
    )
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
            playerZ = (sbyte)minZ;
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
