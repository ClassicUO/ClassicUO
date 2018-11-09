using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class BuffGump : Gump
    {
        private static BuffGump _gump;
        private readonly GumpPic _background;
        private readonly Button _button;
        private GumpDirection _direction;
        private ushort _graphic;

        public BuffGump() : base(0, 0)
        {
            X = 100;
            Y = 100;
            CanMove = true;
            CanCloseWithRightClick = true;
            _graphic = 0x7580;

            AddChildren(_background = new GumpPic(0, 0, _graphic, 0)
            {
                LocalSerial = 1
            });

            AddChildren(_button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                X = -2, Y = 36, ButtonAction = ButtonAction.Activate
            });
            _direction = GumpDirection.LEFT_HORIZONTAL;

            foreach (KeyValuePair<Graphic, BuffIcon> k in World.Player.BuffIcons)
                AddChildren(new BuffControlEntry(World.Player.BuffIcons[k.Key]));
            UpdateElements();
        }

        public static void Toggle()
        {
            UIManager ui = Service.Get<UIManager>();

            if (ui.GetByLocalSerial<BuffGump>() == null)
                ui.Add(_gump = new BuffGump());
            else
                _gump.Dispose();
        }

        public void AddBuff(Graphic graphic)
        {
            AddChildren(new BuffControlEntry(World.Player.BuffIcons[graphic]));
            UpdateElements();
        }

        public void RemoveBuff(Graphic graphic)
        {
            RemoveChildren(Children.OfType<BuffControlEntry>().FirstOrDefault(s => s.Icon.Graphic == graphic));
            UpdateElements();
        }

        private void UpdateElements()
        {
            var list = FindControls<BuffControlEntry>();
            int offset = 0;

            foreach (BuffControlEntry e in list)
            {
                switch (_direction)
                {
                    case GumpDirection.LEFT_VERTICAL:
                        e.X = 26;
                        e.Y = 25 + offset;
                        offset += 31;

                        break;
                    case GumpDirection.LEFT_HORIZONTAL:
                        e.X = 26 + offset;
                        e.Y = 5;
                        offset += 31;

                        break;
                    case GumpDirection.RIGHT_VERTICAL:
                        e.X = 5;
                        e.Y = 48 + offset;
                        offset -= 31;

                        break;
                    case GumpDirection.RIGHT_HORIZONTAL:
                        e.X = 48 + offset;
                        e.Y = 5;
                        offset -= 31;

                        break;
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _graphic++;

                if (_graphic > 0x7582)
                    _graphic = 0x757F;

                switch (_graphic)
                {
                    case 0x7580:
                        _button.X = -2;
                        _button.Y = 36;
                        _direction = GumpDirection.LEFT_HORIZONTAL;

                        break;
                    case 0x7581:
                        _button.X = 34;
                        _button.Y = 78;
                        _direction = GumpDirection.RIGHT_VERTICAL;

                        break;
                    case 0x7582:
                        _button.X = 76;
                        _button.Y = 36;
                        _direction = GumpDirection.RIGHT_HORIZONTAL;

                        break;
                    case 0x757F:
                    default:
                        _button.X = 0;
                        _button.Y = 0;
                        _direction = GumpDirection.LEFT_VERTICAL;

                        break;
                }

                _background.Graphic = _graphic;
                _background.Texture = IO.Resources.Gumps.GetGumpTexture(_graphic);
                UpdateElements();
            }
        }

        private enum GumpDirection
        {
            LEFT_VERTICAL,
            LEFT_HORIZONTAL,
            RIGHT_VERTICAL,
            RIGHT_HORIZONTAL
        }

        private class BuffControlEntry : GumpPic
        {
            private byte _alpha;
            private bool _decreaseAlpha;
            private readonly uint _timer;

            public BuffControlEntry(BuffIcon icon) : base(0, 0, icon.Graphic, 0)
            {
                Icon = icon;
                Texture = IO.Resources.Gumps.GetGumpTexture(icon.Graphic);
                Width = Texture.Width;
                Height = Texture.Height;
                _alpha = 0xFF;
                _decreaseAlpha = true;
                _timer = (uint) (icon.Timer <= 0 ? 0xFFFF_FFFF : CoreGame.Ticks + icon.Timer * 1000);
            }

            public BuffIcon Icon { get; }

            public override void Update(double totalMS, double frameMS)
            {
                Texture.Ticks = (long) totalMS;
                int delta = (int) (_timer - totalMS);

                if (_timer != 0xFFFF_FFFF && delta < 10000)
                {
                    if (delta <= 0)
                        Dispose();
                    else
                    {
                        int alpha = _alpha;
                        int addVal = (10000 - delta) / 600;

                        if (_decreaseAlpha)
                        {
                            alpha -= addVal;

                            if (alpha <= 60)
                            {
                                _decreaseAlpha = false;
                                alpha = 60;
                            }
                        }
                        else
                        {
                            alpha += addVal;

                            if (alpha >= 255)
                            {
                                _decreaseAlpha = true;
                                alpha = 255;
                            }
                        }

                        _alpha = (byte) alpha;
                    }
                }

                base.Update(totalMS, frameMS);
            }

            public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
            {
                return spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(0, false, 1.0f - _alpha / 255f, false));
            }
        }
    }
}