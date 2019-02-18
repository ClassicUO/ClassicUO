
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class TargetLineGump : Gump
    {
        private readonly GumpPic _background;
        private readonly GumpPicWithWidth _hp;
        private Mobile _mobile;

        public static TargetLineGump TTargetLineGump { get; set; }

        public TargetLineGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;

            Add(_background = new GumpPic(0,0, 0x1068, 0));
            Add(_hp = new GumpPicWithWidth(0, 0, 0x1069, 0, 1));
        }

        public Mobile Mobile
        {
            get => _mobile;
            set
            {
                _mobile = value;
                LocalSerial = _mobile.Serial;
            }
        }

        public Hue HpHue
        {
            get => _hp.Hue;
            set
            {
                if (_hp.Hue != value)
                {
                    _hp.Hue = value;
                }
            }
        }

        public Hue BackgroudHue
        {
            get => _background.Hue;
            set
            {
                if (_background.Hue != value)
                {
                    _background.Hue = value;
                }
            }
        }

        public void SetMobile(Mobile mob)
            => Mobile = mob;

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDisposed)
            {
                IsVisible = false;
                return;
            }

            if (!IsVisible)
                IsVisible = true;

            int per = Mobile.HitsMax;

            if (per > 0)
            {
                per = (Mobile.Hits * 100) / per;

                if (per > 100)
                    per = 100;

                if (per < 1)
                    per = 0;
                else
                    per = (34 * per) / 100;
            }

            _hp.Percent = per;
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null)
                return false;

            Point gWinPos = Engine.Profile.Current.GameWindowPosition;
            Point gWinSize = Engine.Profile.Current.GameWindowSize;
            float scale = Engine.Profile.Current.ScaleZoom;

            if (Mobile != null && !Mobile.IsDisposed)
            {
                float x = (Mobile.RealScreenPosition.X + gWinPos.X + 9) / scale;
                float y = (Mobile.RealScreenPosition.Y + gWinPos.Y + 30) / scale;

                X = (int)(x + Mobile.Offset.X);
                Y = (int)(y + Mobile.Offset.Y - Mobile.Offset.Z);
            }

            if (X < gWinPos.X || X + Width > gWinPos.X + gWinSize.X)
                return false;
            if (Y < gWinPos.Y || Y + Height > gWinPos.Y + gWinSize.Y)
                return false;

            return base.Draw(batcher, position, hue);
        }
    }
}
