#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class GameEffect : GameObject
    {
        public AnimDataFrame2 AnimDataFrame;

        protected GameEffect()
        {
            Children = new List<GameEffect>();
            AlphaHue = 0xFF;
        }

        public List<GameEffect> Children { get; }

        public GameObject Source;

        protected GameObject Target;

        protected int TargetX;

        protected int TargetY;

        protected int TargetZ;

        public int IntervalInMs;

        public long NextChangeFrameTime;

        public bool IsEnabled;

        public ushort AnimationGraphic = 0xFFFF;

        public bool IsMoving => Target != null || TargetX != 0 && TargetY != 0;

        public GraphicEffectBlendMode Blend;

        public long Duration = -1;

        public void Load()
        {
            AnimDataFrame = AnimDataLoader.Instance.CalculateCurrentGraphic(Graphic);
            IsEnabled = true;
            AnimIndex = 0;

            if (AnimDataFrame.FrameInterval == 0)
            {
                IntervalInMs = Constants.ITEM_EFFECT_ANIMATION_DELAY;
            }
            else
            {
                IntervalInMs = AnimDataFrame.FrameInterval * Constants.ITEM_EFFECT_ANIMATION_DELAY;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);


            if (Source != null && Source.IsDestroyed)
            {
                Destroy();

                return;
            }

            if (IsDestroyed)
                return;

            if (IsEnabled)
            {
                if (Duration < totalMS && Duration >= 0)
                    Destroy();
                //else
                //{
                //    unsafe
                //    {
                //        int count = AnimDataFrame.FrameCount;
                //        if (count == 0)
                //            count = 1;

                //        AnimationGraphic = (Graphic) (Graphic + AnimDataFrame.FrameData[((int) Math.Max(1, (_start / 50d) / Speed)) % count]);
                //    }

                //    _start += frameMS;
                //}

                else if (NextChangeFrameTime < totalMS)
                {

                    if (AnimDataFrame.FrameCount != 0)
                    {
                        unsafe
                        {
                            AnimationGraphic = (ushort) (Graphic + AnimDataFrame.FrameData[AnimIndex]);
                        }

                        AnimIndex++;

                        if (AnimIndex >= AnimDataFrame.FrameCount)
                            AnimIndex = 0;
                    }
                    else
                    {
                        if (Graphic != AnimationGraphic)
                            AnimationGraphic = Graphic;
                    }

                    NextChangeFrameTime = (long) totalMS + IntervalInMs;
                }
            }
            else if (Graphic != AnimationGraphic)
                AnimationGraphic = Graphic;
        }

        public void AddChildEffect(GameEffect effect)
        {
            Children.Add(effect);
        }

        protected (int x, int y, int z) GetSource()
        {
            return Source == null ? (X, Y, Z) : (Source.X, Source.Y, Source.Z);
        }

        public void SetSource(GameObject source)
        {
            Source = source;
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            UpdateScreenPosition();
            AddToTile();
        }

        public void SetSource(int x, int y, int z)
        {
            Source = null;
            X = (ushort) x;
            Y = (ushort) y;
            Z = (sbyte) z;
            UpdateScreenPosition();
            AddToTile();
        }

        protected (int x, int y, int z) GetTarget()
        {
            return Target == null ? (TargetX, TargetY, TargetZ) : (Target.X, Target.Y, Target.Z);
        }

        public void SetTarget(GameObject target)
        {
            Target = target;
        }

        public void SetTarget(int x, int y, int z)
        {
            Target = null;
            TargetX = x;
            TargetY = y;
            TargetZ = z;
        }

        public override void Destroy()
        {
            Source = null;
            Target = null;
            base.Destroy();
        }
    }
}