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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;

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
        private bool _isDead;
        private bool _isSA_Poisoned;
        private long _lastAnimationIdleDelay;

        public Mobile(Serial serial) : base(serial)
        {
            LastAnimationChangeTime = Engine.Ticks;
            CalculateRandomIdleTime();
        }

        public Deque<Step> Steps { get; } = new Deque<Step>(Constants.MAX_STEP_COUNT);

        public CharacterSpeedType SpeedMode { get; internal set; } = CharacterSpeedType.Normal;

        public long DeathScreenTimer { get; set; }

        private bool _isMale;

        public bool IsMale
        {
            get => _isMale || (Flags & Flags.Female) == 0 || IsOtherMale || IsElfMale || (Graphic < 900 && Graphic % 2 == 0 && !IsOtherFemale && !IsElfFemale);
            set => _isMale = value;
        }

        public bool IsOtherMale => Graphic == 183 || Graphic == 185;
        public bool IsElfMale => Graphic == 605 || Graphic == 607;
        public bool IsOtherFemale => Graphic == 184 || Graphic == 186;
        public bool IsElfFemale => Graphic == 606 || Graphic == 608;

        public RaceType Race { get; set; }

        public ushort Mana { get; set; }

        public ushort ManaMax { get; set; }

        public ushort Stamina { get; set; }

        public ushort StaminaMax { get; set; }

        public NotorietyFlag NotorietyFlag { get; set; }

        public bool IsRenamable { get; set; }

        public bool IsParalyzed => ((byte) Flags & 0x01) != 0;

        public bool IsYellowHits => ((byte) Flags & 0x08) != 0;

        public bool IsPoisoned => FileManager.ClientVersion >= ClientVersions.CV_7000 ? _isSA_Poisoned : ((byte) Flags & 0x04) != 0;

        public bool IgnoreCharacters => ((byte) Flags & 0x10) != 0;

        public bool IsDead
        {
            get => Graphic == 0x0192 ||
                   Graphic == 0x0193 ||
                   (Graphic >= 0x025F && Graphic <= 0x0260) ||
                   Graphic == 0x2B6 || Graphic == 0x02B7 || _isDead;
            set => _isDead = value;
        }

        public bool IsFlying => FileManager.ClientVersion >= ClientVersions.CV_7000 && ((byte) Flags & 0x04) != 0;

        public virtual bool InWarMode
        {
            get => ((byte) Flags & 0x40) != 0;
            set { }
        }

        public bool IsHuman => (Graphic >= 0x0190 && Graphic <= 0x0193) ||
                               (Graphic >= 0x00B7 && Graphic <= 0x00BA) ||
                               (Graphic >= 0x025D && Graphic <= 0x0260) ||
                               Graphic == 0x029A || Graphic == 0x029B ||
                               Graphic == 0x02B6 || Graphic == 0x02B7 ||
                               Graphic == 0x03DB || Graphic == 0x03DF || Graphic == 0x03E2 || Graphic == 0x02E8 || Graphic == 0x02E9; // Vampiric

        public bool IsMounted => HasEquipment && Equipment[0x19] != null && !IsDrivingBoat && Equipment[0x19].GetGraphicForAnimation() != 0xFFFF;

        public bool IsDrivingBoat
        {
            get
            {
                if (FileManager.ClientVersion >= ClientVersions.CV_70331 && HasEquipment)
                {
                    Item m = Equipment[0x19];
                    return m != null && m.Graphic == 0x3E96; // TODO: im not sure if each server sends this value ever
                }

                return false;
            }
        }

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

        internal bool IsMoving => Steps.Count != 0;

        public Item GetSecureTradeBox()
        {
            return Items.FirstOrDefault(s => s.Graphic == 0x1E5E && s.Layer == Layer.Invalid);
        }

        public void SetSAPoison(bool value)
        {
            _isSA_Poisoned = value;
        }

        private void CalculateRandomIdleTime()
        {
            const int TIME = 30000;
            _lastAnimationIdleDelay = Engine.Ticks + (TIME + RandomHelper.GetValue(0, TIME));
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDestroyed)
                return;

            base.Update(totalMS, frameMS);

            if (_lastAnimationIdleDelay < Engine.Ticks)
                SetIdleAnimation();

            ProcessAnimation(out _, true);
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

            if (!IsMoving)
            {
                if (!IsWalking)
                    SetAnimation(0xFF);
                LastStepTime = Engine.Ticks;
            }

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
            if (Steps.Count == 0)
            {
                x = X;
                y = Y;
                z = Z;
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


                ushort graphic = GetGraphicForAnimation();

                ANIMATION_GROUPS_TYPE type = FileManager.Animations.DataIndex[graphic].Type;

                if (FileManager.Animations.DataIndex[graphic].IsUOP && !FileManager.Animations.DataIndex[graphic].IsValidMUL)
                {
                    // do nothing ?
                }
                else
                {
                    if (!FileManager.Animations.DataIndex[graphic].HasBodyConversion)
                    {
                        ushort newGraphic = FileManager.Animations.DataIndex[graphic].Graphic;

                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;
                            ANIMATION_GROUPS_TYPE newType = FileManager.Animations.DataIndex[graphic].Type;

                            if (newType != type) type = newType;
                        }
                    }
                }

                ANIMATION_FLAGS flags = (ANIMATION_FLAGS) FileManager.Animations.DataIndex[graphic].Flags;
                ANIMATION_GROUPS animGroup = ANIMATION_GROUPS.AG_NONE;

                bool isLowExtended = false;
                bool isLow = false;

                if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                {
                    isLowExtended = true;
                    type = ANIMATION_GROUPS_TYPE.MONSTER;
                }
                else if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                {
                    type = ANIMATION_GROUPS_TYPE.ANIMAL;
                    isLow = true;
                }

                switch (type)
                {
                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                    case ANIMATION_GROUPS_TYPE.MONSTER:
                        animGroup = ANIMATION_GROUPS.AG_HIGHT;

                        break;

                    case ANIMATION_GROUPS_TYPE.ANIMAL:
                        animGroup = ANIMATION_GROUPS.AG_LOW;

                        break;

                    case ANIMATION_GROUPS_TYPE.HUMAN:
                    case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                        animGroup = ANIMATION_GROUPS.AG_PEOPLE;

                        break;
                }

                if (animGroup == 0)
                    return;

                if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                {
                    if (animGroup != ANIMATION_GROUPS.AG_PEOPLE)
                    {
                        if (InWarMode)
                            AnimationGroup = 28;
                        else
                            AnimationGroup = 26;

                        return;
                    }
                }

                AnimationGroup = _animationIdle[(byte)animGroup - 1, RandomHelper.GetValue(0, 2)];

                if (isLowExtended && AnimationGroup == 18)
                    AnimationGroup = 1;
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
            return LastStepTime > Engine.Ticks - Constants.WALKING_DELAY && Steps.Count == 0;
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


                    int distance = Distance;

                    float volume = Engine.Profile.Current.SoundVolume / Constants.SOUND_DELTA;

                    if (distance <= World.ClientViewRange && distance >= 1)
                    {
                        float volumeByDist = volume / World.ClientViewRange;
                        volume -= volumeByDist * distance;
                    }

                    Engine.SceneManager.CurrentScene.Audio.PlaySoundWithDistance(soundID, volume);
                    LastStepSoundTime = ticks + delaySound;
                }
            }
        }

        public override void ProcessAnimation(out byte dir, bool evalutate = false)
        {
            ProcessSteps(out dir, evalutate);

            ProcessFootstepsSound();

            if (LastAnimationChangeTime < Engine.Ticks && !NoIterateAnimIndex())
            {
                sbyte frameIndex = AnimIndex;

                if (AnimationFromServer && !AnimationDirection)
                    frameIndex--;
                else
                    frameIndex++;
                ushort id = GetGraphicForAnimation();
                byte animGroup = GetGroupForAnimation(this, id, true);

                if (animGroup == 64 || animGroup == 65)
                {
                    animGroup = (byte) (InWarMode ? 65 : 64);
                    AnimationGroup = animGroup;
                }

                Item mount = HasEquipment ? Equipment[(int) Layer.Mount] : null;

                if (mount != null)
                {
                    switch (animGroup)
                    {
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2:
                        case (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3:
                            id = mount.GetGraphicForAnimation();
                            animGroup = GetGroupForAnimation(this, id, true);

                            break;
                    }
                }

                bool mirror = false;
                FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
                int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ushort hue = 0;
                    ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true).Direction[dir];
                    FileManager.Animations.AnimID = id;
                    FileManager.Animations.AnimGroup = animGroup;
                    FileManager.Animations.Direction = dir;


                    if (direction.FrameCount == 0 || direction.Frames == null)
                        FileManager.Animations.LoadDirectionGroup(ref direction);

                    if (direction.Address != 0 && direction.Size != 0 && direction.FileIndex != -1 || direction.IsUOP)
                    {
                        direction.LastAccessTime = Engine.Ticks;
                        int fc = direction.FrameCount;

                        if (AnimationFromServer)
                        {
                            currentDelay += currentDelay * (AnimationInterval + 1);

                            if (AnimationFrameCount == 0)
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
                                    if (fc == 0)
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
                                            SetAnimation(0xFF);
                                    }
                                    else
                                        SetAnimation(0xFF);
                                }
                            }
                        }
                        else
                        {
                            if (frameIndex >= fc)
                            {
                                frameIndex = 0;

                                if ((Serial & 0x80000000) != 0)
                                {
                                    World.CorpseManager.Remove(0, Serial);
                                    World.RemoveMobile(Serial);
                                }
                            }
                        }

                        AnimIndex = frameIndex;
                    }
                    else if ((Serial & 0x80000000) != 0)
                    {
                        World.CorpseManager.Remove(0, Serial);
                        World.RemoveMobile(Serial);
                    }
                }
                else if ((Serial & 0x80000000) != 0)
                {
                    World.CorpseManager.Remove(0, Serial);
                    World.RemoveMobile(Serial);
                }

                LastAnimationChangeTime = Engine.Ticks + currentDelay;
            }
        }

        public void ProcessSteps(out byte dir, bool evalutate = false)
        {
            dir = (byte) Direction;
            dir &= 7;

            if (Steps.Count != 0 && !IsDestroyed)
            {
                Step step = Steps.Front();
                dir = step.Direction;

                if (step.Run)
                    dir &= 7;

                if (evalutate)
                {
                    if (AnimationFromServer)
                        SetAnimation(0xFF);

                    int maxDelay = MovementSpeed.TimeToCompleteMovement(this, step.Run) - (int) Engine.FrameDelay[1];
                    int delay = (int) Engine.Ticks - (int) LastStepTime;
                    bool removeStep = delay >= maxDelay;
                    bool directionChange = false;

                    if (X != step.X || Y != step.Y)
                    {
                        bool badStep = false;

                        if (Offset.X == 0 && Offset.Y == 0)
                        {
                            int absX = Math.Abs(X - step.X);
                            int absY = Math.Abs(Y - step.Y);

                            badStep = absX > 1 || absY > 1 || absX + absY == 0;

                            if (!badStep)
                            {
                                absX = X;
                                absY = Y;

                                Pathfinder.GetNewXY((byte) (step.Direction & 7), ref absX, ref absY);

                                badStep = absX != step.X || absY != step.Y;
                            }
                        }

                        if (badStep)
                            removeStep = true;
                        else
                        {
                            float steps = maxDelay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float x = delay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float y = x;
                            Offset.Z = (sbyte) ((step.Z - Z) * x * (4.0f / steps));
                            MovementSpeed.GetPixelOffset(step.Direction, ref x, ref y, steps);
                            Offset.X = (sbyte) x;
                            Offset.Y = (sbyte) y;
                        }
                    }
                    else
                    {
                        directionChange = true;
                        removeStep = true;
                    }

                    if (removeStep)
                    {
                        if (Serial == World.Player)
                        {
                            //if (Position.X != step.X || Position.Y != step.Y || Position.Z != step.Z)
                            //{
                            //}

                            if (Z - step.Z >= 22)
                            {
                                // oUCH!!!!
                                AddMessage(MessageType.Label, "Ouch!");
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

                        if (World.InGame && Serial == World.Player)
                        {
                            World.Player.CloseRangedGumps();
                        }

                        Direction = (Direction) step.Direction;
                        IsRunning = step.Run;
                        Offset.X = 0;
                        Offset.Y = 0;
                        Offset.Z = 0;
                        Steps.RemoveFromFront();
                        CalculateRandomIdleTime();

                        if (directionChange)
                        {
                            ProcessSteps(out dir, evalutate);

                            return;
                        }

                        if (Right != null || Left != null)
                            AddToTile();

                        LastStepTime = Engine.Ticks;
                        ProcessDelta();
                    }
                }
            }
            else
            {
                Offset.X = 0;
                Offset.Y = 0;
                Offset.Z = 0;
            }
        }


        public int IsSitting()
        {
            //get
            {
                int result = 0;

                if (IsHuman && !IsMounted && !TestStepNoChangeDirection(this, GetGroupForAnimation(this, isParent: true)) && Tile != null)
                {
                    GameObject start = Tile.FirstNode;

                    while (start != null && result == 0)
                    {
                        if ((start is Item || start is Static || start is Multi) && Math.Abs(Z - start.Z) <= 1)
                        {
                            ushort graphic = start.Graphic;

                            //if (start is Multi || start is Mobile)
                            //    graphic = 0;

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
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
                return;

            var last = TextContainer.Items;

            while (last?.ListRight != null)
                last = last.ListRight;

            if (last == null)
                return;

            int offY = 0;

            bool health = Engine.Profile.Current.ShowMobilesHP;
            int alwaysHP = Engine.Profile.Current.MobileHPShowWhen;
            int mode = Engine.Profile.Current.MobileHPType;

            int startX = Engine.Profile.Current.GameWindowPosition.X + 6;
            int startY = Engine.Profile.Current.GameWindowPosition.Y + 6;
            var scene = Engine.SceneManager.GetScene<GameScene>();
            float scale = scene?.Scale ?? 1;

            int x = RealScreenPosition.X;
            int y = RealScreenPosition.Y;


            if (health && mode != 1 && ((alwaysHP >= 1 && Hits != HitsMax) || alwaysHP == 0))
            {
                y -= 22;
            }

            if (!IsMounted)
                y += 22;

            FileManager.Animations.GetAnimationDimensions(AnimIndex,
                                                          GetGraphicForAnimation(),
                                                          /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                          /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                          IsMounted,
                                                          /*(byte) m.AnimIndex*/ 0,
                                                          out _,
                                                          out int centerY,
                                                          out _,
                                                          out int height);
            x += (int)Offset.X;
            x += 22;
            y += (int)(Offset.Y - Offset.Z - (height + centerY + 8));

            for (; last != null; last = last.ListLeft)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Engine.Ticks)
                        continue;


                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = startX + (int)((x - (last.RenderedText.Width >> 1)) / scale);
                    last.RealScreenPosition.Y = startY + (int)((y - offY) / scale);
                }
            }

            FixTextCoordinatesInScreen();
        }

        public override void Destroy()
        {
            HitsTexture?.Destroy();
            HitsTexture = null;

            if (HasEquipment)
            {
                for (int i = 0; i < Equipment.Length; i++)
                    Equipment[i] = null;
            }

            base.Destroy();
        }

        internal struct Step
        {
            public int X, Y;
            public sbyte Z;
            public byte Direction;
            public bool Run;

#if JAEDAN_MOVEMENT_PATCH || MOVEMENT2
            public byte Rej;
            public bool Anim;
            public byte Seq;
#endif
        }
    }
}