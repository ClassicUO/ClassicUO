using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.WorldObjects
{
    [Flags]
    public enum Notoriety : byte
    {
        Unknown = 0x00,
        Innocent = 0x01,
        Ally = 0x02,
        Gray = 0x03,
        Criminal = 0x04,
        Enemy = 0x05,
        Murderer = 0x06,
        Invulnerable = 0x07
    }

    public enum RaceType : byte
    {
        HUMAN = 1,
        ELF,
        GARGOYLE
    }

    public partial class Mobile : Entity
    {
        protected const int MAX_STEP_COUNT = 5;
        protected const int TURN_DELAY = 100;
        protected const int WALKING_DELAY = 750;
        protected const int PLAYER_WALKING_DELAY = 150;

        private static readonly byte[,] _animAssociateTable =
        {
            {
                (byte) LOW_ANIMATION_GROUP.LAG_WALK, (byte) HIGHT_ANIMATION_GROUP.HAG_WALK,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_WALK, (byte) HIGHT_ANIMATION_GROUP.HAG_WALK,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_RUN, (byte) HIGHT_ANIMATION_GROUP.HAG_FLY,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_RUN, (byte) HIGHT_ANIMATION_GROUP.HAG_FLY,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_STAND, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_2, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_STAND, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_STAND, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_3,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_ONEHANDED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_1
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_2
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_3,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_DOWN
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_WIDE
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_JAB
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_WALK, (byte) HIGHT_ANIMATION_GROUP.HAG_WALK,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_CAST_DIRECTED
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_3,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_CAST_AREA
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_BOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_CROSSBOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_GET_HIT_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_GET_HIT
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_DIE_1, (byte) HIGHT_ANIMATION_GROUP.HAG_DIE_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_DIE_1
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_DIE_2, (byte) HIGHT_ANIMATION_GROUP.HAG_DIE_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_DIE_2
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_WALK, (byte) HIGHT_ANIMATION_GROUP.HAG_WALK,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_RUN, (byte) HIGHT_ANIMATION_GROUP.HAG_FLY,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_STAND, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK_BOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK_CROSSBOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_SLAP_HORSE
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_TURN
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_WALK, (byte) HIGHT_ANIMATION_GROUP.HAG_WALK,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_AND_WALK
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_EMOTE_BOW
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_EAT, (byte) HIGHT_ANIMATION_GROUP.HAG_STAND,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_EMOTE_SALUTE
            },
            {
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3
            }
        };

        protected readonly Deque<Step> _steps = new Deque<Step>();
        private ushort _hits;
        private ushort _hitsMax;
        private bool _isDead;
        private bool _isSA_Poisoned;
        private ushort _mana;
        private ushort _manaMax;
        private Notoriety _notoriety;
        private RaceType _race;
        private bool _renamable;
        private ushort _stamina;
        private ushort _staminaMax;

        public Mobile(in Serial serial) : base(serial)
        {
            _lastAnimationChangeTime = World.Ticks;
        }

        public new MobileView ViewObject => (MobileView) base.ViewObject;


        public RaceType Race
        {
            get => _race;
            set
            {
                if (_race != value)
                {
                    _race = value;
                    _delta |= Delta.Appearance;
                }
            }
        }

        public ushort Hits
        {
            get => _hits;
            set
            {
                if (_hits != value)
                {
                    _hits = value;
                    _delta |= Delta.Hits;
                }
            }
        }

        public ushort HitsMax
        {
            get => _hitsMax;
            set
            {
                if (_hitsMax != value)
                {
                    _hitsMax = value;
                    _delta |= Delta.Hits;
                }
            }
        }

        public ushort Mana
        {
            get => _mana;
            set
            {
                if (_mana != value)
                {
                    _mana = value;
                    _delta |= Delta.Mana;
                }
            }
        }

        public ushort ManaMax
        {
            get => _manaMax;
            set
            {
                if (_manaMax != value)
                {
                    _manaMax = value;
                    _delta |= Delta.Mana;
                }
            }
        }

        public ushort Stamina
        {
            get => _stamina;
            set
            {
                if (_stamina != value)
                {
                    _stamina = value;
                    _delta |= Delta.Stamina;
                }
            }
        }

        public ushort StaminaMax
        {
            get => _staminaMax;
            set
            {
                if (_staminaMax != value)
                {
                    _staminaMax = value;
                    _delta |= Delta.Stamina;
                }
            }
        }

        public Notoriety Notoriety
        {
            get => _notoriety;
            set
            {
                if (_notoriety != value)
                {
                    _notoriety = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        //public bool WarMode
        //{
        //    get { return _warMode; }
        //    set
        //    {
        //        if (_warMode != value)
        //        {
        //            _warMode = value;
        //            _delta |= Delta.Attributes;
        //        }
        //    }
        //}

        public bool Renamable
        {
            get => _renamable;
            set
            {
                if (_renamable != value)
                {
                    _renamable = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public bool Paralyzed => ((byte) Flags & 0x01) != 0;
        public bool YellowBar => ((byte) Flags & 0x08) != 0;

        public bool Poisoned => FileManager.ClientVersion >= ClientVersions.CV_7000
            ? _isSA_Poisoned
            : ((byte) Flags & 0x04) != 0;

        public bool Hidden => ((byte) Flags & 0x80) != 0;

        public bool IsDead
        {
            get => MathHelper.InRange(Graphic, 0x0192, 0x0193) || MathHelper.InRange(Graphic, 0x025F, 0x0260) ||
                   MathHelper.InRange(Graphic, 0x02B6, 0x02B7) || _isDead;
            set => _isDead = value;
        }

        public bool IsFlying =>
            FileManager.ClientVersion >= ClientVersions.CV_7000 && ((byte) Flags.Flying & 0x04) != 0;

        public virtual bool InWarMode
        {
            get => ((byte) Flags & 0x40) != 0;
            set => throw new Exception();
        }

        public bool IsHuman =>
            MathHelper.InRange(Graphic, 0x0190, 0x0193)
            || MathHelper.InRange(Graphic, 0x00B7, 0x00BA)
            || MathHelper.InRange(Graphic, 0x025D, 0x0260)
            || MathHelper.InRange(Graphic, 0x029A, 0x029B)
            || MathHelper.InRange(Graphic, 0x02B6, 0x02B7)
            || Graphic == 0x03DB || Graphic == 0x03DF || Graphic == 0x03E2;

        public override bool Exists => World.Contains(Serial);


        public Item[] Equipment { get; } = new Item[(int) Layer.Bank + 1];


        public bool IsMounted => Equipment[(int) Layer.Mount] != null;
        public bool IsRunning => (Direction & Direction.Running) == Direction.Running;

        public byte AnimationInterval { get; set; }
        public byte AnimationFrameCount { get; set; }
        public byte AnimationRepeatMode { get; set; } = 1;
        public bool AnimationRepeat { get; set; }
        public bool AnimationFromServer { get; set; }
        public bool AnimationDirection { get; set; }

        public Vector3 Offset { get; set; }

        public long LastStepTime { get; set; }

        public virtual bool IsWalking => LastStepTime > World.Ticks - WALKING_DELAY;
        public byte AnimationGroup { get; set; } = 0xFF;
        internal bool IsMoving => _steps.Count > 0;

        public event EventHandler HitsChanged;
        public event EventHandler ManaChanged;
        public event EventHandler StaminaChanged;


        protected override View CreateView()
        {
            return new MobileView(this);
        }

        public void SetSAPoison(in bool value)
        {
            _isSA_Poisoned = value;
        }


        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Hits))
                HitsChanged.Raise(this);

            if (d.HasFlag(Delta.Mana))
                ManaChanged.Raise(this);

            if (d.HasFlag(Delta.Stamina))
                StaminaChanged.Raise(this);
        }


        public void ClearSteps()
        {
            _steps.Clear();
            Offset = Vector3.Zero;
        }


        public void GetNextZ(out int x, out int y, out sbyte z, out Direction dir)
        {
            if (_steps.Count > 0)
            {
                var step = _steps.Front();
                dir = (Direction)step.Direction;
                x = step.X;
                y = step.Y;
                z = step.Z;
                return;
            }
            dir = Direction;
            x = Position.X;
            y = Position.Y;
            z = Position.Z;
        }

        public bool EnqueueStep(in int x, in int y, in sbyte z, in Direction direction, in bool run)
        {
            //if (Deferred != null)
            //    Deferred.Tile = null;

            if (_steps.Count >= MAX_STEP_COUNT) return false;

            int endX = 0, endY = 0;
            sbyte endZ = 0;
            Direction endDir = Direction.NONE;

            GetEndPosition(ref endX, ref endY, ref endZ, ref endDir);

            if (endX == x && endY == y && endZ == z && endDir == direction)
                return true;

            if (!IsMoving)
                LastStepTime = World.Ticks;

            Direction moveDir = CalculateDirection(endX, endY, x, y);

            Step step = new Step();

            if (moveDir != Direction.NONE)
            {
                if (moveDir != endDir)
                {
                    step.X = endX;
                    step.Y = endY;
                    step.Z = endZ;
                    step.Direction = (byte) moveDir;
                    step.Run = run;

                    _steps.AddToBack(step);
                }

                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte) moveDir;
                step.Run = run;
                _steps.AddToBack(step);
            }


            if (moveDir != direction)
            {
                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte) direction;
                step.Run = run;
                _steps.AddToBack(step);
            }

            return true;
        }

        private static Direction CalculateDirection(in int curX, in int curY, in int newX, in int newY)
        {
            int deltaX = newX - curX;
            int deltaY = newY - curY;

            if (deltaX > 0)
            {
                if (deltaY > 0)
                    return Direction.Down;
                if (deltaY == 0)
                    return Direction.East;
                return Direction.Right;
            }

            if (deltaX == 0)
            {
                if (deltaY > 0)
                    return Direction.South;
                return deltaY == 0 ? Direction.NONE : Direction.North;
            }

            if (deltaY > 0)
                return Direction.Left;
            return deltaY == 0 ? Direction.West : Direction.Up;
        }

        internal void GetEndPosition(ref int x, ref int y, ref sbyte z, ref Direction dir)
        {
            if (_steps.Count <= 0)
            {
                x = Position.X;
                y = Position.Y;
                z = Position.Z;
                dir = Direction;
            }
            else
            {
                Step step = _steps.Back();
                x = step.X;
                y = step.Y;
                z = step.Z;
                dir = (Direction) step.Direction;
            }
        }

      
        public void SetAnimation(in byte id, in byte interval = 0, in byte frameCount = 0, in byte repeatCount = 0,
            in bool repeat = false, in bool frameDirection = false)
        {
            AnimationGroup = id;
            AnimIndex = 0;
            AnimationInterval = interval;
            AnimationFrameCount = frameCount;
            AnimationRepeatMode = repeatCount;
            AnimationRepeat = repeat;
            AnimationDirection = frameDirection;
            AnimationFromServer = false;

            _lastAnimationChangeTime = World.Ticks;
        }
 
        protected virtual bool NoIterateAnimIndex()
        {
            return LastStepTime > (uint) (World.Ticks - WALKING_DELAY) && _steps.Count <= 0;
        }

        public override void ProcessAnimation()
        {
            byte dir = (byte) GetDirectionForAnimation();

            if (_steps.Count > 0)
            {
                bool turnOnly;

                do
                {
                    Step step = _steps.Front();

                    if (AnimationFromServer)
                        SetAnimation(0xFF);

                    int maxDelay = MovementSpeed.TimeToCompleteMovement(this, step.Run) - 15;
                    int delay = (int) World.Ticks - (int) LastStepTime;
                    bool removeStep = delay >= maxDelay;

                    if (step.Direction == (byte) Direction)
                    {
                        float framesPerTile = maxDelay / CHARACTER_ANIMATION_DELAY;
                        float frameOffset = delay / CHARACTER_ANIMATION_DELAY;

                        float x = frameOffset;
                        float y = frameOffset;

                        GetPixelOffset(step.Direction, ref x, ref y, framesPerTile);

                        Offset = new Vector3((sbyte) x, (sbyte) y,
                            (int) ((step.Z - Position.Z) * frameOffset * (4.0f / framesPerTile)));


                        //if (Deferred?.Tile != null)
                        //    Deferred.Tile = null;
                        //Tile tile = World.Map.GetTile(step.X, step.Y);
                        //Deferred = new DeferredEntity(this, step.Z, tile);

                        turnOnly = false;
                    }
                    else
                    {
                        turnOnly = true;
                        removeStep = true;
                    }

                    if (removeStep)
                    {
                        if (this == World.Player)
                        {
                            if (Position.X != step.X || Position.Y != step.Y || Position.Z != step.Z)
                            {
                            }

                            if (Position.Z - step.Z >= 22)
                            {
                                // oUCH!!!!
                            }
                        }


                        //if (Deferred != null)
                        //    Deferred.Tile = null;
                        //Deferred = null;


                        Position = new Position((ushort) step.X, (ushort) step.Y, step.Z);
                        Direction = (Direction) step.Direction;

                        Offset = Vector3.Zero;
                        _steps.RemoveFromFront();

                        LastStepTime = World.Ticks;

                        ProcessDelta();
                    }
                } while (_steps.Count > 0 && turnOnly);
            }


            if (_lastAnimationChangeTime < World.Ticks && !NoIterateAnimIndex())
            {
                sbyte frameIndex = AnimIndex;

                if (AnimationFromServer && !AnimationDirection)
                    frameIndex--;
                else
                    frameIndex++;

                Graphic id = GetGraphicForAnimation();
                int animGroup = GetGroupForAnimation(id);

                Item mount = Equipment[(int) Layer.Mount];
                if (mount != null)
                    switch (animGroup)
                    {
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3:
                            id = mount.GetMountAnimation();
                            animGroup = GetGroupForAnimation(id);
                            break;
                    }

                bool mirror = false;
                Animations.GetAnimDirection(ref dir, ref mirror);

                int currentDelay = (int) CHARACTER_ANIMATION_DELAY;

                if (id < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ref AnimationDirection direction = ref Animations.DataIndex[id].Groups[animGroup].Direction[dir];
                    Animations.AnimID = id;
                    Animations.AnimGroup = (byte) animGroup;
                    Animations.Direction = dir;

                    if (direction.FrameCount == 0)
                        Animations.LoadDirectionGroup(ref direction);

                    if (direction.Address != 0 || direction.IsUOP)
                    {
                        int fc = direction.FrameCount;

                        if (AnimationFromServer)
                        {
                            currentDelay += currentDelay * (AnimationInterval + 1);
                            if (AnimationFrameCount <= 0)
                                AnimationFrameCount = (byte) fc;
                            else
                                fc = AnimationFrameCount;

                            if (AnimationDirection)
                            {
                                if (frameIndex >= fc)
                                {
                                    frameIndex = 0;

                                    if (AnimationRepeat)
                                    {
                                        byte repCount = AnimationRepeatMode;
                                        if (repCount == 2)
                                        {
                                            repCount--;
                                            AnimationRepeatMode = repCount;
                                        }
                                        else if (repCount == 1)
                                        {
                                            SetAnimation(0xFF);
                                        }
                                    }
                                    else
                                    {
                                        SetAnimation(0xFF);
                                    }
                                }
                            }
                            else
                            {
                                if (frameIndex < 0)
                                {
                                    if (fc <= 0)
                                        frameIndex = 0;
                                    else
                                        frameIndex = (sbyte) (fc - 1);

                                    if (AnimationRepeat)
                                    {
                                        byte repCount = AnimationRepeatMode;
                                        if (repCount == 2)
                                        {
                                            repCount--;
                                            AnimationRepeatMode = repCount;
                                        }
                                        else if (repCount == 1)
                                        {
                                            SetAnimation(0xFF);
                                        }
                                    }
                                    else
                                    {
                                        SetAnimation(0xFF);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (frameIndex >= fc) frameIndex = 0;
                        }


                        AnimIndex = frameIndex;
                    }

                    _lastAnimationChangeTime = World.Ticks + currentDelay;
                }
            }
        }


        private static void GetPixelOffset(in byte dir, ref float x, ref float y, in float framesPerTile)
        {
            float step_NESW_D = 44.0f / framesPerTile;
            float step_NESW = 22.0f / framesPerTile;

            int checkX = 22;
            int checkY = 22;

            switch (dir & 7)
            {
                case 0:
                {
                    x *= step_NESW;
                    y *= -step_NESW;
                    break;
                }
                case 1:
                {
                    x *= step_NESW_D;
                    checkX = 44;
                    y = 0.0f;
                    break;
                }
                case 2:
                {
                    x *= step_NESW;
                    y *= step_NESW;
                    break;
                }
                case 3:
                {
                    x = 0.0f;
                    y *= step_NESW_D;
                    checkY = 44;
                    break;
                }
                case 4:
                {
                    x *= -step_NESW;
                    y *= step_NESW;
                    break;
                }
                case 5:
                {
                    x *= -step_NESW_D;
                    checkX = 44;
                    y = 0.0f;
                    break;
                }
                case 6:
                {
                    x *= -step_NESW;
                    y *= -step_NESW;
                    break;
                }
                case 7:
                {
                    x = 0.0f;
                    y *= -step_NESW_D;
                    checkY = 44;
                    break;
                }
            }

            int valueX = (int) x;


            if (Math.Abs(valueX) > checkX)
            {
                if (valueX < 0)
                    x = -checkX;
                else
                    x = checkX;
            }

            int valueY = (int) y;

            if (Math.Abs(valueY) > checkY)
            {
                if (valueY < 0)
                    y = -checkY;
                else
                    y = checkY;
            }
        }




        protected struct Step
        {
            public Step(in int x, in int y, in sbyte z, in byte dir, in bool anim, in bool run, in byte seq)
            {
                X = x;
                Y = y;
                Z = z;
                Direction = dir;
                Anim = anim;
                Run = run;
                Seq = seq;
            }

            public int X, Y;
            public sbyte Z;

            public byte Direction;
            public bool Anim;
            public bool Run;
            public byte Seq;
        }
    }
}