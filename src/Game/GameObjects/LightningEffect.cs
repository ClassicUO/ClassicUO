namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class LightningEffect : GameEffect
    {
        public LightningEffect(ushort hue)
        {
            Graphic = 0x4E20;
            Hue = hue;
            IsEnabled = true;
            IntervalInMs = Constants.ITEM_EFFECT_ANIMATION_DELAY;
            AnimIndex = 0;
        }

        public LightningEffect(GameObject source, ushort hue) : this(hue)
        {
            SetSource(source);
        }

        public LightningEffect(int x, int y, int z, ushort hue) : this(hue)
        {
            SetSource(x, y, z);
        }

        public LightningEffect(uint src, int x, int y, int z, ushort hue) : this(hue)
        {
            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(x, y, z);
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (!IsDestroyed)
            {
                if (AnimIndex >= 10) //TODO: fix time
                {
                    World.RemoveEffect(this);
                }
                else
                {
                    AnimationGraphic = (ushort) (Graphic + AnimIndex);

                    if (NextChangeFrameTime < totalTime)
                    {
                        AnimIndex++;
                        NextChangeFrameTime = (long) totalTime + IntervalInMs;
                    }

                    (int x, int y, int z) = GetSource();

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
}