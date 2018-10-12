using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Map;
using ClassicUO.Game.Views;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    class MovingEffect : GameEffect
    {
        private float _timeActive, _timeUntilHit;

        public MovingEffect(Graphic graphic, Hue hue)
        {
            Hue = hue;
            Graphic = graphic;
        }

        public MovingEffect(GameObject source, GameObject target, Graphic graphic, Hue hue) : this(graphic, hue)
        {
            SetSource(source);
            SetTarget(target);
        }

        public MovingEffect(int xSource, int ySource, int zSource, GameObject target, Graphic graphic, Hue hue) : this(
            graphic, hue)
        {
            SetSource(xSource, ySource, zSource);
            SetTarget(target);
        }

        public MovingEffect(int xSource, int ySource, int zSource, int xTarg, int yTarg, int zTarg, Graphic graphic,
            Hue hue) : this(graphic, hue)
        {
            SetSource(xSource, ySource, zSource);
            SetTarget(xTarg, yTarg, zTarg);
        }

        public MovingEffect(Serial src, Serial trg, int xSource, int ySource, int zSource, int xTarget, int yTarget,
            int zTarget, Graphic graphic, Hue hue) : this(graphic, hue)
        {
            sbyte zSourceB = (sbyte)zSource;
            sbyte zTargB = (sbyte)zTarget;

            Entity source = World.Get(src);
            if (source != null)
            {
                if (source is Mobile mobile)
                {
                    SetSource(mobile.Position.X, mobile.Position.Y, mobile.Position.Z);
                    if (mobile != World.Player && !mobile.IsMoving && (xSource | ySource | zSource) != 0)
                        mobile.Position = new Position((ushort)xSource, (ushort)ySource,  zSourceB);
                }
                else if (source is Item)
                {
                    SetSource(source.Position.X, source.Position.Y, source.Position.Z);
                    if ((xSource | ySource | zSource) != 0)
                        source.Position = new Position((ushort)xSource, (ushort)ySource, zSourceB);
                }
                else
                {
                    SetSource(xSource, ySource, zSourceB);
                }
            }
            else
            {
                SetSource(xSource, ySource, zSource);
            }

            Entity target = World.Get(trg);
            if (target != null)
            {
                if (target is Mobile mobile)
                {
                    SetTarget(target);
                    if (mobile != World.Player && !mobile.IsMoving && (xTarget | yTarget | zTarget) != 0)
                        mobile.Position = new Position((ushort)xTarget, (ushort)yTarget, zTargB);
                }
                else if (target is Item)
                {
                    SetSource(target);
                    if ((xTarget | yTarget | zTarget) != 0)
                        target.Position = new Position((ushort)xTarget, (ushort)yTarget, zTargB);
                }
                else
                {
                    SetSource(xTarget, yTarget, zTargB);
                }
            }
        }


        public float AngleToTarget { get; set; }
        protected override View CreateView() => new MovingEffectView(this);


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            (int sx, int sy, int sz) = GetSource();
            (int tx, int ty, int tz) = GetTarget();

            if (_timeUntilHit == 0f)
            {
                _timeActive = 0f;
                _timeUntilHit = (float)Math.Sqrt(Math.Pow((tx - sx), 2) + Math.Pow((ty - sy), 2) + Math.Pow((tz - sz), 2)) * 75f;
            }
            else
            {
                _timeActive += (float)frameMS;
            }

            if (_timeActive >= _timeUntilHit)
            {
                Dispose();
            }
            else
            {
                float x = (sx + (_timeActive / _timeUntilHit) * (float)(tx - sx));
                float y = (sy + (_timeActive / _timeUntilHit) * (float)(ty - sy));
                float z = (sz + (_timeActive / _timeUntilHit) * (float)(tz - sz));

                Position = new Position((ushort)x, (ushort)y, (sbyte)z);
                Tile = World.Map.GetTile((int) x, (int) y);
                Offset = new Vector3(x % 1, y % 1, z % 1);
                AngleToTarget = -((float)Math.Atan2((ty - sy), (tx - sx)) + (float)(Math.PI) * (1f / 4f));
            }
        }
    }
}
