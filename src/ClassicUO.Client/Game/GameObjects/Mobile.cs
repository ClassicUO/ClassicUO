#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile : Entity
    {
        //private static readonly QueuedPool<Mobile> _pool = new QueuedPool<Mobile>(
        //    Constants.PREDICTABLE_CHUNKS,
        //    mobile =>
        //    {
        //        mobile.IsDestroyed = false;
        //        mobile.Graphic = 0;
        //        mobile.Steps.Clear();
        //        mobile.Offset = Vector3.Zero;
        //        mobile.SpeedMode = CharacterSpeedType.Normal;
        //        mobile.Race = 0;
        //        mobile.Hits = 0;
        //        mobile.HitsMax = 0;
        //        mobile.Mana = 0;
        //        mobile.ManaMax = 0;
        //        mobile.Stamina = 0;
        //        mobile.StaminaMax = 0;
        //        mobile.NotorietyFlag = 0;
        //        mobile.IsRenamable = false;
        //        mobile.Flags = 0;
        //        mobile.IsFemale = false;
        //        mobile.InWarMode = false;
        //        mobile.IsRunning = false;
        //        mobile._animationInterval = 0;
        //        mobile.AnimationFrameCount = 0;
        //        mobile._animationRepeateMode = 1;
        //        mobile._animationRepeatModeCount = 1;
        //        mobile._animationRepeat = false;
        //        mobile.AnimationFromServer = false;
        //        mobile._isAnimationForwardDirection = false;
        //        mobile.LastStepSoundTime = 0;
        //        mobile.StepSoundOffset = 0;
        //        mobile.Title = string.Empty;
        //        mobile._animationGroup = 0xFF;
        //        mobile._isDead = false;
        //        mobile._isSA_Poisoned = false;
        //        mobile._lastAnimationIdleDelay = 0;
        //        mobile.X = 0;
        //        mobile.Y = 0;
        //        mobile.Z = 0;
        //        mobile.Direction = 0;
        //        mobile.LastAnimationChangeTime = Time.Ticks;
        //        mobile.TextContainer?.Clear();
        //        mobile.HitsPercentage = 0;
        //        mobile.IsFlipped = false;
        //        mobile.FrameInfo = Rectangle.Empty;
        //        mobile.ObjectHandlesStatus = ObjectHandlesStatus.NONE;
        //        mobile.AlphaHue = 0;
        //        mobile.AllowedToDraw = true;
        //        mobile.IsClicked = false;
        //        mobile.RemoveFromTile();
        //        mobile.Clear();
        //        mobile.Next = null;
        //        mobile.Previous = null;
        //        mobile.Name = null;
        //        mobile.ExecuteAnimation = true;
        //        mobile.HitsRequest = HitsRequestStatus.None;

        //        mobile.CalculateRandomIdleTime();
        //    }
        //);

        private static readonly byte[,] _animationIdle =
        {
            {
                (byte)LowAnimationGroup.Fidget1,
                (byte)LowAnimationGroup.Fidget2,
                (byte)LowAnimationGroup.Fidget1
            },
            {
                (byte)HighAnimationGroup.Fidget1,
                (byte)HighAnimationGroup.Fidget2,
                (byte)HighAnimationGroup.Fidget1
            },
            {
                (byte)PeopleAnimationGroup.Fidget1,
                (byte)PeopleAnimationGroup.Fidget2,
                (byte)PeopleAnimationGroup.Fidget3
            }
        };

        private bool _isDead;
        private bool _isSA_Poisoned;
        private long _lastAnimationIdleDelay;
        private bool _isAnimationForwardDirection;
        private byte _animationGroup = 0xFF;
        private byte _animationInterval;
        private bool _animationRepeat;
        private ushort _animationRepeateMode = 1;
        private ushort _animationRepeatModeCount = 1;

        public Mobile(World world, uint serial) : base(world, serial)
        {
            LastAnimationChangeTime = Time.Ticks;
            CalculateRandomIdleTime();
        }

        public Mobile(World world) : base(world, 0) { }

        public Deque<Step> Steps { get; } = new Deque<Step>(Constants.MAX_STEP_COUNT);
        public bool IsParalyzed => (Flags & Flags.Frozen) != 0;
        public bool IsYellowHits => (Flags & Flags.YellowBar) != 0;
        public bool IsPoisoned =>
            Client.Game.UO.Version >= ClientVersion.CV_7000
                ? _isSA_Poisoned
                : (Flags & Flags.Poisoned) != 0;
        public bool IgnoreCharacters => (Flags & Flags.IgnoreMobiles) != 0;

        public bool IsDead
        {
            get =>
                Graphic == 0x0192
                || Graphic == 0x0193
                || (Graphic >= 0x025F && Graphic <= 0x0260)
                || Graphic == 0x2B6
                || Graphic == 0x02B7
                || _isDead;
            set => _isDead = value;
        }

        public bool IsFlying =>
            Client.Game.UO.Version >= ClientVersion.CV_7000 && (Flags & Flags.Poisoned) != 0;

        public virtual bool InWarMode
        {
            get => (Flags & Flags.WarMode) != 0;
            set { }
        }

        public bool IsHuman =>
            Graphic >= 0x0190 && Graphic <= 0x0193
            || Graphic >= 0x00B7 && Graphic <= 0x00BA
            || Graphic >= 0x025D && Graphic <= 0x0260
            || Graphic == 0x029A
            || Graphic == 0x029B
            || Graphic == 0x02B6
            || Graphic == 0x02B7
            || Graphic == 0x03DB
            || Graphic == 0x03DF
            || Graphic == 0x03E2
            || Graphic == 0x02E8
            || Graphic == 0x02E9
            || Graphic == 0x04E5;

        public bool IsGargoyle =>
            Client.Game.UO.Version >= ClientVersion.CV_7000 && Graphic == 0x029A || Graphic == 0x029B;

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

        public byte AnimationFrameCount;
        public bool AnimationFromServer;
        public bool IsFemale;
        public bool IsRenamable;
        public bool IsRunning;
        public long LastStepSoundTime;
        public NotorietyFlag NotorietyFlag;
        public RaceType Race;
        public CharacterSpeedType SpeedMode = CharacterSpeedType.Normal;
        public ushort Stamina;
        public ushort StaminaMax;
        public ushort Mana;
        public ushort ManaMax;
        public int StepSoundOffset;
        public string Title = string.Empty;

        public static Mobile Create(World world, uint serial)
        {
            Mobile mobile = new Mobile(world); // _pool.GetOne();
            mobile.Serial = serial;

            return mobile;
        }

        public Item GetSecureTradeBox()
        {
            for (LinkedObject i = Items; i != null; i = i.Next)
            {
                Item it = (Item)i;

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

        public override void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update();

            if (_lastAnimationIdleDelay < Time.Ticks)
            {
                SetIdleAnimation();
            }

            ProcessAnimation(true);
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
                    step.Direction = (byte)moveDir;
                    step.Run = run;
                    Steps.AddToBack(step);
                }

                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte)moveDir;
                step.Run = run;
                Steps.AddToBack(step);
            }

            if (moveDir != direction)
            {
                step.X = x;
                step.Y = y;
                step.Z = z;
                step.Direction = (byte)direction;
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
                dir = (Direction)step.Direction;
            }
        }

        public void SetAnimation(
            byte id,
            byte interval = 0,
            byte frameCount = 0,
            ushort repeatCount = 0,
            bool repeat = false,
            bool forward = false,
            bool fromServer = false
        )
        {
            _animationGroup = id;
            AnimIndex = (byte)(forward ? 0 : frameCount);
            _animationInterval = interval;
            AnimationFrameCount = (byte)(forward ? 0 : frameCount);
            _animationRepeateMode = repeatCount;
            _animationRepeatModeCount = repeatCount;
            _animationRepeat = repeat;
            _isAnimationForwardDirection = forward;
            AnimationFromServer = fromServer;
            LastAnimationChangeTime = Time.Ticks;

            CalculateRandomIdleTime();
        }

        public void SetIdleAnimation()
        {
            CalculateRandomIdleTime();

            if (!IsMounted && !InWarMode && ExecuteAnimation)
            {
                AnimIndex = 0;
                AnimationFrameCount = 0;
                _animationInterval = 1;
                _animationRepeateMode = 1;
                _animationRepeatModeCount = 1;
                _isAnimationForwardDirection = true;
                _animationRepeat = false;
                AnimationFromServer = true;

                ushort graphic = GetGraphicForAnimation();

                if (graphic >= Client.Game.UO.Animations.MaxAnimationCount)
                {
                    return;
                }

                //byte action = 0;
                //ushort hue = 0;

                //Client.Game.UO.Animations.ReplaceAnimationValues(
                //    ref graphic,
                //    ref action,
                //    ref hue,
                //    out var useUOP
                //);

                AnimationGroupsType type = Client.Game.UO.Animations.GetAnimType(graphic);
                AnimationFlags  flags = Client.Game.UO.Animations.GetAnimFlags(graphic);
                AnimationGroups animGroup = AnimationGroups.None;

                bool isLowExtended = false;

                if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
                {
                    isLowExtended = true;
                    type = AnimationGroupsType.Monster;
                }
                else if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
                {
                    type = AnimationGroupsType.Animal;
                }

                switch (type)
                {
                    case AnimationGroupsType.SeaMonster:
                    case AnimationGroupsType.Monster:
                        animGroup = AnimationGroups.High;

                        break;

                    case AnimationGroupsType.Animal:
                        animGroup = AnimationGroups.Low;

                        break;

                    case AnimationGroupsType.Human:
                    case AnimationGroupsType.Equipment:
                        animGroup = AnimationGroups.People;

                        break;
                }

                if (animGroup == 0)
                {
                    return;
                }

                if ((flags & AnimationFlags.UseUopAnimation) != 0)
                {
                    if (animGroup != AnimationGroups.People)
                    {
                        if (InWarMode)
                        {
                            _animationGroup = 28;
                        }
                        else
                        {
                            _animationGroup = 26;
                        }

                        return;
                    }

                    if (IsGargoyle && IsFlying)
                    {
                        if (RandomHelper.GetValue(0, 2) != 0)
                        {
                            _animationGroup = 66;
                        }
                        else
                        {
                            _animationGroup = 67;
                        }

                        return;
                    }
                }

                int first_value = RandomHelper.GetValue(0, 2);

                byte original_value = _animationGroup;

                _animationGroup = _animationIdle[(byte)animGroup - 1, first_value];

                if (isLowExtended && _animationGroup == 18)
                {
                    if (
                        !Client.Game.UO.Animations.AnimationExists(graphic, 18)
                        && Client.Game.UO.Animations.AnimationExists(graphic, 17)
                    )
                    {
                        _animationGroup = GetReplacedObjectAnimation(graphic, 17);
                    }
                    else
                    {
                        _animationGroup = 1;
                    }
                }

                if (!Client.Game.UO.Animations.AnimationExists(graphic, _animationGroup))
                {
                    if (first_value == 0)
                    {
                        first_value = 1;
                    }
                    else
                    {
                        first_value = 0;
                    }

                    _animationGroup = _animationIdle[(byte)animGroup - 1, first_value];

                    if (!Client.Game.UO.Animations.AnimationExists(graphic, _animationGroup))
                    {
                        SetAnimation(original_value);
                    }
                }
            }
        }

        private bool NoIterateAnimIndex()
        {
            return !ExecuteAnimation
                || (LastStepTime > Time.Ticks - Constants.WALKING_DELAY && Steps.Count == 0);
        }

        private void ProcessFootstepsSound()
        {
            if (
                ProfileManager.CurrentProfile.EnableFootstepsSound
                && IsHuman
                && !IsHidden
                && !IsDead
                && !IsFlying
            )
            {
                if (Steps.Count != 0 && LastStepSoundTime < Time.Ticks)
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

                    Client.Game.Audio.PlaySoundWithDistance(World, soundID, step.X, step.Y);
                    LastStepSoundTime = Time.Ticks + delaySound;
                }
            }
        }

        public override void ProcessAnimation(bool evalutate = false)
        {
            ProcessSteps(out var dir, evalutate);
            ProcessFootstepsSound();

            if (LastAnimationChangeTime < Time.Ticks && !NoIterateAnimIndex())
            {
                ushort id = GetGraphicForAnimation();
                byte action = GetGroupForAnimation(this, id, true);

                bool mirror = false;
                AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);
                int currentDelay = Constants.CHARACTER_ANIMATION_DELAY;

                if (id < Client.Game.UO.Animations.MaxAnimationCount && dir < 5)
                {
                    var frames = Client.Game.UO.Animations.GetAnimationFrames(
                        id,
                        action,
                        dir,
                        out _,
                        out _
                    );

                    if (frames.Length != 0)
                    {
                        int fc = frames.Length;

                        int frameIndex =
                            AnimIndex
                            + (AnimationFromServer && !_isAnimationForwardDirection ? -1 : 1);

                        if (AnimationFromServer)
                        {
                            currentDelay += currentDelay * (_animationInterval + 1);

                            if (AnimationFrameCount == 0)
                            {
                                AnimationFrameCount = (byte)fc;
                            }
                            else
                            {
                                fc = AnimationFrameCount;
                            }

                            if (_isAnimationForwardDirection && frameIndex >= fc)
                            {
                                frameIndex = 0;
                            }
                            else if (!_isAnimationForwardDirection && frameIndex < 0)
                            {
                                if (fc == 0)
                                {
                                    frameIndex = 0;
                                }
                                else
                                {
                                    frameIndex = (byte)(frames.Length - 1);
                                }
                            }
                            else
                            {
                                goto SKIP;
                            }

                            if (_animationRepeateMode == 0) // play animation infinite time
                            {
                                goto SKIP;
                            }

                            if (--_animationRepeateMode > 0) // play animation n times
                            {
                                goto SKIP;
                            }

                            if (_animationRepeat)
                            {
                                _animationRepeatModeCount = _animationRepeateMode;

                                _animationRepeat = false;
                            }
                            else
                            {
                                SetAnimation(0xFF);
                            }

                            SKIP:
                            ;
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

                        AnimIndex = (byte)(frameIndex % frames.Length);
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

                LastAnimationChangeTime = Time.Ticks + currentDelay;
            }
        }

        public void ProcessSteps(out byte dir, bool evalutate = false)
        {
            dir = (byte)Direction;
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

                    int delay = (int)Time.Ticks - (int)LastStepTime;
                    bool mounted =
                        IsMounted
                        || SpeedMode == CharacterSpeedType.FastUnmount
                        || SpeedMode == CharacterSpeedType.FastUnmountAndCantRun
                        || IsFlying;
                    bool run = step.Run;

                    // Client auto movements sync.
                    // When server sends more than 1 packet in an amount of time less than 100ms if mounted (or 200ms if walking mount)
                    // we need to remove the "teleport" effect.
                    // When delay == 0 means that we received multiple movement packets in a single frame, so the patch becomes quite useless.
                    if (!mounted && Serial != World.Player && Steps.Count > 1 && delay > 0)
                    {
                        mounted =
                            delay
                            <= (
                                run
                                    ? MovementSpeed.STEP_DELAY_MOUNT_RUN
                                    : MovementSpeed.STEP_DELAY_MOUNT_WALK
                            );
                    }

                    int maxDelay =
                        MovementSpeed.TimeToCompleteMovement(run, mounted)
                        - (int)Client.Game.FrameDelay[1];

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

                               Pathfinder.GetNewXY((byte)(step.Direction & 7), ref absX, ref absY);

                                badStep = absX != step.X || absY != step.Y;
                            }
                        }

                        if (badStep)
                        {
                            removeStep = true;
                        }
                        else
                        {
                            float steps = maxDelay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                            float x = delay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                            float y = x;
                            Offset.Z = (sbyte)((step.Z - Z) * x * (4.0f / steps));
                            MovementSpeed.GetPixelOffset(step.Direction, ref x, ref y, steps);
                            Offset.X = (sbyte)x;
                            Offset.Y = (sbyte)y;
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

                            if (
                                World.Player.Walker.StepInfos[
                                    World.Player.Walker.CurrentWalkSequence
                                ].Accepted
                            )
                            {
                                int sequence = World.Player.Walker.CurrentWalkSequence + 1;

                                if (sequence < World.Player.Walker.StepsCount)
                                {
                                    int count = World.Player.Walker.StepsCount - sequence;

                                    for (int i = 0; i < count; i++)
                                    {
                                        World.Player.Walker.StepInfos[sequence - 1] = World
                                            .Player
                                            .Walker
                                            .StepInfos[sequence];

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

                        X = (ushort)step.X;
                        Y = (ushort)step.Y;
                        Z = step.Z;
                        UpdateScreenPosition();

                        if (World.InGame && Serial == World.Player)
                        {
                            World.Player.CloseRangedGumps();
                        }

                        Direction = (Direction)step.Direction;
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

        public bool TryGetSittingInfo(out AnimationsLoader.SittingInfoData data)
        {
            ushort result = 0;

            if (
                IsHuman
                && !IsMounted
                && !IsFlying
                && !TestStepNoChangeDirection(this, GetGroupForAnimation(this, isParent: true))
            )
            {
                GameObject start = this;

                while (start?.TPrevious != null)
                {
                    start = start.TPrevious;
                }

                while (start != null && result == 0)
                {
                    if (
                        (start is Item || start is Static || start is Multi)
                        && Math.Abs(Z - start.Z) <= 1
                    )
                    {
                        if (ChairTable.Table.TryGetValue(start.Graphic, out data))
                        {
                            return true;
                        }
                    }

                    start = (GameObject)start.TNext;
                }
            }

            data = AnimationsLoader.SittingInfoData.Empty;
            return false;
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject)TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject)last.Next;
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

            Client.Game.UO.Animations.GetAnimationDimensions(
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

            p.X += (int)Offset.X + 22;
            p.Y += (int)(Offset.Y - Offset.Z - (height + centerY + 8));
            p = Client.Game.Scene.Camera.WorldToScreen(p);

            if (ObjectHandlesStatus == ObjectHandlesStatus.DISPLAYING)
            {
                p.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT;
            }

            if (
                health
                && HitsTexture != null
                && mode != 1
                && (alwaysHP >= 1 && Hits != HitsMax || alwaysHP == 0)
            )
            {
                p.Y -= HitsTexture.Height;
            }

            for (; last != null; last = (TextObject)last.Previous)
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

        public override void CheckGraphicChange(byte animIndex = 0)
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

                //_pool.ReturnOne(this);
            }
        }

        internal struct Step
        {
            public int X,
                Y;
            public sbyte Z;
            public byte Direction;
            public bool Run;
        }
    }
}
