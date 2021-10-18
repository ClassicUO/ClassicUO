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
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal abstract partial class GameEffect : GameObject
    {
        private readonly EffectManager _manager;

        protected GameEffect(EffectManager manager, ushort graphic, ushort hue, int duration, byte speed)
        {
            _manager = manager;

            Graphic = graphic;
            Hue = hue;
            AllowedToDraw = CanBeDrawn(graphic);
            AlphaHue = 0xFF;
            AnimDataFrame = AnimDataLoader.Instance?.CalculateCurrentGraphic(graphic) ?? default;
            IsEnabled = true;
            AnimIndex = 0;
            

            speed *= 10;

            if (speed == 0)
            {
                speed = Constants.ITEM_EFFECT_ANIMATION_DELAY;
            }

            if (AnimDataFrame.FrameInterval == 0)
            {
                IntervalInMs = speed;

                // NOTE:
                // tested on outlands with arrows & bolts 
                // server sends duration = 50 , a very small amount of time so the arrow will be destroyed suddenly
                // im not sure if this is the right fix, but keep it atm
                
                // NOTE 2:
                // this fix causes issue with other effects. It makes perma effects. So bad
                //duration = -1;
            }
            else
            {
                IntervalInMs = (uint)(AnimDataFrame.FrameInterval * speed);
            }

            Duration = duration > 0 ? Time.Ticks + duration : -1;
        }

        public bool IsMoving => Target != null || TargetX != 0 && TargetY != 0;

        public bool CanCreateExplosionEffect;
        public ushort AnimationGraphic = 0xFFFF;
        public AnimDataFrame AnimDataFrame;
        public byte AnimIndex;
        public float AngleToTarget;
        public GraphicEffectBlendMode Blend;
        public long Duration = -1;
        public uint IntervalInMs;
        public bool IsEnabled;
        public long NextChangeFrameTime;
        public GameObject Source;
        protected GameObject Target;
        protected int TargetX;
        protected int TargetY;
        protected int TargetZ;

      
        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);


            if (Source != null && Source.IsDestroyed)
            {
                Destroy();

                return;
            }

            if (IsDestroyed)
            {
                return;
            }

            if (IsEnabled)
            {
                if (Duration < totalTime && Duration >= 0)
                {
                    Destroy();
                }
                else if (NextChangeFrameTime < totalTime)
                {
                    if (AnimDataFrame.FrameCount != 0)
                    {
                        unsafe
                        {
                            AnimationGraphic = (ushort) (Graphic + AnimDataFrame.FrameData[AnimIndex]);
                        }

                        AnimIndex++;

                        if (AnimIndex >= AnimDataFrame.FrameCount)
                        {
                            AnimIndex = 0;
                        }
                    }
                    else
                    {
                        AnimationGraphic = Graphic;
                    }

                    NextChangeFrameTime = (long) totalTime + IntervalInMs;
                }
            }
            else
            {
                AnimationGraphic = Graphic;
            }
        }

        protected (int x, int y, int z) GetSource()
        {
            return Source == null ? (X, Y, Z) : (Source.X, Source.Y, Source.Z);
        }

        protected void CreateExplosionEffect()
        {
            if (CanCreateExplosionEffect)
            {
                (int targetX, int targetY, int targetZ) = GetTarget();

                FixedEffect effect = new FixedEffect(_manager, 0x36CB, Hue, 400, 0);
                effect.Blend = Blend;
                effect.SetSource(targetX, targetY, targetZ);

                _manager.PushToBack(effect);
            }
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
            _manager?.Remove(this);

            AnimIndex = 0;
            Source = null;
            Target = null;
            base.Destroy();
        }
    }
}
