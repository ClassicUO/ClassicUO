using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class AnimatedItemEffect : GameEffect
    {
        public AnimatedItemEffect(ushort graphic, ushort hue, int duration, int speed)
        {
            Graphic = graphic;
            Hue = hue;
            Duration = duration > 0 ? Time.Ticks + duration : -1;
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(graphic);
            Load();
        }

        public AnimatedItemEffect
        (
            int sourceX,
            int sourceY,
            int sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            int speed
        ) : this(graphic, hue, duration, speed)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public AnimatedItemEffect
        (
            uint sourceSerial,
            int sourceX,
            int sourceY,
            int sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            int speed
        ) : this(graphic, hue, duration, speed)
        {
            Entity source = World.Get(sourceSerial);

            if (source != null && SerialHelper.IsValid(sourceSerial))
            {
                SetSource(source);
            }
            else
            {
                SetSource(sourceX, sourceY, sourceZ);
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (!IsDestroyed)
            {
                (int x, int y, int z) = GetSource();

                if (Source != null)
                {
                    Offset = Source.Offset;
                }

                if (X != x || Y != y || Z != z)
                {
                    X = (ushort) x;
                    Y = (ushort) y;
                    Z = (sbyte) z;
                    UpdateScreenPosition();
                    AddToTile();
                }
            }
        }
    }
}