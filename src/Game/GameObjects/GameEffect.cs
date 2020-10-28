using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class GameEffect : GameObject
    {
        protected GameEffect()
        {
            Children = new List<GameEffect>();
            AlphaHue = 0xFF;
        }

        public List<GameEffect> Children { get; }

        public bool IsMoving => Target != null || TargetX != 0 && TargetY != 0;
        public ushort AnimationGraphic = 0xFFFF;
        public AnimDataFrame2 AnimDataFrame;
        public byte AnimIndex;

        public GraphicEffectBlendMode Blend;

        public long Duration = -1;

        public int IntervalInMs;

        public bool IsEnabled;

        public long NextChangeFrameTime;

        public GameObject Source;

        protected GameObject Target;

        protected int TargetX;

        protected int TargetY;

        protected int TargetZ;

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

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);


            if (Source != null && Source.IsDestroyed)
            {
                World.RemoveEffect(this);

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
                    World.RemoveEffect(this);
                }
                //else
                //{
                //    unsafe
                //    {
                //        int count = AnimDataFrame.FrameCount;
                //        if (count == 0)
                //            count = 1;

                //        AnimationGraphic = (Graphic) (Graphic + AnimDataFrame.FrameData[((int) Math.Max(1, (_start / 50d) / Speed)) % count]);
                //    }

                //    _start += frameTime;
                //}

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
                        if (Graphic != AnimationGraphic)
                        {
                            AnimationGraphic = Graphic;
                        }
                    }

                    NextChangeFrameTime = (long) totalTime + IntervalInMs;
                }
            }
            else if (Graphic != AnimationGraphic)
            {
                AnimationGraphic = Graphic;
            }
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
            AnimIndex = 0;
            Source = null;
            Target = null;
            base.Destroy();
        }
    }
}