using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Views;

namespace ClassicUO.Game.GameObjects
{
    class LightningEffect : GameEffect
    {
        public LightningEffect(Hue hue)
        {
            Graphic = 0x4E20;
            Hue = hue;

            IsEnabled = true;
            Speed = 50;
            AnimIndex = 0;
            //Load();
        }

        public LightningEffect(GameObject source, Hue hue) : this(hue)
        {
            SetSource(source);
        }

        public LightningEffect(int x, int y, int z, Hue hue) : this(hue)
        {
            SetSource(x, y, z);
        }

        public LightningEffect(Serial src, int x, int y, int z, Hue hue) : this(hue)
        {
            Entity source = World.Get(src);
            if (source != null)
                SetSource(source);
            else
                SetSource(x, y, z);
        }


        protected override View CreateView() => new LightningEffectView(this);


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (!IsDisposed)
            {
                if (AnimIndex >= 10) //TODO: fix time
                    Dispose();
                else
                {
                    Graphic = (Graphic)(Graphic + AnimIndex++);

                    (int x, int y, int z) = GetSource();

                    if (Position.X != x || Position.Y != y || Position.Z != z)
                    {
                        Position = new Position((ushort)x, (ushort)y, (sbyte)z);
                        Tile = World.Map.GetTile(x, y);
                    }
                }
            }
        }


    }
}
