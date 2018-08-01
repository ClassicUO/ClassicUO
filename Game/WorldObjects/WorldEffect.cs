using System.Collections.Generic;
using ClassicUO.AssetsLoader;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldEffect : WorldObject
    {
        private readonly List<WorldEffect> _children;

        protected WorldEffect() : base(World.Map)
        {
            _children = new List<WorldEffect>();
        }

        public IReadOnlyList<WorldEffect> Children => _children;

        protected WorldObject Source { get; set; }
        protected WorldObject Target { get; set; }

        protected int SourceX { get; set; }
        protected int SourceY { get; set; }
        protected int SourceZ { get; set; }

        protected int TargetX { get; set; }
        protected int TargetY { get; set; }
        protected int TargetZ { get; set; }

        protected AnimDataFrame AnimDataFrame { get; set; }


        public int Speed { get; set; }
        public long LastChangeFrameTime { get; set; }
        public bool IsEnabled { get; set; }
        public Graphic AnimationGraphic { get; set; }


        public void Load()
        {
            AnimDataFrame = AnimData.CalculateCurrentGraphic(Graphic);
            IsEnabled = true;
            AnimIndex = (sbyte) AnimDataFrame.FrameStart;
            Speed = AnimDataFrame.FrameInterval * 45;
        }

        public virtual void UpdateAnimation(in double ms)
        {
            if (IsEnabled)
            {
                if (LastChangeFrameTime < World.Ticks)
                {
                    AnimationGraphic = (Graphic) (Graphic + AnimDataFrame.FrameData[AnimIndex]);
                    AnimIndex++;

                    if (AnimIndex >= AnimDataFrame.FrameCount)
                        AnimIndex = (sbyte) AnimDataFrame.FrameStart;

                    LastChangeFrameTime = World.Ticks + Speed;
                }
            }
            else if (Graphic != AnimationGraphic) AnimationGraphic = Graphic;
        }


        public void AddChildEffect(in WorldEffect effect)
        {
            _children.Add(effect);
        }

        protected (int x, int y, int z) GetSource()
        {
            if (Source == null)
                return (SourceX, SourceY, SourceZ);
            return (Source.Position.X, Source.Position.Y, Source.Position.Z);
        }

        public void SetSource(in WorldObject source)
        {
            Source = source;
        }

        public void SetSource(in int x, in int y, in int z)
        {
            Source = null;
            SourceX = x;
            SourceY = y;
            SourceZ = z;
        }

        protected (int x, int y, int z) GetTarget()
        {
            if (Target == null)
                return (TargetX, TargetY, TargetZ);
            return (Target.Position.X, Target.Position.Y, Target.Position.Z);
        }

        public void SetTarget(in WorldObject target)
        {
            Target = target;
        }

        public void SetTarget(in int x, in int y, in int z)
        {
            Target = null;
            TargetX = x;
            TargetY = y;
            TargetZ = z;
        }

        public override void Dispose()
        {
            Source = null;
            Target = null;
            base.Dispose();
        }
    }
}