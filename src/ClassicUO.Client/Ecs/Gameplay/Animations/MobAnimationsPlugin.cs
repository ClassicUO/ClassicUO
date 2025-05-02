using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer.Animations;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using static ClassicUO.Game.GameObjects.Mobile;
using static ClassicUO.Renderer.UltimaBatcher2D;
using static TinyEcs.Defaults;

namespace ClassicUO.Ecs;


struct MobAnimation
{
    public int Index;
    public int FramesCount;
    public int Interval;
    public int RepeatMode;
    public int RepeatModeCount;
    public bool Repeat;
    public bool IsFromServer;
    public bool ForwardDirection;

    public byte Action = byte.MaxValue;
    public byte MountAction = byte.MaxValue;
    public Direction Direction;
    public float Time;
    public bool Run;

    public MobAnimation()
    {

    }
}

struct MobileFlags
{
    public Flags Value;
}

[InlineArray(MobileSteps.COUNT)]
struct MobileStepArray
{
    private Game.GameObjects.Mobile.Step _a;
}

struct MobileSteps
{
    public const int COUNT = 10;

    private MobileStepArray _steps;
    public int Index;
    public float Time;

    [UnscopedRef]
    public ref Game.GameObjects.Mobile.Step this[int index] => ref _steps[index];
}

struct MobileQueuedStep
{
    public uint Serial;
    public ushort X, Y;
    public sbyte Z;
    public Direction Direction;
}

