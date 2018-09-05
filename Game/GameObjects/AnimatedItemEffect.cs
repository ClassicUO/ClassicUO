using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.GameObjects
{
    public class AnimatedItemEffect : GameEffect
    {
        public AnimatedItemEffect(Graphic graphic,  Hue hue,  int duration)
        {
            Graphic = graphic;
            Hue = hue;
            Duration = duration;

            Load();
        }

        public AnimatedItemEffect(GameObject source,  Graphic graphic,  Hue hue,  int duration) : this(graphic, hue, duration)
        {
            SetSource(source);
        }

        public AnimatedItemEffect(Serial source,  Graphic graphic,  Hue hue,  int duration) : this(source, 0, 0, 0, graphic, hue, duration)
        {
        }

        public AnimatedItemEffect(int sourceX,  int sourceY,  int sourceZ,  Graphic graphic,  Hue hue,  int duration) : this(graphic, hue, duration)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public AnimatedItemEffect(Serial sourceSerial,  int sourceX,  int sourceY,  int sourceZ,  Graphic graphic,  Hue hue,  int duration) : this(graphic, hue, duration)
        {
            sbyte zSrc = (sbyte)sourceZ;

            GameObject source = World.Get(sourceSerial);
            if (source != null)
            {
                if (sourceSerial.IsMobile)
                {
                    Mobile mob = (Mobile)source;
                    if (mob != World.Player && !mob.IsMoving && (sourceX != 0 || sourceY != 0 || sourceZ != 0))
                    {
                        mob.Position = new Position((ushort)sourceX, (ushort)sourceY, zSrc);
                    }

                    SetSource(mob);
                }
                else if (sourceSerial.IsItem)
                {
                    Item item = (Item)source;
                    if (sourceX != 0 || sourceY != 0 || sourceZ != 0)
                    {
                        item.Position = new Position((ushort)sourceX, (ushort)sourceY, zSrc);
                    }

                    SetSource(item);
                }
            }
        }

        public int Duration { get; set; }
        //public new AnimatedEffectView View => (AnimatedEffectView)base.View;


        protected override View CreateView()
        {
            return new AnimatedEffectView(this);
        }

        public override void UpdateAnimation(double ms)
        {
            base.UpdateAnimation(ms);

            if (LastChangeFrameTime >= Duration && Duration >= 0)
            {
                Dispose();
            }
            else
            {
                (int x, int y, int z) = GetSource();
                Position = new Position((ushort)x, (ushort)y, (sbyte)z);
            }
        }
    }
}