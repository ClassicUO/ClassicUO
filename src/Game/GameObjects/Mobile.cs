#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile : Entity
    {
        private static readonly QueuedPool<Mobile> _pool = new QueuedPool<Mobile>
        (
            Constants.PREDICTABLE_CHUNKS,
            mobile =>
            {
                mobile.IsDestroyed = false;
                mobile.Graphic = 0;
                mobile.Steps.Clear();
                mobile.Offset = Vector3.Zero;
                mobile.SpeedMode = CharacterSpeedType.Normal;
                mobile.Race = 0;
                mobile.Hits = 0;
                mobile.HitsMax = 0;
                mobile.Mana = 0;
                mobile.ManaMax = 0;
                mobile.Stamina = 0;
                mobile.StaminaMax = 0;
                mobile.NotorietyFlag = 0;
                mobile.IsRenamable = false;
                mobile.Flags = 0;
                mobile.IsFemale = false;
                mobile.InWarMode = false;
                mobile.IsRunning = false;
                mobile.AnimationInterval = 0;
                mobile.AnimationFrameCount = 0;
                mobile.AnimationRepeatMode = 1;
                mobile.AnimationRepeat = false;
                mobile.AnimationFromServer = false;
                mobile.AnimationForwardDirection = false;
                mobile.LastStepSoundTime = 0;
                mobile.StepSoundOffset = 0;
                mobile.Title = string.Empty;
                mobile.AnimationGroup = 0xFF;
                mobile._isDead = false;
                mobile._isSA_Poisoned = false;
                mobile._lastAnimationIdleDelay = 0;
                mobile.X = 0;
                mobile.Y = 0;
                mobile.Z = 0;
                mobile.Direction = 0;
                mobile.LastAnimationChangeTime = Time.Ticks;
                mobile.TextContainer?.Clear();
                mobile.HitsPercentage = 0;
                mobile.HitsTexture?.Destroy();
                mobile.HitsTexture = null;
                mobile.IsFlipped = false;
                mobile.FrameInfo = Rectangle.Empty;
                mobile.UseObjectHandles = false;
                mobile.ClosedObjectHandles = false;
                mobile.ObjectHandlesOpened = false;
                mobile.AlphaHue = 0;
                mobile.AllowedToDraw = true;
                mobile.IsClicked = false;
                mobile.RemoveFromTile();
                mobile.Clear();
                mobile.Next = null;
                mobile.Previous = null;
                mobile.Name = null;
                
                mobile.HitsRequested = false;

                mobile.CalculateRandomIdleTime();
            }
        );

        private static readonly byte[,] _animationIdle =
        {
            {
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_2,
                (byte) LOW_ANIMATION_GROUP.LAG_FIDGET_1
            },
            {
                (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1, (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_2,
                (byte) HIGHT_ANIMATION_GROUP.HAG_FIDGET_1
            },
            {
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1, (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2,
                (byte) PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3
            }
        };
        private bool _isDead;
        private bool _isSA_Poisoned;
        private long _lastAnimationIdleDelay;

        public Mobile(uint serial) : base(serial)
        {
            LastAnimationChangeTime = Time.Ticks;
            CalculateRandomIdleTime();
        }

        public Mobile() : base(0)
        {
        }


        public Deque<Step> Steps { get; } = new Deque<Step>(Constants.MAX_STEP_COUNT);

        public bool IsParalyzed => ((byte) Flags & 0x01) != 0;
        public bool IsYellowHits => ((byte) Flags & 0x08) != 0;
        public bool IsPoisoned => Client.Version >= ClientVersion.CV_7000 ? _isSA_Poisoned : ((byte) Flags & 0x04) != 0;
        public bool IgnoreCharacters => ((byte) Flags & 0x10) != 0;

        public bool IsDead
        {
            get => Graphic == 0x0192 || Graphic == 0x0193 || Graphic >= 0x025F && Graphic <= 0x0260 || Graphic == 0x2B6 || Graphic == 0x02B7 || _isDead;
            set => _isDead = value;
        }

        public bool IsFlying => Client.Version >= ClientVersion.CV_7000 && ((byte) Flags & 0x04) != 0;

        public virtual bool InWarMode
        {
            get => ((byte) Flags & 0x40) != 0;
            set { }
        }

        public bool IsHuman => Graphic >= 0x0190 && Graphic <= 0x0193 || Graphic >= 0x00B7 && Graphic <= 0x00BA || Graphic >= 0x025D && Graphic <= 0x0260 || Graphic == 0x029A || Graphic == 0x029B || Graphic == 0x02B6 || Graphic == 0x02B7 || Graphic == 0x03DB || Graphic == 0x03DF || Graphic == 0x03E2 || Graphic == 0x02E8 || Graphic == 0x02E9 || Graphic == 0x04E5;

        public bool IsGargoyle => Client.Version >= ClientVersion.CV_7000 && Graphic == 0x029A || Graphic == 0x029B;

        public bool IsMounted
        {
            get
            {
                Item it = FindItemByLayer(Layer.Mount);

                if (it != null && !IsDrivingBoat && it.GetGraphicForAnimation() != 0xFFFF)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsDrivingBoat
        {
            get
            {
                Item it = FindItemByLayer(Layer.Mount);

                return it != null && it.Graphic == 0x3E96;
            }
        }

        protected virtual bool IsWalking => LastStepTime > Time.Ticks - Constants.WALKING_DELAY;
        public bool AnimationForwardDirection;
        public byte AnimationFrameCount;
        public bool AnimationFromServer;
        public byte AnimationGroup = 0xFF;
        public byte AnimationInterval;
        public bool AnimationRepeat;
        public byte AnimationRepeatMode = 1;
        public bool IsFemale;
        public bool IsRenamable;

        public bool IsRunning;
        public long LastStepSoundTime;
        public ushort Mana;
        public ushort ManaMax;
        public NotorietyFlag NotorietyFlag;
        public RaceType Race;
        public CharacterSpeedType SpeedMode = CharacterSpeedType.Normal;
        public ushort Stamina;
        public ushort StaminaMax;
        public int StepSoundOffset;
        public string Title = string.Empty;


        public static Mobile Create(uint serial)
        {
            Mobile mobile = _pool.GetOne();
            mobile.Serial = serial;

            return mobile;
        }


        public Item GetSecureTradeBox()
        {
            for (LinkedObject i = Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (it.Graphic == 0x1E5E && it.Layer == 0)
                {
                    return it;
                }
            }

            return null;
        }

        public void SetSAPoison(bool value)
        {
            _isSA_Poisoned = value;
        }

        private void CalculateRandomIdleTime()
        {
            const int TIME = 30000;
            _lastAnimationIdleDelay = Time.Ticks + (TIME + RandomHelper.GetValue(0, TIME));
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update(totalTime, frameTime);

            if (_lastAnimationIdleDelay < Time.Ticks)
            {
                SetIdleAnimation();
            }

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
            {
                return false;
            }

            GetEndPosition(out int endX, out int endY, out sbyte endZ, out Direction endDir);

            if (endX == x && endY == y && endZ == z && endDir == direction)
            {
                return true;
            }

            if (Steps.Count == 0)
            {
                if (!IsWalking)
                {
                    SetAnimation(0xFF);
                }

                LastStepTime = Time.Ticks;
            }

            Direction moveDir = DirectionHelper.CalculateDirection(endX, endY, x, y);
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
                ref Step step = ref Steps.Back();
                x = step.X;
                y = step.Y;
                z = step.Z;
                dir = (Direction) step.Direction;
            }
        }


        public void SetAnimation
        (
            byte id,
            byte interval = 0,
            byte frameCount = 0,
            byte repeatCount = 0,
            bool repeat = false,
            bool forward = false
        )
        {
            AnimationGroup = id;
            AnimIndex = (sbyte) (forward ? 0 : frameCount);
            AnimationInterval = interval;
            AnimationFrameCount = frameCount;
            AnimationRepeatMode = repeatCount;
            AnimationRepeat = repeat;
            AnimationForwardDirection = forward;
            AnimationFromServer = false;
            LastAnimationChangeTime = Time.Ticks;


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
                AnimationForwardDirection = true;
                AnimationRepeat = false;
                AnimationFromServer = true;


                ushort graphic = GetGraphicForAnimation();

                if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                {
                    return;
                }

                ANIMATION_GROUPS_TYPE type = AnimationsLoader.Instance.DataIndex[graphic].Type;

                if (AnimationsLoader.Instance.DataIndex[graphic].IsUOP && !AnimationsLoader.Instance.DataIndex[graphic].IsValidMUL)
                {
                    // do nothing ?
                }
                else
                {
                    if (!AnimationsLoader.Instance.DataIndex[graphic].HasBodyConversion)
                    {
                        ushort newGraphic = AnimationsLoader.Instance.DataIndex[graphic].Graphic;

                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;

                            ANIMATION_GROUPS_TYPE newType = AnimationsLoader.Instance.DataIndex[graphic].Type;

                            if (newType != type)
                            {
                                type = newType;
                            }
                        }
                    }
                }

                ANIMATION_FLAGS flags = AnimationsLoader.Instance.DataIndex[graphic].Flags;

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
                {
                    return;
                }

                if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                {
                    if (animGroup != ANIMATION_GROUPS.AG_PEOPLE)
                    {
                        if (InWarMode)
                        {
                            AnimationGroup = 28;
                        }
                        else
                        {
                            AnimationGroup = 26;
                        }

                        return;
                    }

                    if (IsGargoyle && IsFlying)
                    {
                        if (RandomHelper.GetValue(0, 2) != 0)
                        {
                            AnimationGroup = 66;
                        }
                        else
                        {
                            AnimationGroup = 67;
                        }

                        return;
                    }
                }

                int first_value = RandomHelper.GetValue(0, 2);

                byte original_value = AnimationGroup;

                AnimationGroup = _animationIdle[(byte) animGroup - 1, first_value];

                if (isLowExtended && AnimationGroup == 18)
                {
                    if (!AnimationsLoader.Instance.IsAnimationExists(graphic, 18) && AnimationsLoader.Instance.IsAnimationExists(graphic, 17))
                    {
                        AnimationGroup = GetReplacedObjectAnimation(graphic, 17);
                    }
                    else
                    {
                        AnimationGroup = 1;
                    }
                }

                if (!AnimationsLoader.Instance.IsAnimationExists(graphic, AnimationGroup))
                {
                    if (first_value == 0)
                    {
                        first_value = 1;
                    }
                    else
                    {
                        first_value = 0;
                    }

                    AnimationGroup = _animationIdle[(byte) animGroup - 1, first_value];

                    if (!AnimationsLoader.Instance.IsAnimationExists(graphic, AnimationGroup))
                    {
                        SetAnimation(original_value);
                    }
                }
            }
        }

        protected virtual bool NoIterateAnimIndex()
        {
            return LastStepTime > Time.Ticks - Constants.WALKING_DELAY && Steps.Count == 0;
        }

        private void ProcessFootstepsSound()
        {
            if (ProfileManager.CurrentProfile.EnableFootstepsSound && IsHuman && !IsHidden && !IsDead && !IsFlying)
            {
                long ticks = Time.Ticks;

                if (Steps.Count != 0 && LastStepSoundTime < ticks)
                {
                    ref Step step = ref Steps.Back();

                    int incID = StepSoundOffset;
                    int soundID = 0x012B;
                    int delaySound = 400;

                    if (IsMounted)
                    {
                        if (step.Run)
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

                    Client.Game.Scene.Audio.PlaySoundWithDistance(soundID, step.X, step.Y);
                    LastStepSoundTime = ticks + delaySound;
                }
            }
        }

        public override void ProcessAnimation(out byte dir, bool evalutate = false)
        {
            ProcessSteps(out dir, evalutate);

            ProcessFootstepsSound();

            if (LastAnimationChangeTime < Time.Ticks && !NoIterateAnimIndex())
            {
                sbyte frameIndex = AnimIndex;

                if (AnimationFromServer && !AnimationForwardDirection)
                {
                    frameIndex--;
                }
                else
                {
                    frameIndex++;
                }

                ushort id = GetGraphicForAnimation();
                byte animGroup = GetGroupForAnimation(this, id, true);

                bool mirror = false;
                AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);
                int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && dir < 5)
                {
                    ushort hue = 0;

                    AnimationDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true).Direction[dir];

                    if (direction != null && (direction.FrameCount == 0 || direction.Frames == null))
                    {
                        AnimationsLoader.Instance.LoadAnimationFrames(id, animGroup, dir, ref direction);
                    }

                    if (direction != null && direction.FrameCount != 0)
                    {
                        direction.LastAccessTime = Time.Ticks;
                        int fc = direction.FrameCount;

                        if (AnimationFromServer)
                        {
                            currentDelay += currentDelay * (AnimationInterval + 1);

                            if (AnimationFrameCount == 0)
                            {
                                AnimationFrameCount = (byte) fc;
                            }
                            else
                            {
                                /*fc -= AnimationFrameCount;

                                if (fc <= 0)
                                    fc = AnimationFrameCount;*/

                                fc = AnimationFrameCount;
                            }

                            if (AnimationForwardDirection)
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
                                    if (fc == 0)
                                    {
                                        frameIndex = 0;
                                    }
                                    else
                                    {
                                        frameIndex = (sbyte) (fc - 1);
                                    }

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

                //for (var item = Items; item != null; item = item.Next)
                //{
                //    Item it = (Item)item;
                //    it.ProcessAnimation(out _, true);
                //}

                LastAnimationChangeTime = Time.Ticks + currentDelay;
            }
        }

        public void ProcessSteps(out byte dir, bool evalutate = false)
        {
            dir = (byte) Direction;
            dir &= 7;

            if (Steps.Count != 0 && !IsDestroyed)
            {
                ref Step step = ref Steps.Front();
                dir = step.Direction;

                if (step.Run)
                {
                    dir &= 7;
                }

                if (evalutate)
                {
                    if (AnimationFromServer)
                    {
                        SetAnimation(0xFF);
                    }

                    int delay = (int) Time.Ticks - (int) LastStepTime;
                    bool mounted = IsMounted || SpeedMode == CharacterSpeedType.FastUnmount || SpeedMode == CharacterSpeedType.FastUnmountAndCantRun || IsFlying;
                    bool run = step.Run;

                    // Client auto movements sync. 
                    // When server sends more than 1 packet in an amount of time less than 100ms if mounted (or 200ms if walking mount)
                    // we need to remove the "teleport" effect.
                    // When delay == 0 means that we received multiple movement packets in a single frame, so the patch becomes quite useless.
                    if (!mounted && Serial != World.Player && Steps.Count > 1 && delay > 0)
                    {
                        mounted = delay <= (run ? MovementSpeed.STEP_DELAY_MOUNT_RUN : MovementSpeed.STEP_DELAY_MOUNT_WALK);
                    }

                    int maxDelay = MovementSpeed.TimeToCompleteMovement(run, mounted) - (int) Client.Game.FrameDelay[1];

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
                        {
                            removeStep = true;
                        }
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
                                AddMessage(MessageType.Label, ResGeneral.Ouch, TextType.CLIENT);
                            }

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
                            {
                                World.Player.Walker.CurrentWalkSequence++;
                            }
                        }

                        X = (ushort) step.X;
                        Y = (ushort) step.Y;
                        Z = step.Z;
                        UpdateScreenPosition();

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

                        if (TNext != null || TPrevious != null)
                        {
                            AddToTile();
                        }

                        LastStepTime = Time.Ticks;
                    }

                    UpdateTextCoordsV();
                }
            }
        }

        public int IsSitting()
        {
            //get
            {
                int result = 0;

                if (IsHuman && !IsMounted && !IsFlying && !TestStepNoChangeDirection(this, GetGroupForAnimation(this, isParent: true)))
                {
                    GameObject start = this;

                    while (start?.TPrevious != null)
                    {
                        start = start.TPrevious;
                    }

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
                                case 0x0E50:
                                case 0x0E51:
                                case 0x0E52:
                                case 0x0E53:
                                case 0x1049:
                                case 0x104A:
                                case 0x11FC:
                                case 0x1207:
                                case 0x1208:
                                case 0x1209:
                                case 0x120A:
                                case 0x120B:
                                case 0x120C:
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
                                case 0x319A:
                                case 0x319B:
                                case 0x35ED:
                                case 0x35EE:
                                case 0x3DFF:
                                case 0x3E00:
                                case 0x4023:
                                case 0x4024:
                                case 0x4027:
                                case 0x4028:
                                case 0x4029:
                                case 0x402A:
                                case 0x4BDC:
                                case 0x4C1B:
                                case 0x4C1E:
                                case 0x4C80:
                                case 0x4C81:
                                case 0x4C82:
                                case 0x4C83:
                                case 0x4C84:
                                case 0x4C85:
                                case 0x4C86:
                                case 0x4C87:
                                case 0x4C88:
                                case 0x4C89:
                                case 0x4C8A:
                                case 0x4C8B:
                                case 0x4C8C:
                                case 0x4C8D:
                                case 0x4C8E:
                                case 0x4C8F:
                                case 0x4DE0:
                                case 0x63BC:
                                case 0x63BD:
                                case 0x63C3:
                                case 0x63C4:
                                case 0x996C:
                                case 0x9977:
                                case 0x9C57:
                                case 0x9C58:
                                case 0x9C59:
                                case 0x9C5A:
                                case 0x9C5D:
                                case 0x9C5E:
                                case 0x9C5F:
                                case 0x9C60:
                                case 0x9C61:
                                case 0x9C62:
                                case 0x9E8E:
                                case 0x9E8F:
                                case 0x9E90:
                                case 0x9E91:
                                case 0x9E9F:
                                case 0x9EA0:
                                case 0x9EA1:
                                case 0x9EA2:
                                case 0xA05C:
                                case 0xA05D:
                                case 0xA05E:
                                case 0xA05F:
                                case 0xA211:
                                case 0xA4EA:
                                case 0xA4EB:
                                case 0xA586:
                                case 0xA587:

                                    for (int i = 0; i < AnimationsLoader.Instance.SittingInfos.Length; i++)
                                    {
                                        if (AnimationsLoader.Instance.SittingInfos[i].Graphic == graphic)
                                        {
                                            result = i + 1;

                                            break;
                                        }
                                    }

                                    break;
                            }
                        }

                        start = start.TNext;
                    }
                }

                return result;
            }
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject) TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject) last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            bool health = ProfileManager.CurrentProfile.ShowMobilesHP;
            int alwaysHP = ProfileManager.CurrentProfile.MobileHPShowWhen;
            int mode = ProfileManager.CurrentProfile.MobileHPType;

            Point p = RealScreenPosition;


            if (IsGargoyle && IsFlying)
            {
                p.Y -= 22;
            }
            else if (!IsMounted)
            {
                p.Y += 22;
            }

            AnimationsLoader.Instance.GetAnimationDimensions
            (
                AnimIndex,
                GetGraphicForAnimation(),
                /*(byte) m.GetDirectionForAnimation()*/
                0,
                /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                0,
                IsMounted,
                /*(byte) m.AnimIndex*/
                0,
                out _,
                out int centerY,
                out _,
                out int height
            );

            p.X += (int) Offset.X + 22;
            p.Y += (int) (Offset.Y - Offset.Z - (height + centerY + 8));
            p = Client.Game.Scene.Camera.WorldToScreen(p);

            if (ObjectHandlesOpened)
            {
                p.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT;
            }

            if (health && HitsTexture != null && mode != 1 && (alwaysHP >= 1 && Hits != HitsMax || alwaysHP == 0))
            {
                p.Y -= HitsTexture.Height;
            }

            for (; last != null; last = (TextObject) last.Previous)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Time.Ticks)
                    {
                        continue;
                    }

                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = p.X - (last.RenderedText.Width >> 1);
                    last.RealScreenPosition.Y = p.Y - offY;
                }
            }

            FixTextCoordinatesInScreen();
        }

        public override void CheckGraphicChange(sbyte animIndex = 0)
        {
            switch (Graphic)
            {
                case 0x0190:
                case 0x0192:
                {
                    IsFemale = false;
                    Race = RaceType.HUMAN;

                    break;
                }

                case 0x0191:
                case 0x0193:
                {
                    IsFemale = true;
                    Race = RaceType.HUMAN;

                    break;
                }

                case 0x025D:
                {
                    IsFemale = false;
                    Race = RaceType.ELF;

                    break;
                }

                case 0x025E:
                {
                    IsFemale = true;
                    Race = RaceType.ELF;

                    break;
                }

                case 0x029A:
                {
                    IsFemale = false;
                    Race = RaceType.GARGOYLE;

                    break;
                }

                case 0x029B:
                {
                    IsFemale = true;
                    Race = RaceType.GARGOYLE;

                    break;
                }
            }
        }

        public override void Destroy()
        {
            uint serial = Serial & 0x3FFFFFFF;

            ClearSteps();

            base.Destroy();

            if (!(this is PlayerMobile))
            {
                UIManager.GetGump<PaperDollGump>(serial)?.Dispose();

                _pool.ReturnOne(this);
            }
        }

        internal struct Step
        {
            public int X, Y;
            public sbyte Z;
            public byte Direction;
            public bool Run;
        }
    }
}