readonly struct MobAnimationsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<MobileQueuedStep>();

        var readMobileStepsFn = ReadMobilesSteps;
        scheduler.OnUpdate(readMobileStepsFn, ThreadingMode.Single)
            .RunIf((EventReader<MobileQueuedStep> stepsQueued) => !stepsQueued.IsEmpty);

        var handleMobileStepsFn = HandleMobileSteps;
        scheduler.OnUpdate(handleMobileStepsFn, ThreadingMode.Single);

        var processMobileAnimationsFn = ProcessMobileAnimations;
        scheduler.OnUpdate(processMobileAnimationsFn, ThreadingMode.Single);
    }

    void ReadMobilesSteps(TinyEcs.World world, Time time, EventReader<MobileQueuedStep> stepsQueued, Res<GameContext> gameCtx, Res<NetworkEntitiesMap> entitiesMap)
    {
        foreach (var queuedStep in stepsQueued)
        {
            var ent = entitiesMap.Value.GetOrCreate(world, queuedStep.Serial);

            if (gameCtx.Value.PlayerSerial == queuedStep.Serial)
            {
                world.Set(ent, new WorldPosition()
                {
                    X = queuedStep.X,
                    Y = queuedStep.Y,
                    Z = queuedStep.Z,
                });
                world.Set(ent, new Facing() { Value = queuedStep.Direction });
                continue;
            }

            if (!world.Has<MobileSteps>(ent))
                world.Set(ent, new MobileSteps() { Index = -1 });

            if (!world.Has<WorldPosition>(ent))
                world.Set(ent, new WorldPosition()
                {
                    X = queuedStep.X,
                    Y = queuedStep.Y,
                    Z = queuedStep.Z,
                });

            if (!world.Has<Facing>(ent))
                world.Set(ent, new Facing() { Value = queuedStep.Direction });

            ref var steps = ref world.Get<MobileSteps>(ent);

            if (steps.Index >= MobileSteps.COUNT - 1)
            {
                world.Set(ent, new WorldPosition()
                {
                    X = queuedStep.X,
                    Y = queuedStep.Y,
                    Z = queuedStep.Z,
                });
                world.Set(ent, new Facing() { Value = queuedStep.Direction });
                steps.Index = -1;
                continue;
            }

            int endX, endY;
            sbyte endZ;
            Direction endDir;

            var clearedDir = (queuedStep.Direction & (Direction.Mask | ~Direction.Running));

            if (steps.Index < 0)
            {
                ref var pos = ref world.Get<WorldPosition>(ent);
                endX = pos.X;
                endY = pos.Y;
                endZ = pos.Z;
                endDir = world.Get<Facing>(ent).Value;
            }
            else
            {
                ref var step = ref steps[steps.Index];
                endX = step.X;
                endY = step.Y;
                endZ = step.Z;
                endDir = (Direction)step.Direction;
            }

            if (endX == queuedStep.X && endY == queuedStep.Y && endZ == queuedStep.Z && clearedDir == endDir)
            {
                continue;
            }

            if (steps.Index < 0)
            {
                if (steps.Time <= time.Total - Constants.WALKING_DELAY)
                {
                    ref var anim = ref world.Get<MobAnimation>(ent);
                    anim.Run = true;
                    //anim.Time = 0;
                }
                steps.Time = time.Total;
            }

            var moveDir = DirectionHelper.CalculateDirection(endX, endY, queuedStep.X, queuedStep.Y);

            if (moveDir != Direction.NONE)
            {
                if (moveDir != endDir)
                {
                    steps.Index = Math.Min(MobileSteps.COUNT - 1, steps.Index + 1);
                    ref var step1 = ref steps[steps.Index];
                    step1.X = endX;
                    step1.Y = endY;
                    step1.Z = endZ;
                    step1.Direction = (byte)moveDir;
                    step1.Run = queuedStep.Direction.HasFlag(Direction.Running);
                }

                steps.Index = Math.Min(MobileSteps.COUNT - 1, steps.Index + 1);
                ref var step2 = ref steps[steps.Index];
                step2.X = queuedStep.X;
                step2.Y = queuedStep.Y;
                step2.Z = queuedStep.Z;
                step2.Direction = (byte)moveDir;
                step2.Run = queuedStep.Direction.HasFlag(Direction.Running);
            }

            if (moveDir != clearedDir)
            {
                steps.Index = Math.Min(MobileSteps.COUNT - 1, steps.Index + 1);
                ref var step3 = ref steps[steps.Index];
                step3.X = queuedStep.X;
                step3.Y = queuedStep.Y;
                step3.Z = queuedStep.Z;
                step3.Direction = (byte)clearedDir;
                step3.Run = queuedStep.Direction.HasFlag(Direction.Running);
            }
        }
    }

    void HandleMobileSteps
    (
        Time time,
        Query<TinyEcs.Data<MobileSteps, WorldPosition, Facing, MobAnimation, ScreenPositionOffset>, Without<ContainedInto>> queryHandleWalking
    )
    {
        foreach ((var steps, var position, var direction, var animation, var offset) in queryHandleWalking)
        {
            while (steps.Ref.Index >= 0)
            {
                ref var step = ref steps.Ref[0];

                var delay = time.Total - steps.Ref.Time;
                var mount = animation.Ref.MountAction != 0xFF;

                if (!mount && steps.Ref.Index > 0 && delay > 0)
                {
                    mount = delay <= (step.Run ? MovementSpeed.STEP_DELAY_MOUNT_RUN : MovementSpeed.STEP_DELAY_WALK);
                }

                var maxDelay = MovementSpeed.TimeToCompleteMovement(step.Run, mount);
                var removeStep = delay >= maxDelay;
                var directionChange = false;

                if (position.Ref.X != step.X || position.Ref.Y != step.Y)
                {
                    var badStep = false;

                    if (offset.Ref.Value == Vector2.Zero)
                    {
                        var absX = Math.Abs(position.Ref.X - step.X);
                        var absY = Math.Abs(position.Ref.Y - step.Y);

                        badStep = absX > 1 || absY > 1 || absX + absY == 0;

                        if (!badStep)
                        {
                            absX = position.Ref.X;
                            absY = position.Ref.Y;

                            Pathfinder.GetNewXY((byte)(step.Direction & 7), ref absX, ref absY);

                            badStep = absX != step.X || absY != step.Y;
                        }
                    }

                    if (badStep)
                        removeStep = true;
                    else
                    {
                        var stepsCount = maxDelay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                        var x = delay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                        var y = x;

                        var offsetZ = ((step.Z - position.Ref.Z) * x * (4.0f / stepsCount));
                        MovementSpeed.GetPixelOffset(step.Direction, ref x, ref y, stepsCount);

                        offset.Ref.Value.X = x;
                        offset.Ref.Value.Y = y - offsetZ;
                    }

                    animation.Ref.Run = true;
                }
                else
                {
                    directionChange = true;
                    removeStep = true;
                }

                if (removeStep)
                {
                    position.Ref.X = (ushort)step.X;
                    position.Ref.Y = (ushort)step.Y;
                    position.Ref.Z = step.Z;
                    direction.Ref.Value = (Direction)step.Direction;

                    if (step.Run)
                        direction.Ref.Value |= Direction.Running;

                    for (var j = 1; j <= steps.Ref.Index; ++j)
                        steps.Ref[j - 1] = steps.Ref[j];

                    steps.Ref.Index -= 1;
                    offset.Ref.Value = Vector2.Zero;

                    if (directionChange)
                        continue;

                    animation.Ref.Run = true;
                    steps.Ref.Time = time.Total;
                }

                break;
            }
        }
    }

    void ProcessMobileAnimations
    (
        TinyEcs.World world,
        Time time,
        Res<GameContext> gameCtx,
        Res<UOFileManager> fileManager,
        Res<AssetsServer> assetsServer,
        Query<TinyEcs.Data<MobAnimation, Graphic, Facing, EquipmentSlots, MobileFlags, MobileSteps>,
            Filter<Without<ContainedInto>, Optional<MobileFlags>, Optional<MobileSteps>>> query
    )
    {
        foreach ((
            var animation,
            var graphic,
            var direction,
            var slots,
            var mobFlags,
            var mobSteps) in query)
        {
            // for (var i = 0; i < entities.Length; ++i)
            // {
            //     ref var animation = ref animationA[i];
            //     ref var graphic = ref graphicA[i];
            //     ref var direction = ref facingA[i];
            //     ref var slots = ref equipmentSlotsA[i];
            //     ref var mobFlags = ref mobFlagsA.IsEmpty ? ref Unsafe.NullRef<MobileFlags>() : ref mobFlagsA[i];
            //     ref var mobSteps = ref mobStepsA.IsEmpty ? ref Unsafe.NullRef<MobileSteps>() : ref mobStepsA[i];


            // }

            if (animation.Ref.Time >= time.Total)
                continue;

            var flags = Unsafe.IsNullRef(ref mobFlags.Ref) ? Flags.None : mobFlags.Ref.Value;
            var isWalking = false;
            var iterate = true;
            var realDirection = direction.Ref.Value;
            var mirror = false;
            var animId = graphic.Ref.Value;

            if (!Unsafe.IsNullRef(ref mobSteps.Ref))
            {
                isWalking = mobSteps.Ref.Time > time.Total - Constants.WALKING_DELAY;

                if (mobSteps.Ref.Index >= 0)
                {
                    isWalking = true;
                    realDirection = (Direction)mobSteps.Ref[0].Direction;
                    if (mobSteps.Ref[0].Run)
                    {
                        realDirection |= Direction.Running;
                    }
                    else
                    {
                        realDirection &= ~Direction.Running;
                    }
                }
                else if (isWalking)
                {
                    iterate = false;
                }
            }

            animation.Ref.MountAction = 0xFF;

            if (slots.Ref[Layer.Mount].IsValid() && world.Exists(slots.Ref[Layer.Mount]))
            {
                var mountGraphic = world.Get<Graphic>(slots.Ref[Layer.Mount]).Value;
                (mountGraphic, _) = Mounts.FixMountGraphic(fileManager.Value.TileData, mountGraphic);

                animation.Ref.MountAction = GetAnimationGroup(
                    gameCtx.Value.ClientVersion, fileManager.Value.Animations, assetsServer.Value.Animations,
                    mountGraphic, realDirection, isWalking, true, false, flags,
                    animation.Ref.IsFromServer, animation.Ref.MountAction
                );
            }

            animation.Ref.Action = GetAnimationGroup(
                gameCtx.Value.ClientVersion, fileManager.Value.Animations, assetsServer.Value.Animations,
                animId, realDirection, isWalking, animation.Ref.MountAction != 0xFF, false, flags,
                animation.Ref.IsFromServer, animation.Ref.Action);

            realDirection &= ~Direction.Running;
            realDirection &= Direction.Mask;

            var dir = (byte)(realDirection);
            assetsServer.Value.Animations.GetAnimDirection(ref dir, ref mirror);

            var frames = assetsServer.Value.Animations.GetAnimationFrames
            (
                animId,
                animation.Ref.Action,
                dir,
                out _,
                out _
            );

            var currDelay = Constants.CHARACTER_ANIMATION_DELAY;
            var fc = frames.Length;
            var d = iterate ? 1 : 0;
            var frameIndex = animation.Ref.Index + (animation.Ref.IsFromServer && !animation.Ref.ForwardDirection ? -d : d);

            if (animation.Ref.IsFromServer)
            {
                currDelay += currDelay * (animation.Ref.Interval + 1);

                if (animation.Ref.FramesCount == 0)
                    animation.Ref.FramesCount = fc;
                else
                    fc = animation.Ref.FramesCount;

                if (animation.Ref.ForwardDirection && frameIndex >= fc)
                    frameIndex = 0;
                else if (!animation.Ref.ForwardDirection && frameIndex < 0)
                    frameIndex = fc == 0 ? 0 : frames.Length - 1;
                else
                    goto SKIP;

                animation.Ref.RepeatMode = Math.Max(0, animation.Ref.RepeatMode - 1);
                if (animation.Ref.RepeatMode >= 0)
                    goto SKIP;

                if (animation.Ref.Repeat)
                {
                    animation.Ref.RepeatModeCount = animation.Ref.RepeatMode;
                    animation.Ref.Repeat = false;
                }
                else
                {
                    animation.Ref.Run = true;
                }
            SKIP:;
            }
            else
            {
                if (frameIndex >= fc)
                {
                    frameIndex = 0;
                    animation.Ref.Run = false;
                }
            }

            animation.Ref.Index = frames.IsEmpty ? 0 : frameIndex % frames.Length;
            animation.Ref.Time = time.Total + currDelay;
        }
    }


    private static readonly ushort[] HANDS_BASE_ANIMID =
    {
        0x0263, 0x0264, 0x0265, 0x0266, 0x0267, 0x0268, 0x0269, 0x026D, 0x0270,
        0x0272, 0x0274, 0x027A, 0x027C, 0x027F, 0x0281, 0x0286, 0x0288, 0x0289,
        0x028B, 0
    };

    private static readonly ushort[] HAND2_BASE_ANIMID =
    {
        0x0240, 0x0241, 0x0242, 0x0243, 0x0244, 0x0245, 0x0246, 0x03E0, 0x03E1, 0
    };

    private static byte GetAnimationGroup
    (
        ClientVersion clientVersion,
        ClassicUO.Assets.AnimationsLoader animationLoader,
        ClassicUO.Renderer.Animations.Animations animations,
        ushort graphic,
        Direction direction,
        bool isWalking,
        bool isMounted,
        bool isEquipment,
        Flags mobFlags,
        bool fromServer,
        byte action
    )
    {
        if (graphic >= animations.MaxAnimationCount)
        {
            return 0;
        }

        var isParent = true;
        var isGargoyle = Races.IsGargoyle(clientVersion, graphic);
        var isDead = false;
        var isRun = direction.HasFlag(Direction.Running);

        var originalType = animations.GetAnimType(graphic);
        animations.ConvertBodyIfNeeded(ref graphic, isParent);
        var type = animations.GetAnimType(graphic);
        var flags = animations.GetAnimFlags(graphic);

        var uop = (flags & AnimationFlags.UseUopAnimation) != 0;

        if (fromServer && action != 0xFF)
        {
            ushort v13 = action;

            if (v13 == 12)
            {
                if (!(type == AnimationGroupsType.Human || type == AnimationGroupsType.Equipment || (flags & AnimationFlags.Unknown1000) != 0))
                {
                    if (type != AnimationGroupsType.Monster)
                    {
                        if (type == AnimationGroupsType.Human || type == AnimationGroupsType.Equipment)
                        {
                            v13 = 16;
                        }
                        else
                        {
                            v13 = 5;
                        }
                    }
                    else
                    {
                        v13 = 4;
                    }
                }
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type != AnimationGroupsType.SeaMonster)
                {
                    if (type == AnimationGroupsType.Animal)
                    {
                        if (IsReplacedObjectAnimation(animationLoader, 0, v13))
                        {
                            originalType = AnimationGroupsType.Unknown;
                        }

                        if (v13 > 12)
                        {
                            switch (v13)
                            {
                                case 23:
                                    v13 = 0;

                                    break;

                                case 24:
                                    v13 = 1;

                                    break;
                                case 26:

                                    if (!animations.AnimationExists(graphic, 26) || (mobFlags.HasFlag(Flags.WarMode) && animations.AnimationExists(graphic, 9)))
                                    {
                                        v13 = 9;
                                    }

                                    break;

                                case 28:

                                    v13 = (ushort)(animations.AnimationExists(graphic, 10) ? 10 : 5);

                                    break;

                                default:
                                    v13 = 2;

                                    break;
                            }
                        }

                        //if (v13 > 12)
                        //    v13 = 0; // 2
                    }
                    else
                    {
                        if (IsReplacedObjectAnimation(animationLoader, 1, v13))
                        {
                            // LABEL_190:

                            LABEL_190(flags, ref v13);

                            return (byte)v13;
                        }
                    }
                }
                else
                {
                    if (IsReplacedObjectAnimation(animationLoader, 3, v13))
                    {
                        originalType = AnimationGroupsType.Unknown;
                    }

                    if (v13 > 8)
                    {
                        v13 = 2;
                    }
                }
            }
            else
            {
                if (IsReplacedObjectAnimation(animationLoader, 2, v13))
                {
                    originalType = AnimationGroupsType.Unknown;
                }


                if (!animations.AnimationExists(graphic, (byte)v13))
                {
                    v13 = 1;
                }

                if ((flags & AnimationFlags.UseUopAnimation) != 0)
                {
                    // do nothing?
                }
                else if (v13 > 21)
                {
                    v13 = 1;
                }
            }


            if (originalType == AnimationGroupsType.Unknown)
            {
                LABEL_190(flags, ref v13);

                return (byte)v13;
            }

            if (originalType != 0)
            {
                if (originalType == AnimationGroupsType.Animal && type == AnimationGroupsType.Monster)
                {
                    switch (v13)
                    {
                        case 0:
                            v13 = 0;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 1:
                            v13 = 19;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 3:
                            v13 = 11;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 5:
                            v13 = 4;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 6:
                            v13 = 5;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 7:
                        case 11:
                            v13 = 10;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 8:
                            v13 = 2;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 9:
                            v13 = 17;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 10:
                            v13 = 18;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 12:
                            v13 = 3;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;
                    }

                    // LABEL_187
                    v13 = 1;
                }

                LABEL_190(flags, ref v13);

                return (byte)v13;
            }

            switch (type)
            {
                case AnimationGroupsType.Human:

                    switch (v13)
                    {
                        case 0:
                            v13 = 0;

                            goto LABEL_189;

                        case 2:
                            v13 = 21;

                            goto LABEL_189;

                        case 3:
                            v13 = 22;

                            goto LABEL_189;

                        case 4:
                        case 9:
                            v13 = 9;

                            goto LABEL_189;

                        case 5:
                            //LABEL_163:
                            v13 = 11;

                            goto LABEL_189;

                        case 6:
                            v13 = 13;

                            goto LABEL_189;

                        case 7:
                            //LABEL_165:
                            v13 = 18;

                            goto LABEL_189;

                        case 8:
                            //LABEL_172:
                            v13 = 19;

                            goto LABEL_189;

                        case 10:
                        case 21:
                            v13 = 20;

                            goto LABEL_189;

                        case 12:
                        case 14:
                            v13 = 16;

                            goto LABEL_189;

                        case 13:
                            //LABEL_164:
                            v13 = 17;

                            goto LABEL_189;

                        case 15:
                        case 16:
                            v13 = 30;

                            goto LABEL_189;

                        case 17:
                            v13 = 5;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 18:
                            v13 = 6;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 19:
                            v13 = 1;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;
                    }

                    //LABEL_161:
                    v13 = 4;

                    goto LABEL_189;

                case AnimationGroupsType.Animal:

                    switch (v13)
                    {
                        case 0:
                            v13 = 0;

                            goto LABEL_189;

                        case 2:
                            v13 = 8;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 3:
                            v13 = 12;

                            goto LABEL_189;

                        case 4:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 12:
                        case 13:
                        case 14:
                            v13 = 5;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 5:
                            v13 = 6;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 10:
                        case 21:
                            v13 = 7;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 11:
                            v13 = 3;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;

                        case 17:
                            //LABEL_170:
                            v13 = 9;

                            goto LABEL_189;

                        case 18:
                            //LABEL_162:
                            v13 = 10;

                            goto LABEL_189;

                        case 19:
                            v13 = 1;
                            LABEL_190(flags, ref v13);

                            return (byte)v13;
                    }

                    v13 = 2;
                    LABEL_190(flags, ref v13);

                    return (byte)v13;

                case AnimationGroupsType.SeaMonster:

                    switch (v13)
                    {
                        case 0:
                            //LABEL_182:
                            v13 = 0;

                            goto LABEL_189;

                        case 2:
                        case 3:
                            //LABEL_178:
                            v13 = 8;

                            goto LABEL_189;

                        case 4:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 12:
                        case 13:
                        case 14:
                            //LABEL_183:
                            v13 = 5;

                            goto LABEL_189;

                        case 5:
                            //LABEL_184:
                            v13 = 6;

                            goto LABEL_189;

                        case 10:
                        case 21:
                            //LABEL_185:
                            v13 = 7;

                            goto LABEL_189;

                        case 17:
                            //LABEL_186:
                            v13 = 3;

                            goto LABEL_189;

                        case 18:
                            v13 = 4;

                            goto LABEL_189;

                        case 19:
                            LABEL_190(flags, ref v13);

                            return (byte)v13;
                    }

                    v13 = 2;
                    LABEL_190(flags, ref v13);

                    return (byte)v13;

                default:
                LABEL_189:

                    LABEL_190(flags, ref v13);

                    return (byte)v13;
            }

            // LABEL_188
            v13 = 2;

            LABEL_190(flags, ref v13);

            return (byte)v13;

            static void LABEL_222(AnimationFlags flags, ref ushort v13)
            {
                if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
                {
                    switch (v13)
                    {
                        case 0:
                            v13 = 0;

                            goto LABEL_243;

                        case 1:
                            v13 = 19;

                            goto LABEL_243;

                        case 5:
                        case 6:

                            if ((flags & AnimationFlags.IdleAt8Frame) != 0)
                            {
                                v13 = 4;
                            }
                            else
                            {
                                v13 = (ushort)(6 - (Random.Shared.Next() % 2 != 0 ? 1 : 0));
                            }

                            goto LABEL_243;

                        case 8:
                            v13 = 2;

                            goto LABEL_243;

                        case 9:
                            v13 = 17;

                            goto LABEL_243;

                        case 10:
                            v13 = 18;

                            if ((flags & AnimationFlags.IdleAt8Frame) != 0)
                            {
                                v13--;
                            }

                            goto LABEL_243;

                        case 12:
                            v13 = 3;

                            goto LABEL_243;
                    }

                    // LABEL_241
                    v13 = 1;
                }
                else
                {
                    if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
                    {
                        switch (v13)
                        {
                            case 0:
                                // LABEL_232
                                v13 = 0;

                                break;

                            case 2:
                                v13 = 8;

                                break;

                            case 3:
                                v13 = 12;

                                break;

                            case 4:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 12:
                            case 13:
                            case 14:
                                v13 = 5;

                                break;

                            case 5:
                                v13 = 6;

                                break;

                            case 10:
                            case 21:
                                v13 = 7;

                                break;

                            case 11:
                                //LABEL_238:
                                v13 = 3;

                                break;

                            case 17:
                                v13 = 9;

                                break;

                            case 18:
                                v13 = 10;

                                break;

                            case 19:

                                v13 = 1;

                                break;

                            default:
                                //LABEL_242:
                                v13 = 2;

                                break;
                        }
                    }
                }

            LABEL_243:
                v13 = (ushort)(v13 & 0x7F);

                //if (v13 > 34)
                //    v13 = 0;
            }

            static void LABEL_190(AnimationFlags flags, ref ushort v13)
            {
                if ((flags & AnimationFlags.Unknown80) != 0 && v13 == 4)
                {
                    v13 = 5;
                }

                if ((flags & AnimationFlags.Unknown200) != 0)
                {
                    if (v13 - 7 > 9)
                    {
                        if (v13 == 19)
                        {
                            //LABEL_196
                            v13 = 0;
                        }
                        else if (v13 > 19)
                        {
                            v13 = 1;
                        }

                        LABEL_222(flags, ref v13);

                        return;
                    }
                }
                else
                {
                    if ((flags & AnimationFlags.Unknown100) != 0)
                    {
                        switch (v13)
                        {
                            case 10:
                            case 15:
                            case 16:
                                v13 = 1;
                                LABEL_222(flags, ref v13);

                                return;

                            case 11:
                                v13 = 17;
                                LABEL_222(flags, ref v13);

                                return;
                        }

                        LABEL_222(flags, ref v13);

                        return;
                    }

                    if ((flags & AnimationFlags.Unknown1) != 0)
                    {
                        if (v13 == 21)
                        {
                            v13 = 10;
                        }

                        LABEL_222(flags, ref v13);

                        return;
                    }

                    if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) == 0)
                    {
                        //LABEL_222:
                        LABEL_222(flags, ref v13);

                        return;
                    }

                    switch (v13)
                    {
                        case 0:
                            v13 = 0;

                            break;

                        case 2:
                            v13 = 21;
                            LABEL_222(flags, ref v13);

                            return;

                        case 3:
                            v13 = 22;
                            LABEL_222(flags, ref v13);

                            return;

                        case 4:
                        case 9:
                            v13 = 9;
                            LABEL_222(flags, ref v13);

                            return;

                        case 5:
                            v13 = 11;
                            LABEL_222(flags, ref v13);

                            return;

                        case 6:
                            v13 = 13;
                            LABEL_222(flags, ref v13);

                            return;

                        case 7:
                            v13 = 18;
                            LABEL_222(flags, ref v13);

                            return;

                        case 8:
                            v13 = 19;
                            LABEL_222(flags, ref v13);

                            return;

                        case 10:
                        case 21:
                            v13 = 20;
                            LABEL_222(flags, ref v13);

                            return;

                        case 11:
                            v13 = 3;
                            LABEL_222(flags, ref v13);

                            return;

                        case 12:
                        case 14:
                            v13 = 16;
                            LABEL_222(flags, ref v13);

                            return;

                        case 13:
                            //LABEL_202:
                            v13 = 17;
                            LABEL_222(flags, ref v13);

                            return;

                        case 15:
                        case 16:
                            v13 = 30;
                            LABEL_222(flags, ref v13);

                            return;

                        case 17:
                            v13 = 5;
                            LABEL_222(flags, ref v13);

                            return;

                        case 18:
                            v13 = 6;
                            LABEL_222(flags, ref v13);

                            return;

                        case 19:
                            //LABEL_201:
                            v13 = 1;
                            LABEL_222(flags, ref v13);

                            return;
                    }
                }

                v13 = 4;

                LABEL_222(flags, ref v13);
            }
        }

        byte result = (byte)(isParent ? 0xFF : 0);

        switch (type)
        {
            case AnimationGroupsType.Animal:

                if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
                {
                    CalculateHight
                    (
                        animations,
                        graphic,
                        mobFlags,
                        flags,
                        isRun,
                        isWalking,
                        ref result
                    );
                }
                else
                {
                    if (!isWalking)
                    {
                        if (result == 0xFF)
                        {
                            if ((flags & AnimationFlags.UseUopAnimation) != 0)
                            {
                                if (mobFlags.HasFlag(Flags.WarMode) && animations.AnimationExists(graphic, 1))
                                {
                                    result = 1;
                                }
                                else
                                {
                                    result = 25;
                                }
                            }
                            else
                            {
                                result = 2;
                            }
                        }
                    }
                    else if (isRun)
                    {
                        if ((flags & AnimationFlags.UseUopAnimation) != 0)
                        {
                            result = 24;
                        }
                        else
                        {
                            result = animations.AnimationExists(graphic, 1) ? (byte)1 : (byte)2;
                        }
                    }
                    else if ((flags & AnimationFlags.UseUopAnimation) != 0 && (!mobFlags.HasFlag(Flags.WarMode) || !animations.AnimationExists(graphic, 0)))
                    {
                        result = 22;
                    }
                    else
                    {
                        result = 0;
                    }
                }

                break;

            case AnimationGroupsType.Monster:
                CalculateHight
                (
                    animations,
                    graphic,
                    mobFlags,
                    flags,
                    isRun,
                    isWalking,
                    ref result
                );

                break;

            case AnimationGroupsType.SeaMonster:

                if (!isWalking)
                {
                    if (result == 0xFF)
                    {
                        result = 2;
                    }
                }
                else if (isRun)
                {
                    result = 1;
                }
                else
                {
                    result = 0;
                }

                break;

            default:
                {
                    if (!isWalking)
                    {
                        if (result == 0xFF)
                        {
                            if (isMounted)
                            {
                                result = 25;
                            }
                            else if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                            {
                                if (mobFlags.HasFlag(Flags.WarMode))
                                {
                                    result = 65;
                                }
                                else
                                {
                                    result = 64;
                                }
                            }
                            else if (!mobFlags.HasFlag(Flags.WarMode) || isDead)
                            {
                                if (uop && type == AnimationGroupsType.Equipment && animations.AnimationExists(graphic, 37))
                                {
                                    result = 37;
                                }
                                else
                                {
                                    result = 4;
                                }
                            }
                            else
                            {
                                if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                                {
                                    result = 64;
                                }
                                else
                                {
                                    result = 7;
                                }
                            }
                        }
                    }
                    else if (isMounted)
                    {
                        if (isRun)
                        {
                            result = 24;
                        }
                        else
                        {
                            result = 23;
                        }
                    }
                    else if (isRun || !mobFlags.HasFlag(Flags.WarMode) || isDead)
                    {
                        if ((flags & AnimationFlags.UseUopAnimation) != 0)
                        {
                            // i'm not sure here if it's necessary the isgargoyle
                            if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                            {
                                if (isRun)
                                {
                                    result = 63;
                                }
                                else
                                {
                                    result = 62;
                                }
                            }
                            else
                            {
                                if (isRun && animations.AnimationExists(graphic, 24))
                                {
                                    result = 24;
                                }
                                else
                                {
                                    if (isRun)
                                    {
                                        if (uop && type == AnimationGroupsType.Equipment && !animations.AnimationExists(graphic, 2))
                                        {
                                            result = 3;
                                        }
                                        else
                                        {
                                            result = 2;
                                        }
                                    }
                                    else
                                    {
                                        if (uop && type == AnimationGroupsType.Equipment && !animations.AnimationExists(graphic, 0))
                                        {
                                            result = 1;
                                        }
                                        else
                                        {
                                            result = 0;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (isRun)
                            {
                                result = 2;
                            }
                            else
                            {
                                result = 0;
                            }
                        }
                    }
                    else if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    {
                        result = 62;
                    }
                    else
                    {
                        result = 15;
                    }


                    //Item hand2 = mobile.FindItemByLayer(Layer.TwoHanded);

                    //if (!isWalking)
                    //{
                    //    if (result == 0xFF)
                    //    {
                    //        bool haveLightAtHand2 = hand2 != null && hand2.ItemData.IsLight && hand2.ItemData.AnimID == graphic;

                    //        if (isMounted)
                    //        {
                    //            if (haveLightAtHand2)
                    //            {
                    //                result = 28;
                    //            }
                    //            else
                    //            {
                    //                result = 25;
                    //            }
                    //        }
                    //        else if (isGargoyle && mobFlags.HasFlag(Flags.Flying)) // TODO: what's up when it is dead?
                    //        {
                    //            if (mobFlags.HasFlag(Flags.WarMode))
                    //            {
                    //                result = 65;
                    //            }
                    //            else
                    //            {
                    //                result = 64;
                    //            }
                    //        }
                    //        else if (!mobFlags.HasFlag(Flags.WarMode) || isDead)
                    //        {
                    //            if (haveLightAtHand2)
                    //            {
                    //                // TODO: UOP EQUIPMENT ?
                    //                result = 0;
                    //            }
                    //            else
                    //            {
                    //                if (uop && type == AnimationGroupsType.EQUIPMENT && animations.AnimationExists(graphic, 37))
                    //                {
                    //                    result = 37;
                    //                }
                    //                else
                    //                {
                    //                    result = 4;
                    //                }
                    //            }
                    //        }
                    //        else if (haveLightAtHand2)
                    //        {
                    //            // TODO: UOP EQUIPMENT ?

                    //            result = 2;
                    //        }
                    //        else
                    //        {
                    //            unsafe
                    //            {
                    //                ushort* handAnimIDs = stackalloc ushort[2];
                    //                Item hand1 = mobile.FindItemByLayer(Layer.OneHanded);

                    //                if (hand1 != null)
                    //                {
                    //                    handAnimIDs[0] = hand1.ItemData.AnimID;
                    //                }

                    //                if (hand2 != null)
                    //                {
                    //                    handAnimIDs[1] = hand2.ItemData.AnimID;
                    //                }


                    //                if (hand1 == null)
                    //                {
                    //                    if (hand2 != null)
                    //                    {
                    //                        if (uop && type == AnimationGroupsType.EQUIPMENT && !animations.AnimationExists(graphic, 7))
                    //                        {
                    //                            result = 8;
                    //                        }
                    //                        else
                    //                        {
                    //                            result = 7;
                    //                        }

                    //                        for (int i = 0; i < 2; i++)
                    //                        {
                    //                            if (handAnimIDs[i] >= 0x0263 && handAnimIDs[i] <= 0x028B)
                    //                            {
                    //                                for (int k = 0; k < HANDS_BASE_ANIMID.Length; k++)
                    //                                {
                    //                                    if (handAnimIDs[i] == HANDS_BASE_ANIMID[k])
                    //                                    {
                    //                                        result = 8;
                    //                                        i = 2;

                    //                                        break;
                    //                                    }
                    //                                }
                    //                            }
                    //                        }
                    //                    }
                    //                    else if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    //                    {
                    //                        result = 64;
                    //                    }
                    //                    else
                    //                    {
                    //                        result = 7;
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    result = 7;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //else if (isMounted)
                    //{
                    //    if (isRun)
                    //    {
                    //        result = 24;
                    //    }
                    //    else
                    //    {
                    //        result = 23;
                    //    }
                    //}
                    ////else if (EquippedGraphic0x3E96)
                    ////{

                    ////}
                    //else if (isRun || !mobFlags.HasFlag(Flags.WarMode) || isDead)
                    //{
                    //    if ((flags & AnimationFlags.UseUopAnimation) != 0)
                    //    {
                    //        // i'm not sure here if it's necessary the isgargoyle
                    //        if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    //        {
                    //            if (isRun)
                    //            {
                    //                result = 63;
                    //            }
                    //            else
                    //            {
                    //                result = 62;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (isRun && animations.AnimationExists(graphic, 24))
                    //            {
                    //                result = 24;
                    //            }
                    //            else
                    //            {
                    //                if (isRun)
                    //                {
                    //                    if (uop && type == AnimationGroupsType.EQUIPMENT && !animations.AnimationExists(graphic, 2))
                    //                    {
                    //                        result = 3;
                    //                    }
                    //                    else
                    //                    {
                    //                        result = 2;

                    //                        if (isGargoyle)
                    //                        {
                    //                            hand2 = mobile.FindItemByLayer(Layer.OneHanded);
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    if (uop && type == AnimationGroupsType.EQUIPMENT && !animations.AnimationExists(graphic, 0))
                    //                    {
                    //                        result = 1;
                    //                    }
                    //                    else
                    //                    {
                    //                        result = 0;
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (isRun)
                    //        {
                    //            result = (byte)(hand2 != null ? 3 : 2);
                    //        }
                    //        else
                    //        {
                    //            result = (byte)(hand2 != null ? 1 : 0);
                    //        }
                    //    }

                    //    if (hand2 != null)
                    //    {
                    //        ushort hand2Graphic = hand2.ItemData.AnimID;

                    //        if (hand2Graphic < 0x0240 || hand2Graphic > 0x03E1)
                    //        {
                    //            if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    //            {
                    //                if (isRun)
                    //                {
                    //                    result = 63;
                    //                }
                    //                else
                    //                {
                    //                    result = 62;
                    //                }
                    //            }
                    //            else
                    //            {
                    //                if (isRun)
                    //                {
                    //                    result = 3;
                    //                }
                    //                else
                    //                {
                    //                    result = 1;
                    //                }
                    //            }
                    //        }
                    //        else
                    //        {
                    //            for (int i = 0; i < HAND2_BASE_ANIMID.Length; i++)
                    //            {
                    //                if (HAND2_BASE_ANIMID[i] == hand2Graphic)
                    //                {
                    //                    if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    //                    {
                    //                        if (isRun)
                    //                        {
                    //                            result = 63;
                    //                        }
                    //                        else
                    //                        {
                    //                            result = 62;
                    //                        }
                    //                    }
                    //                    else
                    //                    {
                    //                        if (isRun)
                    //                        {
                    //                            result = 3;
                    //                        }
                    //                        else
                    //                        {
                    //                            result = 1;
                    //                        }
                    //                    }

                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //else if (isGargoyle && mobFlags.HasFlag(Flags.Flying))
                    //{
                    //    result = 62;
                    //}
                    //else
                    //{
                    //    result = 15;
                    //}



                    break;
                }
        }

        return result;

        static bool IsReplacedObjectAnimation(AnimationsLoader animationLoader, byte anim, ushort v13)
        {
            if (anim < animationLoader.GroupReplaces.Length)
            {
                foreach (var tuple in animationLoader.GroupReplaces[anim])
                {
                    if (tuple.Item1 == v13)
                    {
                        return tuple.Item2 != 0xFF;
                    }
                }
            }

            return false;
        }

        static void CalculateHight
        (
            ClassicUO.Renderer.Animations.Animations animations,
            ushort graphic,
            Flags mobFlags,
            AnimationFlags flags,
            bool isrun,
            bool iswalking,
            ref byte result
        )
        {
            if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) != 0)
            {
                if (result == 0xFF)
                {
                    result = 0;
                }
            }
            else if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
            {
                if (!iswalking)
                {
                    if (result == 0xFF)
                    {
                        result = 2;
                    }
                }
                else if (isrun)
                {
                    result = 1;
                }
                else
                {
                    result = 0;
                }
            }
            else
            {
                if (mobFlags.HasFlag(Flags.Flying))
                {
                    result = 19;
                }
                else if (!iswalking)
                {
                    if (result == 0xFF)
                    {
                        if ((flags & AnimationFlags.IdleAt8Frame) != 0 && animations.AnimationExists(graphic, 8))
                        {
                            result = 8;
                        }
                        else
                        {
                            if ((flags & AnimationFlags.UseUopAnimation) != 0 && !mobFlags.HasFlag(Flags.WarMode))
                            {
                                result = 25;
                            }
                            else
                            {
                                result = 1;
                            }
                        }
                    }
                }
                else if (isrun)
                {
                    if ((flags & AnimationFlags.CanFlying) != 0 && animations.AnimationExists(graphic, 19))
                    {
                        result = 19;
                    }
                    else
                    {
                        if ((flags & AnimationFlags.UseUopAnimation) != 0)
                        {
                            result = 24;
                        }
                        else
                        {
                            result = 0;
                        }
                    }
                }
                else
                {
                    if ((flags & AnimationFlags.UseUopAnimation) != 0 && !mobFlags.HasFlag(Flags.WarMode))
                    {
                        result = 22;
                    }
                    else
                    {
                        result = 0;
                    }
                }
            }
        }
    }
}

public static class Races
{
    public static bool IsGargoyle(ClientVersion clientVersion, ushort graphic)
            => clientVersion >= ClientVersion.CV_7000 && (graphic == 0x029A || graphic == 0x029B);

    public static bool IsHuman(ushort graphic)
        => (graphic >= 0x0190 && graphic <= 0x0193) ||
            (graphic >= 0x00B7 && graphic <= 0x00BA) ||
            (graphic >= 0x025D && graphic <= 0x0260) ||
            graphic == 0x029A || graphic == 0x029B ||
            graphic == 0x02B6 || graphic == 0x02B7 ||
            graphic == 0x03DB || graphic == 0x03DF ||
            graphic == 0x03E2 || graphic == 0x02E8 ||
            graphic == 0x02E9 || graphic == 0x04E5;
}
