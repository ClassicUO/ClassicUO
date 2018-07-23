using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
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

    public class Mobile : Entity
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

        public bool IsFlying => FileManager.ClientVersion >= ClientVersions.CV_7000 && ((byte)Flags.Flying & 0x04) != 0;

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


        public Item GetItemAtLayer(in Layer layer)
        {
            return Equipment[(int) layer];
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


        protected override void OnTileChanged(int x, int y)
        {
            base.OnTileChanged(x, y);
            if (Tile == null)
                return;
        }


        public void ClearSteps()
        {
            _steps.Clear();
            Offset = Vector3.Zero;
        }

        public bool EnqueueStep(in int x, in int y, in sbyte z, in Direction direction, in bool run)
        {
            if (_steps.Count >= MAX_STEP_COUNT) return false;

            int endX = 0, endY = 0;
            sbyte endZ = 0;
            var endDir = Direction.NONE;

            GetEndPosition(ref endX, ref endY, ref endZ, ref endDir);

            if (endX == x && endY == y && endZ == z && endDir == direction)
                return true;

            if (!IsMoving)
                LastStepTime = World.Ticks;

            var moveDir = CalculateDirection(endX, endY, x, y);

            var step = new Step();

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
            var deltaX = newX - curX;
            var deltaY = newY - curY;

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
                if (deltaY == 0)
                    return Direction.NONE;
                return Direction.North;
            }

            if (deltaY > 0)
                return Direction.Left;
            if (deltaY == 0)
                return Direction.West;
            return Direction.Up;
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
                var step = _steps.Back();
                x = step.X;
                y = step.Y;
                z = step.Z;
                dir = (Direction) step.Direction;
            }
        }

        public void GetAnimationGroup(in ANIMATION_GROUPS group, ref byte animation)
        {
            if ((sbyte) group > 0 && animation < (byte) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT)
                animation = _animAssociateTable[animation, (sbyte) group - 1];
        }

        public byte GetAnimationGroup(in ushort checkGraphic = 0)
        {
            Graphic graphic = checkGraphic;
            if (graphic == 0)
                graphic = GetMountAnimation();

            var groupIndex = Animations.GetGroupIndex(graphic);
            var result = AnimationGroup;

            if (result != 0xFF && (Serial & 0x80000000) <= 0 && (!AnimationFromServer || checkGraphic > 0))
            {
                GetAnimationGroup(groupIndex, ref result);

                if (!Animations.AnimationExists(graphic, result))
                    CorrectAnimationGroup(graphic, groupIndex, ref result);
            }

            var isWalking = IsWalking;
            var isRun = IsRunning;

            if (_steps.Count > 0)
            {
                isWalking = true;
                isRun = _steps.Front().Run;
            }

            if (groupIndex == ANIMATION_GROUPS.AG_LOW)
            {
                if (isWalking)
                {
                    if (isRun)
                        result = (byte) LOW_ANIMATION_GROUP.LAG_RUN;
                    else
                        result = (byte) LOW_ANIMATION_GROUP.LAG_WALK;
                }
                else if (AnimationGroup == 0xFF)
                {
                    result = (byte) LOW_ANIMATION_GROUP.LAG_STAND;
                    AnimIndex = 0;
                }
            }
            else if (groupIndex == ANIMATION_GROUPS.AG_HIGHT)
            {
                if (isWalking)
                {
                    result = (byte) HIGHT_ANIMATION_GROUP.HAG_WALK;
                    if (isRun)
                        if (Animations.AnimationExists(graphic, (byte) HIGHT_ANIMATION_GROUP.HAG_FLY))
                            result = (byte) HIGHT_ANIMATION_GROUP.HAG_FLY;
                }
                else if (AnimationGroup == 0xFF)
                {
                    result = (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;
                    AnimIndex = 0;
                }

                if (graphic == 151)
                    result++;
            }
            else if (groupIndex == ANIMATION_GROUPS.AG_PEOPLE)
            {
                var inWar = InWarMode;

                if (isWalking)
                {
                    if (isRun)
                    {
                        if (GetItemAtLayer(Layer.Mount) != null)
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST;
                        else if (GetItemAtLayer(Layer.LeftHand) != null || GetItemAtLayer(Layer.RightHand) != null)
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED;
                        else
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED;

                        if (!IsHuman && !Animations.AnimationExists(graphic, result))
                        {
                            if (GetItemAtLayer(Layer.Mount) != null)
                            {
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                            }
                            else if ((GetItemAtLayer(Layer.LeftHand) != null ||
                                      GetItemAtLayer(Layer.RightHand) != null) && !IsDead)
                            {
                                if (inWar)
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                                else
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                            }
                            else if (inWar && !IsDead)
                            {
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            }
                            else
                            {
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                            }
                        }
                    }
                    else
                    {
                        if (GetItemAtLayer(Layer.Mount) != null)
                        {
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                        }
                        else if ((GetItemAtLayer(Layer.LeftHand) != null || GetItemAtLayer(Layer.RightHand) != null) &&
                                 !IsDead)
                        {
                            if (inWar)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            else
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                        }
                        else if (inWar && !IsDead)
                        {
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                        }
                        else
                        {
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                        }
                    }
                }
                else if (AnimationGroup == 0xFF)
                {
                    if (GetItemAtLayer(Layer.Mount) != null)
                    {
                        result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND;
                    }
                    else if (inWar && !IsDead)
                    {
                        if (GetItemAtLayer(Layer.LeftHand) != null)
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                        else if (GetItemAtLayer(Layer.RightHand) != null)
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK;
                        else
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                    }
                    else
                    {
                        result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
                    }

                    AnimIndex = 0;
                }

                if (Race == RaceType.GARGOYLE)
                    if (IsFlying)
                    {
                        if (result == 0 || result == 1)
                            result = 62;
                        else if (result == 2 || result == 3)
                            result = 63;
                        else if (result == 4)
                            result = 64;
                        else if (result == 6)
                            result = 66;
                        else if (result == 7 || result == 8)
                            result = 65;
                        else if (result >= 9 && result <= 11)
                            result = 71;
                        else if (result >= 12 && result <= 14)
                            result = 72;
                        else if (result == 15)
                            result = 62;
                        else if (result == 20)
                            result = 77;
                        else if (result == 31)
                            result = 71;
                        else if (result == 34)
                            result = 78;
                        else if (result >= 200 && result <= 259)
                            result = 75;
                        else if (result >= 260 && result <= 270) result = 75;
                    }
            }

            return result;
        }

        private void CorrectAnimationGroup(in ushort graphic, in ANIMATION_GROUPS group, ref byte animation)
        {
            if (group == ANIMATION_GROUPS.AG_LOW)
            {
                switch ((LOW_ANIMATION_GROUP) animation)
                {
                    case LOW_ANIMATION_GROUP.LAG_DIE_2:
                        animation = (byte) LOW_ANIMATION_GROUP.LAG_DIE_1;
                        break;
                    case LOW_ANIMATION_GROUP.LAG_FIDGET_2:
                        animation = (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1;
                        break;
                    case LOW_ANIMATION_GROUP.LAG_ATTACK_3:
                    case LOW_ANIMATION_GROUP.LAG_ATTACK_2:
                        animation = (byte) LOW_ANIMATION_GROUP.LAG_ATTACK_1;
                        break;
                }

                if (!Animations.AnimationExists(graphic, animation))
                    animation = (byte) LOW_ANIMATION_GROUP.LAG_STAND;
            }
            else if (group == ANIMATION_GROUPS.AG_HIGHT)
            {
                switch ((HIGHT_ANIMATION_GROUP) animation)
                {
                    case HIGHT_ANIMATION_GROUP.HAG_DIE_2:
                        animation = (byte) HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_FIDGET_2:
                        animation = (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_ATTACK_3:
                    case HIGHT_ANIMATION_GROUP.HAG_ATTACK_2:
                        animation = (byte) HIGHT_ANIMATION_GROUP.HAG_ATTACK_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_4:
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_3:
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_2:
                        animation = (byte) HIGHT_ANIMATION_GROUP.HAG_MISC_1;
                        break;
                }

                if (!Animations.AnimationExists(graphic, animation))
                    animation = (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;
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


        public static byte GetReplacedObjectAnimation(in Mobile mobile, in ushort index)
        {
            ushort getReplacedGroup(in IReadOnlyList<Tuple<ushort, byte>> list, in ushort idx, in ushort walkIdx)
            {
                foreach (var item in list)
                    if (item.Item1 == idx)
                    {
                        if (item.Item2 == 0xFF)
                            return walkIdx;
                        return item.Item2;
                    }

                return idx;
            }

            var group = Animations.GetGroupIndex(mobile.Graphic);

            if (group == ANIMATION_GROUPS.AG_LOW)
                return (byte) (getReplacedGroup(Animations.GroupReplaces[0], index,
                                   (ushort) LOW_ANIMATION_GROUP.LAG_WALK) %
                               (ushort) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT);
            if (group == ANIMATION_GROUPS.AG_PEOPLE)
                return (byte) (getReplacedGroup(Animations.GroupReplaces[1], index,
                                   (ushort) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED) %
                               (ushort) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT);

            return (byte) (index % (ushort) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT);
        }

        public static byte GetObjectNewAnimation(in Mobile mobile, in ushort type, in ushort action, in byte mode)
        {
            if (mobile.Graphic >= Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                return 0;

            switch (type)
            {
                case 0:
                    return GetObjectNewAnimationType_0(mobile, action, mode);
                case 1:
                case 2:
                    return GetObjectNewAnimationType_1_2(mobile, action, mode);
                case 3:
                    return GetObjectNewAnimationType_3(mobile, action, mode);
                case 4:
                    return GetObjectNewAnimationType_4(mobile, action, mode);
                case 5:
                    return GetObjectNewAnimationType_5(mobile, action, mode);
                case 6:
                case 14:
                    return GetObjectNewAnimationType_6_14(mobile, action, mode);
                case 7:
                    return GetObjectNewAnimationType_7(mobile, action, mode);
                case 8:
                    return GetObjectNewAnimationType_8(mobile, action, mode);
                case 9:
                case 10:
                    return GetObjectNewAnimationType_9_10(mobile, action, mode);
                case 11:
                    return GetObjectNewAnimationType_11(mobile, action, mode);
            }

            return 0;
        }

        private static byte GetObjectNewAnimationType_0(in Mobile mobile, in ushort action, in byte mode)
        {
            if (action <= 10)
            {
                ref var ia = ref Animations.DataIndex[mobile.Graphic];
                var type = ANIMATION_GROUPS_TYPE.MONSTER;

                if ((ia.Flags & 0x80000000) != 0)
                    type = ia.Type;

                if (type == ANIMATION_GROUPS_TYPE.MONSTER)
                {
                    switch (mode % 4)
                    {
                        case 1:
                            return 5;
                        case 2:
                            return 6;
                        case 3:
                            if ((ia.Flags & 1) != 0)
                                return 12;
                            goto case 0;
                        case 0:
                            return 4;
                    }
                }
                else if (type == ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (mode % 2 != 0)
                        return 6;

                    return 5;
                }
                else if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                    {
                        if (action > 0)
                        {
                            if (action == 1)
                                return 27;
                            if (action == 2)
                                return 28;

                            return 26;
                        }

                        return 29;
                    }

                    switch (action)
                    {
                        default:
                            return 31;
                        case 1:
                            return 18;
                        case 2:
                            return 19;
                        case 6:
                            return 12;
                        case 7:
                            return 13;
                        case 8:
                            return 14;
                        case 3:
                            return 11;
                        case 4:
                            return 9;
                        case 5:
                            return 10;
                    }
                }

                if (mode % 2 != 0)
                    return 6;

                return 5;
            }

            return 0;
        }

        private static byte GetObjectNewAnimationType_1_2(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type <= ANIMATION_GROUPS_TYPE.ANIMAL || mobile.Equipment[(int) Layer.Mount] != null)
                    return 0xFF;
                return 30;
            }

            if (mode % 2 != 0) return 15;

            return 16;
        }

        private static byte GetObjectNewAnimationType_3(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type == ANIMATION_GROUPS_TYPE.SEA_MONSTER) return 8;

                if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mode % 2 != 0)
                        return 21;
                    return 22;
                }

                if (mode % 2 != 0)
                    return 8;
                return 12;
            }

            if (mode % 2 != 0) return 2;

            return 3;
        }

        private static byte GetObjectNewAnimationType_4(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type > ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                        return 0xFF;
                    return 20;
                }

                return 7;
            }

            return 10;
        }

        private static byte GetObjectNewAnimationType_5(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type <= ANIMATION_GROUPS_TYPE.SEA_MONSTER)
            {
                if (mode % 2 != 0)
                    return 18;

                return 17;
            }

            if (type != ANIMATION_GROUPS_TYPE.ANIMAL)
            {
                if (mobile.Equipment[(int) Layer.Mount] != null)
                    return 0xFF;

                if (mode % 2 != 0)
                    return 6;

                return 5;
            }

            switch (mode % 3)
            {
                case 1:
                    return 10;
                case 2:
                    return 3;
                default:
                    break;
            }

            return 9;
        }

        private static byte GetObjectNewAnimationType_6_14(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                        return 3;
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                        return 0xFF;
                    return 34;
                }

                return 5;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_7(in Mobile mobile, in ushort action, in byte mode)
        {
            if (mobile.Equipment[(int) Layer.Mount] != null)
                return 0xFF;

            if (action > 0)
            {
                if (action == 1)
                    return 33;
            }
            else
            {
                return 32;
            }

            return 0;
        }

        private static byte GetObjectNewAnimationType_8(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                        return 9;
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                        return 0xFF;
                    return 33;
                }

                return 3;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_9_10(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
                return 0xFF;
            return 20;
        }

        private static byte GetObjectNewAnimationType_11(in Mobile mobile, in ushort action, in byte mode)
        {
            ref var ia = ref Animations.DataIndex[mobile.Graphic];
            var type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type >= ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                        return 0xFF;
                    switch (action)
                    {
                        case 1:
                        case 2:
                            return 17;
                        default:
                            break;
                    }

                    return 16;
                }

                return 5;
            }

            return 12;
        }


        protected virtual bool NoIterateAnimIndex()
        {
            return LastStepTime > (uint) (World.Ticks - WALKING_DELAY) && _steps.Count <= 0;
        }

        public override void ProcessAnimation()
        {
            var dir = (byte) GetAnimationDirection();

            if (_steps.Count > 0)
            {
                var turnOnly = false;

                do
                {
                    var step = _steps.Front();

                    if (AnimationFromServer)
                        SetAnimation(0xFF);

                    var maxDelay = MovementSpeed.TimeToCompleteMovement(this, step.Run) - 15;
                    var delay = (int) World.Ticks - (int) LastStepTime;
                    var removeStep = delay >= maxDelay;

                    if (step.Direction == (byte) Direction)
                    {
                        var framesPerTile = maxDelay / CHARACTER_ANIMATION_DELAY;
                        var frameOffset = delay / CHARACTER_ANIMATION_DELAY;

                        var x = frameOffset;
                        var y = frameOffset;

                        GetPixelOffset(step.Direction, ref x, ref y, framesPerTile);

                        Offset = new Vector3((sbyte) x, (sbyte) y,
                            (int) ((step.Z - Position.Z) * frameOffset * (4.0f / framesPerTile)));

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
                var frameIndex = AnimIndex;

                if (AnimationFromServer && !AnimationDirection)
                    frameIndex--;
                else
                    frameIndex++;

                var id = GetMountAnimation();
                int animGroup = GetAnimationGroup(id);

                var mount = Equipment[(int) Layer.Mount];
                if (mount != null)
                    switch (animGroup)
                    {
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3:
                            id = mount.GetMountAnimation();
                            animGroup = GetAnimationGroup(id);
                            break;
                    }

                var mirror = false;
                Animations.GetAnimDirection(ref dir, ref mirror);

                var currentDelay = (int) CHARACTER_ANIMATION_DELAY;

                if (id < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ref var direction = ref Animations.DataIndex[id].Groups[animGroup].Direction[dir];
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
                                        var repCount = AnimationRepeatMode;
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
                                        var repCount = AnimationRepeatMode;
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

        public Direction GetAnimationDirection()
        {
            var dir = Direction & Direction.Up;

            if (_steps.Count > 0) dir = (Direction) _steps.Front().Direction & Direction.Up;
            return dir;
        }

        private static void GetPixelOffset(in byte dir, ref float x, ref float y, in float framesPerTile)
        {
            var step_NESW_D = 44.0f / framesPerTile;
            var step_NESW = 22.0f / framesPerTile;

            var checkX = 22;
            var checkY = 22;

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

            var valueX = (int) x;


            if (Math.Abs(valueX) > checkX)
            {
                if (valueX < 0)
                    x = -checkX;
                else
                    x = checkX;
            }

            var valueY = (int) y;

            if (Math.Abs(valueY) > checkY)
            {
                if (valueY < 0)
                    y = -checkY;
                else
                    y = checkY;
            }
        }

        public Graphic GetMountAnimation()
        {
            ushort g = Graphic;

            switch (g)
            {
                case 0x0192:
                case 0x0193:
                {
                    g -= 2;
                    break;
                }
            }

            return g;
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