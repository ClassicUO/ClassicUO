#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.Views;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    public enum RaceType : byte
    {
        HUMAN = 1,
        ELF,
        GARGOYLE
    }

    public enum CharacterSpeedType
    {
        Normal,
        FastUnmount,
        CantRun,
        FastUnmountAndCantRun
    }

    public partial class Mobile : Entity
    {
        private ushort _hits;
        private ushort _hitsMax;
        private bool _isDead;
        private bool _isRenamable;
        private bool _isSA_Poisoned;
        private ushort _mana;
        private ushort _manaMax;
        private NotorietyFlag _notorietyFlag;
        private RaceType _race;
        private ushort _stamina;
        private ushort _staminaMax;
        private long _lastAnimationIdleDelay;

        public Mobile(Serial serial) : base(serial)
        {
            _lastAnimationChangeTime = Engine.Ticks;
            CalculateRandomIdleTime();
        }

        private void CalculateRandomIdleTime()
        {
            _lastAnimationIdleDelay = Engine.Ticks + (30000 + RandomHelper.GetValue(0, 30000));
        }

        public Deque<Step> Steps { get; } = new Deque<Step>();

        public CharacterSpeedType SpeedMode { get; internal set; } = CharacterSpeedType.Normal;

        public bool IsFemale => (Flags & Flags.Female) != 0 || Graphic == 0x0191 || Graphic == 0x0193;

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

        public NotorietyFlag NotorietyFlag
        {
            get => _notorietyFlag;
            set
            {
                if (_notorietyFlag != value)
                {
                    _notorietyFlag = value;
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

        public bool IsRenamable
        {
            get => _isRenamable;
            set
            {
                if (_isRenamable != value)
                {
                    _isRenamable = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        public bool IsParalyzed => ((byte) Flags & 0x01) != 0;

        public bool IsYellowHits => ((byte) Flags & 0x08) != 0;

        public bool IsPoisoned => FileManager.ClientVersion >= ClientVersions.CV_7000 ? _isSA_Poisoned : ((byte) Flags & 0x04) != 0;

        public bool IsHidden => ((byte) Flags & 0x80) != 0;

        public bool IgnoreCharacters => ((byte) Flags & 0x10) != 0;

        public bool IsDead
        {
            get => MathHelper.InRange(Graphic, 0x0192, 0x0193) || MathHelper.InRange(Graphic, 0x025F, 0x0260) || MathHelper.InRange(Graphic, 0x02B6, 0x02B7) || _isDead;
            set => _isDead = value;
        }

        public bool IsFlying => FileManager.ClientVersion >= ClientVersions.CV_7000 && ((byte) Flags & 0x04) != 0;

        public virtual bool InWarMode
        {
            get => ((byte) Flags & 0x40) != 0;
            set { }
        }

        public bool IsHuman => MathHelper.InRange(Graphic, 0x0190, 0x0193) || MathHelper.InRange(Graphic, 0x00B7, 0x00BA) || MathHelper.InRange(Graphic, 0x025D, 0x0260) || MathHelper.InRange(Graphic, 0x029A, 0x029B) || MathHelper.InRange(Graphic, 0x02B6, 0x02B7) || Graphic == 0x03DB || Graphic == 0x03DF || Graphic == 0x03E2 || Graphic == 0x02E8 || Graphic == 0x02E9; // Vampiric

        public override bool Exists => World.Contains(Serial);

        public Item[] Equipment { get; } = new Item[(int) Layer.Bank + 1];

        public bool IsMounted => Equipment[(int) Layer.Mount] != null;

        public bool IsRunning { get; internal set; }

        public byte AnimationInterval { get; set; }

        public byte AnimationFrameCount { get; set; }

        public byte AnimationRepeatMode { get; set; } = 1;

        public bool AnimationRepeat { get; set; }

        public bool AnimationFromServer { get; set; }

        public bool AnimationDirection { get; set; }

        public long LastStepTime { get; set; }

        protected virtual bool IsWalking => LastStepTime > Engine.Ticks - Constants.WALKING_DELAY;

        public byte AnimationGroup { get; set; } = 0xFF;

        internal bool IsMoving => Steps.Count > 0;

        public event EventHandler HitsChanged;

        public event EventHandler ManaChanged;

        public event EventHandler StaminaChanged;

        protected override View CreateView()
        {
            return new MobileView(this);
        }

        public void SetSAPoison(bool value)
        {
            _isSA_Poisoned = value;
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_lastAnimationIdleDelay < Engine.Ticks)
                SetIdleAnimation();

            ProcessAnimation();
        }     

        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Hits)) HitsChanged.Raise(this);
            if (d.HasFlag(Delta.Mana)) ManaChanged.Raise(this);
            if (d.HasFlag(Delta.Stamina)) StaminaChanged.Raise(this);
        }

        public void ClearSteps()
        {
            Steps.Clear();
            Offset = Vector3.Zero;
        }

        public bool EnqueueStep(int x, int y, sbyte z, Direction direction, bool run)
        {
            if (Steps.Count >= Constants.MAX_STEP_COUNT)
                return false;

            GetEndPosition(out int endX, out int endY, out sbyte endZ, out Direction endDir);

            if (endX == x && endY == y && endZ == z && endDir == direction) return true;
            if (!IsMoving) LastStepTime = Engine.Ticks;
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
                    Steps.AddToBack(step);
                }

                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte) moveDir;
                step.Run = run;
                Steps.AddToBack(step);
            }

            if (moveDir != direction)
            {
                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte) direction;
                step.Run = run;
                Steps.AddToBack(step);
            }

            return true;
        }

        private static Direction CalculateDirection(int curX, int curY, int newX, int newY)
        {
            int deltaX = newX - curX;
            int deltaY = newY - curY;

            if (deltaX > 0)
            {
                if (deltaY > 0) return Direction.Down;

                return deltaY == 0 ? Direction.East : Direction.Right;
            }

            if (deltaX == 0)
            {
                if (deltaY > 0) return Direction.South;

                return deltaY == 0 ? Direction.NONE : Direction.North;
            }

            if (deltaY > 0) return Direction.Left;

            return deltaY == 0 ? Direction.West : Direction.Up;
        }

        internal void GetEndPosition(out int x, out int y, out sbyte z, out Direction dir)
        {
            if (Steps.Count <= 0)
            {
                x = Position.X;
                y = Position.Y;
                z = Position.Z;
                dir = Direction;
            }
            else
            {
                Step step = Steps.Back();
                x = step.X;
                y = step.Y;
                z = step.Z;
                dir = (Direction) step.Direction;
            }
        }

