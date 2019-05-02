using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TargetLineGump : Gump
    {
        private readonly GumpPic _background;
        private readonly GumpPicWithWidth _hp;

        public TargetLineGump(Mobile mobile) : base(mobile.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;

            Add(_background = new GumpPic(0, 0, 0x1068, 0));
            Add(_hp = new GumpPicWithWidth(0, 0, 0x1069, 0, 1));

            Mobile = mobile;
        }

        public Mobile Mobile { get; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDestroyed)
            {
                Dispose();
                Engine.UI.RemoveTargetLineGump(Mobile);

                return;
            }

            int per = Mobile.HitsMax;

            if (per > 0)
            {
                per = Mobile.Hits * 100 / per;

                if (per > 100)
                    per = 100;

                if (per < 1)
                    per = 0;
                else
                    per = 34 * per / 100;
            }

            _hp.Percent = per;

            if (Mobile.IsPoisoned)
            {
                if (_hp.Hue != 63)
                    _hp.Hue = 63;
            }
            else if (Mobile.IsYellowHits)
            {
                if (_hp.Hue != 53)
                    _hp.Hue = 53;
            }
            else if (_hp.Hue != 90)
                _hp.Hue = 90;


            _background.Hue = Notoriety.GetHue(Mobile.NotorietyFlag);
            ;
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || Mobile == null || Mobile.IsDestroyed)
                return false;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            int gx = Engine.Profile.Current.GameWindowPosition.X;
            int gy = Engine.Profile.Current.GameWindowPosition.Y;
            int w = Engine.Profile.Current.GameWindowSize.X;
            int h = Engine.Profile.Current.GameWindowSize.Y;

            x = (int) ((Mobile.RealScreenPosition.X + Mobile.Offset.X - (Width >> 1) + 22) / scale);
            y = (int) ((Mobile.RealScreenPosition.Y + Mobile.Offset.Y - Mobile.Offset.Z + 22) / scale);

            x += gx + 6;
            y += gy;

            X = x;
            Y = y;

            if (x < gx || x + Width > gx + w)
                return false;

            if (y < gy || y + Height > gy + h)
                return false;


            return base.Draw(batcher, x, y);
        }
    }
}