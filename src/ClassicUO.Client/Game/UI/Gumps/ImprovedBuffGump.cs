using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using System;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ImprovedBuffGump : Gump
    {
        private GumpPic _background;
        private Button _button;
        private GumpDirection _direction;
        private ushort _graphic;
        private DataBox _box;

        public ImprovedBuffGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;


            _direction = GumpDirection.LEFT_HORIZONTAL;
            _graphic = 0x7580;

            BuildGump();
        }

        public void AddBuff(BuffIcon icon)
        {
            CoolDownBar coolDownBar = new CoolDownBar(TimeSpan.FromMilliseconds(icon.Timer - Time.Ticks), icon.Text, 905, 0, 0, icon.Graphic);
            int x = 0;
            bool upsideDown = false;
            switch (_direction)
            {
                case GumpDirection.LEFT_HORIZONTAL:
                    upsideDown = true;
                    x = 25;
                    break;

                case GumpDirection.LEFT_VERTICAL:
                    x = 25;
                    break;

            }

            BuffBarManager.AddCoolDownBar(coolDownBar, x, upsideDown);
            _box.Add(coolDownBar);
        }

        protected override void UpdateContents()
        {
            base.UpdateContents();
            _background.Graphic = _graphic;
            switch (_direction)
            {
                case GumpDirection.LEFT_HORIZONTAL:
                    _button.X = -2;
                    _button.Y = 36;

                    break;

                case GumpDirection.LEFT_VERTICAL:
                default:
                    _button.X = 0;
                    _button.Y = 0;

                    break;
            }
            BuffBarManager.UpdatePositions(_direction == GumpDirection.LEFT_HORIZONTAL);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                switch (_graphic)
                {
                    case 0x7580:
                        _direction = GumpDirection.LEFT_VERTICAL;
                        _graphic = 0x757F;
                        break;

                    case 0x757F:
                    default:
                        _direction = GumpDirection.LEFT_HORIZONTAL;
                        _graphic = 0x7580;
                        break;
                }
                RequestUpdateContents();
            }
        }

        private void BuildGump()
        {
            Add
            (
                _background = new GumpPic(0, 0, _graphic, 0)
                {
                    LocalSerial = 1
                }
            );
            _background.Graphic = _graphic;
            _background.X = 0;
            _background.Y = 0;

            Add
            (
                _button = new Button(0, 0x7585, 0x7589, 0x7589)
                {
                    ButtonAction = ButtonAction.Activate
                }
            );


            switch (_direction)
            {
                case GumpDirection.LEFT_HORIZONTAL:
                    _button.X = -2;
                    _button.Y = 36;

                    break;

                case GumpDirection.LEFT_VERTICAL:
                default:
                    _button.X = 0;
                    _button.Y = 0;

                    break;
            }

            Add
            (
                _box = new DataBox(0, 0, 0, 0)
                {
                    WantUpdateSize = true
                }
            );
        }

        public ImprovedBuffGump(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", _graphic.ToString());
            writer.WriteAttributeString("direction", ((int)_direction).ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _graphic = ushort.Parse(xml.GetAttribute("graphic"));
            _direction = (GumpDirection)byte.Parse(xml.GetAttribute("direction"));
            BuildGump();
        }

        public override GumpType GumpType => GumpType.Buff;

        private enum GumpDirection
        {
            LEFT_VERTICAL,
            LEFT_HORIZONTAL
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        private static class BuffBarManager
        {
            private const int MAX_COOLDOWN_BARS = 20;
            private static CoolDownBar[] coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];
            public static void AddCoolDownBar(CoolDownBar coolDownBar, int x, bool bottomUp)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] == null || coolDownBars[i].IsDisposed)
                    {
                        coolDownBar.X = x;
                        if (!bottomUp)
                            coolDownBar.Y = (i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)) + 20;
                        else
                            coolDownBar.Y = -(i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)) + 5;

                        coolDownBars[i] = coolDownBar;
                        return;
                    }
                }
            }

            public static void UpdatePositions(bool bottomUp)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed)
                    {
                        if (!bottomUp)
                        {
                            coolDownBars[i].Y = (i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)) + 20;
                        }
                        else
                        {
                            coolDownBars[i].Y = -(i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)) + 5;
                        }
                    }
                }
            }
        }
    }
}
