using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Dust765.Managers;
using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Dust765.External
{
    class OnCastingGump : Gump
    {
        const byte _iconSize = 16, _spaceSize = 3, _borderSize = 3;
        public uint Timer { get; set; }
        private uint _startTime;
        private uint _endTime;
        //private float _updateTime;
        private AlphaBlendControl _background;
        private Label _text;
        //private TextureControl _icon;
        private StaticPic _icon;
        public SpellAction _spell;

        public OnCastingGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            GameActions.iscasting = false;
            IsVisible = false;
            BuildGump();
        }

        private static int[] _stopAtClilocs = new int[]
        {
            // procurar clilocs de disturb de magia
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
    };


        public void Start(uint _spell_id, uint _re = 0)
        {
            _startTime = Time.Ticks;
            uint circle;

            if (!ProfileManager.CurrentProfile.OnCastingGump_hidden)
            {
                IsVisible = true;
            }

            try
            {
                _spell = (SpellAction)_spell_id;
                circle = (uint)SpellManager.GetCircle(_spell);
                uint protection_delay = 0;
                bool ignore_proctetion_delay = (_spell == SpellAction.Protection || _spell == SpellAction.ArchProtection);
                if (World.Player.IsBuffIconExists(BuffIconType.Protection) && !ignore_proctetion_delay
                    || World.Player.IsBuffIconExists(BuffIconType.EssenceOfWind))
                {
                    protection_delay = 2;
                }
                _endTime = _startTime + 400 + (circle + protection_delay) * 250 + _re; // (0.5+ 0.25 * circle) * 1000
                GameActions.iscasting = true;
            }
            catch
            {

                Stop();

            }
        }


        public void Stop()
        {
            GameActions.iscasting = false;
            IsVisible = false;   
        }
        /*
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
        }
        */
        public void OnCliloc(uint cliloc)
        {
            // stop the timer
            if (!GameActions.iscasting)
            {
                return;
            }
                
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible ||
                ProfileManager.CurrentProfile == null ||
                !ProfileManager.CurrentProfile.OnCastingGump ||
                World.Player == null ||
                World.Player.IsDestroyed)
                return false;

            Width = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
            Height = _borderSize * 2 + _iconSize;

            _background.Width = Width;
            _background.Height = Height;

            /*
            int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
            int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;
            
            x = gx + World.Player.RealScreenPosition.X;
            y = gy + World.Player.RealScreenPosition.Y;

            x += (int) World.Player.Offset.X;// + 22; -OFFSET FIX
            y += (int) (World.Player.Offset.Y - World.Player.Offset.Z);// + 22; -OFFSET FIX

            x -= Width >> 1;
            x += 5;
            y += 10;

            //x += ProfileManager.CurrentProfile.BandageGumpOffset.X;
            //y += ProfileManager.CurrentProfile.BandageGumpOffset.Y;

            Y = y;
            X = x;
            */

            return base.Draw(batcher, Mouse.Position.X, Mouse.Position.Y);
        }

        //public override void Update(double totalMS, double frameMS)
        public override void Update()
        {
            //base.Update(totalMS, frameMS);
            base.Update();

            if (IsDisposed)
                return;

            if (World.Player == null || World.Player.IsDestroyed)
            {
                Dispose();
                return;
            }


            //if (GameActions.iscasting && _updateTime < totalMS)
            if (GameActions.iscasting)
            {
                //_updateTime = (float) totalMS + 125;
                if (Time.Ticks >= _endTime)
                {
                    Stop();
                }
            }
            else
            {
                if (!GameActions.iscasting && IsVisible)
                {
                    Stop();
                }
            }
        }

        private void BuildGump()
        {
            _background = new AlphaBlendControl()
            {
                Alpha = 0.6f
            };

            _text = new Label($"Casting", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize + _iconSize + _spaceSize + 3,
                Y = _borderSize - 2
            };

            /*
            _icon = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _icon.Texture = ArtLoader.Instance.GetTexture(0x0E21);
            _icon.Hue = 0;
            _icon.X = _borderSize;
            _icon.Y = _borderSize; // slight offset due to imgs offset
            _icon.Width = _iconSize;
            _icon.Height = _iconSize;
            */

            /*
            Add
            (
                _icon = new StaticPic(0x0E21, 0)
                {
                    X = _borderSize - _iconSize, Y = _borderSize - 1,
                    AcceptMouseInput = false
                }
            );
            */

            Add(_background);
            Add(_text);
            //Add(_icon);
        }
    }
}