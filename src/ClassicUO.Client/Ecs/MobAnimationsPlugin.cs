using System;
using System.Runtime.CompilerServices;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using TinyEcs;

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

    public byte Action;
    public byte MountAction;
    public Direction Direction;
    public uint Time;
    public bool Run;
}

struct MobileFlags
{
    public Flags Value;
}

unsafe struct MobileSteps
{
    public const int COUNT = 10;

    private Game.GameObjects.Mobile.Step _step0;
    private Game.GameObjects.Mobile.Step _step1;
    private Game.GameObjects.Mobile.Step _step2;
    private Game.GameObjects.Mobile.Step _step3;
    private Game.GameObjects.Mobile.Step _step4;
    private Game.GameObjects.Mobile.Step _step5;
    private Game.GameObjects.Mobile.Step _step6;
    private Game.GameObjects.Mobile.Step _step7;
    private Game.GameObjects.Mobile.Step _step8;
    private Game.GameObjects.Mobile.Step _step9;

    public ref Game.GameObjects.Mobile.Step this[int index] => ref (new Span<Game.GameObjects.Mobile.Step>(Unsafe.AsPointer(ref _step0), COUNT)[index]);

    public int Count;
    public uint Time;
}

struct MobileEquipment
{
    uint __MOC;
}

readonly struct MobAnimationsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var mocTime = 0;
        scheduler.AddSystem((Query<(
                Renderable,
                MobAnimation,
                Optional<MobileFlags>,
                Optional<MobileSteps>,
                Optional<MobileEquipment>
                )> query) => {
            query.Each(
            (
                ref Renderable renderable,
                ref MobAnimation animation,
                ref MobileFlags mobFlags,
                ref MobileSteps mobSteps,
                ref MobileEquipment mobEquip
            ) => {
                if (animation.Time >= mocTime) return;

                var flags = Unsafe.IsNullRef(ref mobFlags) ? Flags.None : mobFlags.Value;
                var isWalking = false;
                var iterate = true;
                var realDirection = Direction.NONE; // TODO
                var mirror = false;


                if (!Unsafe.IsNullRef(ref mobSteps))
                {
                    isWalking = mobSteps.Time > mocTime - Constants.WALKING_DELAY;

                    if (mobSteps.Count > 0)
                    {
                        isWalking = true;
                        realDirection = (Direction)mobSteps[0].Direction;
                        if (mobSteps[0].Run)
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

                var hasMount = false;
                if (!Unsafe.IsNullRef(ref mobEquip))
                {
                    // TODO
                }

                animation.Action = 0; // TODO
                animation.Direction = realDirection;

                var dir = (byte)(realDirection & Direction.Mask);

            });
        });
    }
}