#if JAEDAN_MOVEMENT_PATCH
        public virtual void ForcePosition(ushort x, ushort y, sbyte z, Direction dir)
        {
            Position = new Position(x, y, z);
            Direction = dir;
            ClearSteps();
            AddToTile();
            ProcessDelta();
        }
#endif

        public void SetAnimation(byte id, byte interval = 0, byte frameCount = 0, byte repeatCount = 0, bool repeat = false, bool frameDirection = false)
        {
            AnimationGroup = id;
            AnimIndex = 0;
            AnimationInterval = interval;
            AnimationFrameCount = frameCount;
            AnimationRepeatMode = repeatCount;
            AnimationRepeat = repeat;
            AnimationDirection = frameDirection;
            AnimationFromServer = false;
            _lastAnimationChangeTime = Engine.Ticks;
            CalculateRandomIdleTime();
        }

        public void SetIdleAnimation()
        {
            CalculateRandomIdleTime();

            if (!IsMounted)
            {
                AnimIndex = 0;
                AnimationFrameCount = 0;
                AnimationInterval = 1;
                AnimationRepeatMode = 1;
                AnimationDirection = true;
                AnimationRepeat = false;
                AnimationFromServer = true;

                byte index = (byte) Animations.GetGroupIndex(GetGraphicForAnimation());

                AnimationGroup = _animationIdle[index - 1, RandomHelper.GetValue(0, 2)];
            }
        }

        private static readonly byte[,] _animationIdle =
        {
            {
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_2, (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1
            },
            {
                (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_2, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1
            },
            {
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1, (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2, (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3
            }
        };

        protected virtual bool NoIterateAnimIndex()
        {
            return LastStepTime > (uint) (Engine.Ticks - Constants.WALKING_DELAY) && Steps.Count <= 0;
        }

        public override void ProcessAnimation()
        {
            byte dir = (byte) GetDirectionForAnimation();

            if (Steps.Count > 0)
            {
                bool turnOnly;

                do
                {
                    Step step = Steps.Front();
                    if (AnimationFromServer) SetAnimation(0xFF);
                    int maxDelay = MovementSpeed.TimeToCompleteMovement(this, step.Run) - (IsMounted || SpeedMode == CharacterSpeedType.FastUnmount ? 1 : 15); // default 15 = less smooth
                    int delay = (int) Engine.Ticks - (int) LastStepTime;
                    bool removeStep = delay >= maxDelay;

                    if (Position.X != step.X || Position.Y != step.Y)
                    {
                        if (Engine.Profile.Current.SmoothMovements)
                        {
                            float framesPerTile = maxDelay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float frameOffset = delay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float x = frameOffset;
                            float y = frameOffset;
                            MovementSpeed.GetPixelOffset((byte) Direction, ref x, ref y, framesPerTile);
                            Offset = new Vector3((sbyte) x, (sbyte) y, (int) ((step.Z - Position.Z) * frameOffset * (4.0f / framesPerTile)));
                        }

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

#if !JAEDAN_MOVEMENT_PATCH
                            if (World.Player.Walker.StepInfos[World.Player.Walker.CurrentWalkSequence].Accepted)
                            {
                                int sequence = World.Player.Walker.CurrentWalkSequence + 1;

                                if (sequence < World.Player.Walker.StepsCount)
                                {
                                    int count = World.Player.Walker.StepsCount - sequence;

                                    for (int i = 0; i < count; i++)
                                    {
                                        World.Player.Walker.StepInfos[sequence - 1] = World.Player.Walker.StepInfos[sequence];
                                        sequence++;
                                    }
                                }

                                World.Player.Walker.StepsCount--;
                            }
                            else
                                World.Player.Walker.CurrentWalkSequence++;
#endif
                        }

                        Position = new Position((ushort) step.X, (ushort) step.Y, step.Z);
                        AddToTile();
                        Direction = (Direction) step.Direction;
                        IsRunning = step.Run;
                        Offset = Vector3.Zero;
                        Steps.RemoveFromFront();
                        LastStepTime = Engine.Ticks;
                        ProcessDelta();
                    }
                } while (Steps.Count > 0 && turnOnly);
            }

            if (_lastAnimationChangeTime < Engine.Ticks && !NoIterateAnimIndex())
            {
                sbyte frameIndex = AnimIndex;

                if (AnimationFromServer && !AnimationDirection)
                    frameIndex--;
                else
                    frameIndex++;
                Graphic id = GetGraphicForAnimation();
                int animGroup = GetGroupForAnimation(this, id);

                if (animGroup == 64 || animGroup == 65)
                {
                    animGroup = InWarMode ? 65 : 64;
                    AnimationGroup = (byte) animGroup;
                }

                Item mount = Equipment[(int) Layer.Mount];

                if (mount != null)
                {
                    switch (animGroup)
                    {
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3:
                            id = mount.GetGraphicForAnimation();
                            animGroup = GetGroupForAnimation(this, id);

                            break;
                    }
                }

                bool mirror = false;
                Animations.GetAnimDirection(ref dir, ref mirror);
                int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                if (id < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ref AnimationDirection direction = ref Animations.DataIndex[id].Groups[animGroup].Direction[dir];
                    Animations.AnimID = id;
                    Animations.AnimGroup = (byte) animGroup;
                    Animations.Direction = dir;
                    if ((direction.FrameCount == 0 || direction.Frames == null)) Animations.LoadDirectionGroup(ref direction);

                    if (direction.Address != 0 || direction.IsUOP)
                    {
                        direction.LastAccessTime = Engine.Ticks;
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
                                        else if (repCount == 1) SetAnimation(0xFF);
                                    }
                                    else
                                        SetAnimation(0xFF);
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
                                        else if (repCount == 1) SetAnimation(0xFF);
                                    }
                                    else
                                        SetAnimation(0xFF);
                                }
                            }
                        }
                        else
                        {
                            if (frameIndex >= fc) frameIndex = 0;
                        }

                        AnimIndex = frameIndex;
                    }
                }

                _lastAnimationChangeTime = Engine.Ticks + currentDelay;
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < Equipment.Length; i++)
                Equipment[i] = null;
            base.Dispose();
        }

        public struct Step
        {
            public int X, Y;
            public sbyte Z;
            public byte Direction;
            public bool Run;
        }
    }
}