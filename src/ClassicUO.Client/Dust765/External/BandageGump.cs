using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using System;   // EDIT: MARK

namespace ClassicUO.Dust765.External
{
    class BandageGump : Gump
    {
        const byte _iconSize = 16, _spaceSize = 3, _borderSize = 3;
        public uint Timer { get; set; }
        private bool _useTime = false;
        private uint _startTime;
        private uint _initialTimer;
        private static bool _upDownToggle { get => ProfileManager.CurrentProfile.BandageGumpUpDownToggle; }
        //private float _updateTime;
        private AlphaBlendControl _background;
        private Label _text;
        private StaticPic _icon;

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
            // EDIT: MARK
            _useTime = false;
            IsVisible = false;
            // EDIT-END: MARK

            BuildGump();
        }

        public void Start()
        {
            _useTime = true;
            _startTime = Time.Ticks;
            IsVisible = true;
            // EDIT: MARK
            if (World.Player.Dexterity >= 80)
            {
                //FIX IS DEX IS TOO HIGH
                ushort _useDex = World.Player.Dexterity;
                if (_useDex >= 181)
                {
                    _useDex = 180;
                }
                //FIX IS DEX IS TOO HIGH

                _initialTimer = Convert.ToUInt32(8 - Math.Floor((_useDex - 80) * 1.0) / 20) - 1;
            }
            else
            {
                _initialTimer = 8;
            }
            // EDIT-END: MARK
        }

        public void Stop()
        {
            _useTime = false;
            IsVisible = false;
            Timer = 0;
        }

        public void OnMessage(string text, uint hue, string name, bool isunicode = true)
        {
            // attempt to pick up on things that OnCliloc missed
            if (name != "System" && text.Length <= 0)
                return;

            // stop the timer
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_stopAtClilocs[i]) == text)
                {
                    Stop();
                    return;
                }
            }

            // start the timer
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_stopAtClilocs[i]) == text)
                {
                    Start();
                    return;
                }
            }
        }

        public void OnCliloc(uint cliloc)
        {
            // stop the timer
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }

            // start the timer
            for (int i = 0; i < _startAtClilocs.Length; i++)
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
                ProfileManager.CurrentProfile == null ||
                !ProfileManager.CurrentProfile.BandageGump ||
                World.Player == null ||
                World.Player.IsDestroyed)
                return false;

            Width = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
            Height = _borderSize * 2 + _iconSize;

            _background.Width = Width;
            _background.Height = Height;

            int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
            int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;
            
            x = gx + World.Player.RealScreenPosition.X;
            y = gy + World.Player.RealScreenPosition.Y;

            x += (int) World.Player.Offset.X;// + 22; -OFFSET FIX
            y += (int) (World.Player.Offset.Y - World.Player.Offset.Z);// + 22; -OFFSET FIX

            x -= Width >> 1;
            x += 5;
            y += 10;

            x += ProfileManager.CurrentProfile.BandageGumpOffset.X;
            y += ProfileManager.CurrentProfile.BandageGumpOffset.Y;

            Y = y;
            X = x;

            return base.Draw(batcher, x, y);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
                return;

            if (World.Player == null || World.Player.IsDestroyed)
            {
                Dispose();
                return;
            }

            //if (_startTime < _updateTime)
            //{
                //_updateTime = (float) _updateTime + 125;
                //IsVisible = false;
                //Timer = 0;

                // ## BEGIN - END ## // OUTLANDS
                /*
                switch (Settings.GlobalSettings.ShardType)
                {
                    case 2: // outlands
                        if (World.Player.EnergyResistance > 0)
                        {
                            IsVisible = true;
                            Timer = (uint) World.Player.EnergyResistance;
                        }
                        break;

                    default:
                        if (_useTime)
                        {
                            IsVisible = true;
                            Timer = (Time.Ticks - _startTime) / 1000;
                            if (Timer > 20) // fail-safe (this can never be reached)
                            {
                                Stop();
                                IsVisible = false;
                            }
                        }
                        break;
                }
                */
                // ## BEGIN - END ## // OUTLANDS

                if (_useTime)
                {
                    if (_upDownToggle)
                    {
                        //COUNT UP
                        IsVisible = true;
                        Timer = (Time.Ticks - _startTime) / 1000;
                        if (Timer > 10) // fail-safe (this can never be reached)
                        {
                            Stop();
                        }
                    }
                    else if (!_upDownToggle)
                    {
                        //COUNT DOWN
                        IsVisible = true;

                        // EDIT: MARK
                        uint _delta = (Time.Ticks - _startTime) / 1000;
                        Timer = _initialTimer - _delta;

                        /*
                        if ((Time.Ticks - _startTime) / 1000 > 0.750)
                        {
                            _startTime = Time.Ticks;
                            Timer = Timer - 1;
                        }

                        if (Timer > 20 || Timer <= 0) // fail-safe (this can never be reached)
                        {
                            Stop();
                            IsVisible = false;
                        }  
                        */

                        if (Timer > 10 || Timer <= 0 || _delta > 10) // fail-safe (this can never be reached)
                        {
                            Stop();
                        }
                        // EDIT-END: MARK
                    }
                }

                if (IsVisible)
                {
                    _text.Text = $"{Timer}";
                }
            //}
        }

        private void BuildGump()
        {
            _background = new AlphaBlendControl()
            {
                Alpha = 0.6f
            };

            _text = new Label($"{Timer}", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize + _iconSize + _spaceSize + 3,
                Y = _borderSize - 2
            };

            Add
            (
                _icon = new StaticPic(0x0E21, 0)
                {
                    X = _borderSize - _iconSize, Y = _borderSize - 1,
                    AcceptMouseInput = false
                }
            );

            Add(_background);
            Add(_text);
            Add(_icon);
        }
    }
}