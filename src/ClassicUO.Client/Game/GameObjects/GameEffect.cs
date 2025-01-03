// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal abstract partial class GameEffect : GameObject
    {
        private readonly EffectManager _manager;

        protected GameEffect(World world, EffectManager manager, ushort graphic, ushort hue, int duration, byte speed) : base(world)
        {
            _manager = manager;

            Graphic = graphic;
            Hue = hue;
            AllowedToDraw = CanBeDrawn(world, graphic);
            AlphaHue = 0xFF;
            AnimDataFrame = Client.Game?.UO?.FileManager?.AnimData?.CalculateCurrentGraphic(graphic) ?? default;
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
        protected ushort TargetX;
        protected ushort TargetY;
        protected sbyte TargetZ;


        public override void Update()
        {
            base.Update();


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
                if (Duration < Time.Ticks && Duration >= 0)
                {
                    Destroy();
                }
                else if (NextChangeFrameTime < Time.Ticks)
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

                    NextChangeFrameTime = (long)Time.Ticks + IntervalInMs;
                }
            }
            else
            {
                AnimationGraphic = Graphic;
            }
        }

        protected (ushort x, ushort y, sbyte z) GetSource()
        {
            return Source == null ? (X, Y, Z) : (Source.X, Source.Y, Source.Z);
        }

        protected void CreateExplosionEffect()
        {
            if (CanCreateExplosionEffect)
            {
                (var targetX, var targetY, var targetZ) = GetTarget();

                FixedEffect effect = new FixedEffect(World, _manager, 0x36CB, Hue, 400, 0);
                effect.Blend = Blend;
                effect.SetSource(targetX, targetY, targetZ);

                _manager.PushToBack(effect);
            }
        }

        public void SetSource(GameObject source)
        {
            Source = source;
            SetInWorldTile(source.X, source.Y, source.Z);
        }

        public void SetSource(ushort x, ushort y, sbyte z)
        {
            Source = null;

            SetInWorldTile(x, y,z);
        }

        protected (ushort x, ushort y, sbyte z) GetTarget()
        {
            return Target == null ? (TargetX, TargetY, TargetZ) : (Target.X, Target.Y, Target.Z);
        }

        public void SetTarget(GameObject target)
        {
            Target = target;
        }

        public void SetTarget(ushort x, ushort y, sbyte z)
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
