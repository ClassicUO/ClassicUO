
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class TargetLineGump : Gump
    {
        private readonly GumpPic _background;
        private readonly GumpPicWithWidth _hp;

        public TargetLineGump(Mobile mobile) : base(mobile.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;

            Add(_background = new GumpPic(0,0, 0x1068, 0));
            Add(_hp = new GumpPicWithWidth(0, 0, 0x1069, 0, 1));

            Mobile = mobile;
        }

        public Mobile Mobile { get; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDisposed)
            {
                Dispose();
                Engine.UI.RemoveTargetLineGump(Mobile);
                return;
            }

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


            _background.Hue = Notoriety.GetHue(Mobile.NotorietyFlag);;
        }

        public override bool Draw(Batcher2D batcher, Point position)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null)
                return false;

            Point gWinPos = Engine.Profile.Current.GameWindowPosition;
            Point gWinSize = Engine.Profile.Current.GameWindowSize;
            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            if (Mobile != null && !Mobile.IsDisposed)
            {
                float x = (Mobile.RealScreenPosition.X + gWinPos.X) / scale;
                float y = (Mobile.RealScreenPosition.Y + gWinPos.Y) / scale;

                X = (int)(x + Mobile.Offset.X) - Width / 2 + 22;
                Y = (int)(y + Mobile.Offset.Y - Mobile.Offset.Z) + 22;
            }

            if (X < gWinPos.X || X + Width > gWinPos.X + gWinSize.X)
                return false;
            if (Y < gWinPos.Y || Y + Height > gWinPos.Y + gWinSize.Y)
                return false;

            position.X = X;
            position.Y = Y;

            return base.Draw(batcher, position);
        }
    }
}
