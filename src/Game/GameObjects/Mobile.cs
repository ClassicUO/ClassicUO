#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.IO;
using ClassicUO.IO.Audio;
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

    internal partial class Mobile : Entity
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

        public long WaitDeathScreenTimer { get; set; }

        public Mobile(Serial serial) : base(serial)
        {
            LastAnimationChangeTime = Engine.Ticks;
            CalculateRandomIdleTime();

            _frames = new ViewLayer[(int)Layer.Legs];
            HasShadow = true;
        }

        private void CalculateRandomIdleTime()
        {
            _lastAnimationIdleDelay = Engine.Ticks + (30000 + RandomHelper.GetValue(0, 30000));
        }

        public Deque<Step> Steps { get; } = new Deque<Step>(Constants.MAX_STEP_COUNT);

        public CharacterSpeedType SpeedMode { get; internal set; } = CharacterSpeedType.Normal;

        private bool _isFemale;

        public bool IsFemale
        {
            get => _isFemale || (Flags & Flags.Female) != 0 || Graphic == 0x0191 || Graphic == 0x0193 || Graphic == 0x025E || Graphic == 0x029B;
            set
            {
                if (_isFemale != value)
                {
                    _isFemale = value;
                    _delta |= Delta.Appearance;
                }
            }
        }

      
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

        public bool IsMounted => Equipment[(int) Layer.Mount] != null;

        public bool IsRunning { get; internal set; }

        public byte AnimationInterval { get; set; }

        public byte AnimationFrameCount { get; set; }

        public byte AnimationRepeatMode { get; set; } = 1;

        public bool AnimationRepeat { get; set; }

        public bool AnimationFromServer { get; set; }

        public bool AnimationDirection { get; set; }

        public long LastStepTime { get; set; }

        public long LastStepSoundTime { get; set; }

        public int StepSoundOffset { get; set; }

        protected virtual bool IsWalking => LastStepTime > Engine.Ticks - Constants.WALKING_DELAY;

        public byte AnimationGroup { get; set; } = 0xFF;

        internal bool IsMoving => Steps.Count > 0;

        public event EventHandler HitsChanged;

        public event EventHandler ManaChanged;

        public event EventHandler StaminaChanged;


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

