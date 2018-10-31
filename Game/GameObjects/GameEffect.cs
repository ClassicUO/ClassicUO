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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    public abstract class GameEffect : GameObject, IDeferreable
    {
        private readonly List<GameEffect> _children;

        protected GameEffect() : base(World.Map)
        {
            _children = new List<GameEffect>();
        }

        public IReadOnlyList<GameEffect> Children => _children;

        protected GameObject Source { get; set; }

        protected GameObject Target { get; set; }

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

        public Graphic AnimationGraphic { get; set; } = Graphic.Invalid;

        public bool IsMoving => Target != null || TargetX != 0 && TargetY != 0;

        public GraphicEffectBlendMode Blend { get; set; }

        public bool IsItemEffect => Source is Item item && item.OnGround;

        public long Duration { get; set; } = -1;

        public DeferredEntity DeferredObject { get; set; }

        public void Load()
        {
            AnimDataFrame = AnimData.CalculateCurrentGraphic(Graphic);
            IsEnabled = true;
            AnimIndex = (sbyte) AnimDataFrame.FrameStart;
            Speed = AnimDataFrame.FrameInterval * 45;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (IsEnabled)
            {
                if (Duration < totalMS && Duration >= 0)
                    Dispose();
                else if (LastChangeFrameTime < totalMS)
                {
                    if (AnimDataFrame.FrameCount > 0)
                    {
                        AnimationGraphic = (Graphic) (Graphic + AnimDataFrame.FrameData[AnimIndex]);
                        AnimIndex++;
                        if (AnimIndex >= AnimDataFrame.FrameCount) AnimIndex = (sbyte) AnimDataFrame.FrameStart;
                    }
                    else
                    {
                        if (Graphic != AnimationGraphic)
                            AnimationGraphic = Graphic;
                    }

                    LastChangeFrameTime = (long) totalMS + Speed;
                }
            }
            else if (Graphic != AnimationGraphic) AnimationGraphic = Graphic;
        }

        public void AddChildEffect(GameEffect effect)
        {
            _children.Add(effect);
        }

        protected (int x, int y, int z) GetSource()
        {
            if (Source == null) return (SourceX, SourceY, SourceZ);

            return (Source.Position.X, Source.Position.Y, Source.Position.Z);
        }

        public void SetSource(GameObject source)
        {
            Source = source;
        }

        public void SetSource(int x, int y, int z)
        {
            Source = null;
            SourceX = x;
            SourceY = y;
            SourceZ = z;
        }

        protected (int x, int y, int z) GetTarget()
        {
            if (Target == null) return (TargetX, TargetY, TargetZ);

            return (Target.Position.X, Target.Position.Y, Target.Position.Z);
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

        public override void Dispose()
        {
            Source = null;
            Target = null;
            base.Dispose();
        }
    }
}