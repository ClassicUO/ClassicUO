using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    class BandageGump : Gump
    {
        const byte _iconSize = 16, _spaceSize = 2, _borderSize = 2;
        private AlphaBlendControl _background;
        private Label _text;
        private TextureControl _icon;
        private PlayerMobile Mobile;

        public BandageGump(PlayerMobile mobile) : base(mobile.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;

            Mobile = mobile;
            
            BuildGump();
        }
        
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Engine.Profile == null ||
                Engine.Profile.Current == null ||
                !Engine.Profile.Current.BandageGump ||
                Mobile == null ||
                Mobile.IsDestroyed ||
                Mobile.EnergyResistance == 0)
                return false;

            _text.Text = $"{Mobile.EnergyResistance}";
            
            Width = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
            Height = _borderSize * 2 + _iconSize;

            _background.Width = Width;
            _background.Height = Height;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            int gx = Engine.Profile.Current.GameWindowPosition.X;
            int gy = Engine.Profile.Current.GameWindowPosition.Y;
            int w = Engine.Profile.Current.GameWindowSize.X;
            int h = Engine.Profile.Current.GameWindowSize.Y;

            x = gx + Mobile.RealScreenPosition.X;
            y = gy + Mobile.RealScreenPosition.Y;

            x += (int) Mobile.Offset.X + 22;
            y += (int) (Mobile.Offset.Y - Mobile.Offset.Z) + 22;

            x = (int) (x / scale);
            y = (int) (y / scale);
            x -= (int) (gx / scale);
            y -= (int) (gy / scale);
            x += gx;
            y += gy;

            x -= Width >> 1;
            x += 5;
            y += 10;

            x += Engine.Profile.Current.BandageGumpOffset.X;
            y += Engine.Profile.Current.BandageGumpOffset.Y;

            Y = y;
            X = x;

            return base.Draw(batcher, x, y);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDestroyed)
            {
                Dispose();

                return;
            }
        }

        private void BuildGump()
        {
            _background = new AlphaBlendControl()
            {
                Alpha = 0.6f
            };
            
            _text = new Label("", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize + _iconSize + _spaceSize + 3,
                Y = _borderSize - 2
            };

            _icon = new TextureControl(){
                AcceptMouseInput = false
            };

            _icon.Texture = FileManager.Art.GetTexture(0x0E21);
            _icon.Hue = 0;
            _icon.X = _borderSize;
            _icon.Y = _borderSize - 1; // slight offset due to imgs offset
            _icon.Width = _iconSize;
            _icon.Height = _iconSize;
            
            Add(_background);
            Add(_text);
            Add(_icon);
        }
    }
}
