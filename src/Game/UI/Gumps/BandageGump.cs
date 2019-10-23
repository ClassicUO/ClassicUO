using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    class BandageGump : Gump
    {
        const byte _iconSize = 16, _spaceSize = 2, _borderSize = 2;
        public uint Time { get; set; }
        private bool _useTime;
        private uint _startTime;
        private float _updateTime;
        private AlphaBlendControl _background;
        private Label _text;
        private TextureControl _icon;
        private static int[] _startAtClilocs = new int[]
        {
            500956,
            500957,
            500958,
            500959,
            500960
        };
        private static int[] _stopAtClilocs = new int[]
        {
            500955,
            500962,
            500963,
            500964,
            500965,
            500966,
            500967,
            500968,
            500969,
            503252,
            503253,
            503254,
            503255,
            503256,
            503257,
            503258,
            503259,
            503260,
            503261,
            1010058,
            1010648,
            1010650,
            1060088,
            1060167
        };

        public BandageGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;

            BuildGump();
        }

        public void Start()
        {
            _useTime = true;
            _startTime = Engine.Ticks;
        }

        public void Stop()
        {
            _useTime = false;
        }

        public void OnMessage(string text, Hue hue, string name, bool isunicode = true)
        {
            // attempt to pick up on things that OnCliloc missed
            if (name != "System" && text.Length <= 0)
                return;

            // stop the timer
            for (int i=0;i<_stopAtClilocs.Length;i++)
            {
                if (FileManager.Cliloc.GetString(_stopAtClilocs[i]) == text )
                {
                    Stop();
                    return;
                }
            }

            // start the timer
            for (int i=0;i<_stopAtClilocs.Length;i++)
            {
                if (FileManager.Cliloc.GetString(_stopAtClilocs[i]) == text )
                {
                    Start();
                    return;
                }
            }
        }

        public void OnCliloc(uint cliloc)
        {
            // stop the timer
            for (int i=0;i<_stopAtClilocs.Length;i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }

            // start the timer
            for (int i=0;i<_startAtClilocs.Length;i++)
            {
                if (_startAtClilocs[i] == cliloc)
                {
                    Start();
                    return;
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible ||
                Engine.Profile == null ||
                Engine.Profile.Current == null ||
                !Engine.Profile.Current.BandageGump ||
                World.Player == null ||
                World.Player.IsDestroyed)
                return false;

            Width = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
            Height = _borderSize * 2 + _iconSize;

            _background.Width = Width;
            _background.Height = Height;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            int gx = Engine.Profile.Current.GameWindowPosition.X;
            int gy = Engine.Profile.Current.GameWindowPosition.Y;
            int w = Engine.Profile.Current.GameWindowSize.X;
            int h = Engine.Profile.Current.GameWindowSize.Y;

            x = gx + World.Player.RealScreenPosition.X;
            y = gy + World.Player.RealScreenPosition.Y;

            x += (int) World.Player.Offset.X + 22;
            y += (int) (World.Player.Offset.Y - World.Player.Offset.Z) + 22;

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

            if (_updateTime < totalMS)
            {
                _updateTime = (float) totalMS + 125;
                IsVisible = false;
                Time = 0;

                switch (Engine.GlobalSettings.ShardType)
                {
                    case 2: // outlands
                        if (World.Player.EnergyResistance > 0)
                        {
                            IsVisible = true;
                            Time = World.Player.EnergyResistance;
                        }
                        break;

                    default:
                        if (_useTime)
                        {
                            IsVisible = true;
                            Time = (Engine.Ticks - _startTime) / 1000;
                            if (Time > 20) // fail-safe (this can never be reached)
                            {
                                Stop();
                                IsVisible = false;
                            }
                        }
                        break;
                }

                if (IsVisible)
                    _text.Text = $"{Time}";
            }

            if (IsDisposed)
                return;

            if (World.Player == null || World.Player.IsDestroyed)
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

            _text = new Label($"{Time}", true, 0x35, 0, 1, FontStyle.BlackBorder)
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