#if JAEDAN_MOVEMENT_PATCH || MOVEMENT2
        public virtual void ForcePosition(ushort x, ushort y, sbyte z, Direction dir)
        {
            ClearSteps();
            Position = new Position(x, y, z);
            Direction = dir;
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
            LastAnimationChangeTime = Engine.Ticks;
            CalculateRandomIdleTime();
        }

        public void SetIdleAnimation()
        {
            CalculateRandomIdleTime();

            if (!IsMounted && !InWarMode)
            {
                AnimIndex = 0;
                AnimationFrameCount = 0;
                AnimationInterval = 1;
                AnimationRepeatMode = 1;
                AnimationDirection = true;
                AnimationRepeat = false;
                AnimationFromServer = true;

                byte index = (byte) FileManager.Animations.GetGroupIndex(GetGraphicForAnimation());

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

        private void ProcessFootstepsSound()
        {
            if (Engine.Profile.Current.EnableFootstepsSound && IsHuman && !IsHidden)
            {
                long ticks = Engine.Ticks;

                if (IsMoving && LastStepSoundTime < ticks)
                {
                    int incID = StepSoundOffset;
                    int soundID = 0x012B;
                    int delaySound = 400;

                    if (IsMounted)
                    {
                        if (Steps.Back().Run)
                        {
                            soundID = 0x0129;
                            delaySound = 150;
                        }
                        else
                        {
                            incID = 0;
                            delaySound = 350;
                        }
                    }

                    delaySound = delaySound * 13 / 10;

                    soundID += incID;

                    StepSoundOffset = (incID + 1) % 2;

                    float soundByRange = Engine.Profile.Current.SoundVolume / (float) World.ViewRange;
                    soundByRange *= Distance;
                    float volume = (Engine.Profile.Current.SoundVolume - soundByRange) / 250f;

                    //if (volume > 0 && volume < 0.01f)
                    //    volume = 0.01f;

                    Engine.SceneManager.CurrentScene.Audio.PlaySoundWithDistance(soundID, volume);
                    LastStepSoundTime = ticks + delaySound;
                }
            }
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
                    int maxDelay = MovementSpeed.TimeToCompleteMovement(this, step.Run);
                    int delay = (int) Engine.Ticks - (int) LastStepTime;
                    bool removeStep = delay >= maxDelay;

                    //if ((byte) Direction == step.Direction)
                    if (X != step.X || Y != step.Y)
                    {     
                        float framesPerTile = maxDelay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                        float frameOffset = delay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                        float x = frameOffset;
                        float y = frameOffset;

                        MovementSpeed.GetPixelOffset((byte) Direction, ref x, ref y, framesPerTile);
                        Offset = new Vector3((sbyte) x, (sbyte) y, (int) ((step.Z - Z) * frameOffset * (4.0f / framesPerTile)));
                     
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
                            //if (Position.X != step.X || Position.Y != step.Y || Position.Z != step.Z)
                            //{
                            //}

                            if (Position.Z - step.Z >= 22)
                            {
                                // oUCH!!!!
                                AddOverhead(MessageType.Label, "Ouch!");
                            }

#if !JAEDAN_MOVEMENT_PATCH && !MOVEMENT2
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
                        CalculateRandomIdleTime();
                        LastStepTime = Engine.Ticks;
                        ProcessDelta();
                    }
                } while (Steps.Count != 0 && turnOnly);
            }

            ProcessFootstepsSound();

            if (LastAnimationChangeTime < Engine.Ticks && !NoIterateAnimIndex())
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
                FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
                int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ref AnimationDirection direction = ref FileManager.Animations.DataIndex[id].Groups[animGroup].Direction[dir];
                    FileManager.Animations.AnimID = id;
                    FileManager.Animations.AnimGroup = (byte) animGroup;
                    FileManager.Animations.Direction = dir;
                    if ((direction.FrameCount == 0 || direction.FramesHashes == null)) FileManager.Animations.LoadDirectionGroup(ref direction);

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

                LastAnimationChangeTime = Engine.Ticks + currentDelay;
            }
        }

        /* public int IsSitting
        {
            get
            {
                int result = 0;

                if (IsHuman && !IsMounted)
                {
                    GameObject start = World.Map.GetTile(X, Y).FirstNode;

                    while (start != null && result == 0 && !TestStepNoChangeDirection(this, GetGroupForAnimation(this)))
                    {
                        if (GameObjectHelper.TryGetStaticData(start, out var itemdata) && Math.Abs(Z - start.Z) <= 1)
                        {
                            ushort graphic = start.Graphic;

                            if (start is Multi)
                                graphic = 0;

                            switch (graphic)
                            {
                                case 0x0459:
                                case 0x045A:
                                case 0x045B:
                                case 0x045C:
                                case 0x0A2A:
                                case 0x0A2B:
                                case 0x0B2C:
                                case 0x0B2D:
                                case 0x0B2E:
                                case 0x0B2F:
                                case 0x0B30:
                                case 0x0B31:
                                case 0x0B32:
                                case 0x0B33:
                                case 0x0B4E:
                                case 0x0B4F:
                                case 0x0B50:
                                case 0x0B51:
                                case 0x0B52:
                                case 0x0B53:
                                case 0x0B54:
                                case 0x0B55:
                                case 0x0B56:
                                case 0x0B57:
                                case 0x0B58:
                                case 0x0B59:
                                case 0x0B5A:
                                case 0x0B5B:
                                case 0x0B5C:
                                case 0x0B5D:
                                case 0x0B5E:
                                case 0x0B5F:
                                case 0x0B60:
                                case 0x0B61:
                                case 0x0B62:
                                case 0x0B63:
                                case 0x0B64:
                                case 0x0B65:
                                case 0x0B66:
                                case 0x0B67:
                                case 0x0B68:
                                case 0x0B69:
                                case 0x0B6A:
                                case 0x0B91:
                                case 0x0B92:
                                case 0x0B93:
                                case 0x0B94:
                                case 0x0CF3:
                                case 0x0CF4:
                                case 0x0CF6:
                                case 0x0CF7:
                                case 0x11FC:
                                case 0x1218:
                                case 0x1219:
                                case 0x121A:
                                case 0x121B:
                                case 0x1527:
                                case 0x1771:
                                case 0x1776:
                                case 0x1779:
                                case 0x1DC7:
                                case 0x1DC8:
                                case 0x1DC9:
                                case 0x1DCA:
                                case 0x1DCB:
                                case 0x1DCC:
                                case 0x1DCD:
                                case 0x1DCE:
                                case 0x1DCF:
                                case 0x1DD0:
                                case 0x1DD1:
                                case 0x1DD2:
                                case 0x2A58:
                                case 0x2A59:
                                case 0x2A5A:
                                case 0x2A5B:
                                case 0x2A7F:
                                case 0x2A80:
                                case 0x2DDF:
                                case 0x2DE0:
                                case 0x2DE3:
                                case 0x2DE4:
                                case 0x2DE5:
                                case 0x2DE6:
                                case 0x2DEB:
                                case 0x2DEC:
                                case 0x2DED:
                                case 0x2DEE:
                                case 0x2DF5:
                                case 0x2DF6:
                                case 0x3088:
                                case 0x3089:
                                case 0x308A:
                                case 0x308B:
                                case 0x35ED:
                                case 0x35EE:
                                case 0x3DFF:
                                case 0x3E00:

                                    for (int i = 0; i < 98; i++)
                                    {
                                        if (FileManager.Animations.SittingInfos[i].Graphic == graphic)
                                        {
                                            result = i + 1;

                                            break;
                                        }
                                    }

                                    break;
                            }
                        }

                        start = start.Right;
                    }
                }

                return result;
            }
        } */

        public override void Dispose()
        {
            for (int i = 0; i < Equipment.Length; i++)
                Equipment[i] = null;
            base.Dispose();
        }

        internal struct Step
        {
            public int X, Y;
            public sbyte Z;
            public byte Direction;
            public bool Run;
            public byte Rej;
            public bool Anim;
            public byte Seq;
        }
    }
}