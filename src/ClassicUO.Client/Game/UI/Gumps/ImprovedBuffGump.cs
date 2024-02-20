using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ImprovedBuffGump : Gump
    {
        public const int HEIGHT = CoolDownBar.COOL_DOWN_HEIGHT * (BuffBarManager.MAX_COOLDOWN_BARS + 2);
        private GumpPic _background;
        private Button _button;
        private bool _direction = false;
        private ushort _graphic = 2091;
        private DataBox _box;

        public ImprovedBuffGump() : base(0, 0)
        {
            X = 100;
            Y = 100;
            Width = CoolDownBar.COOL_DOWN_WIDTH;
            Height = HEIGHT;
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = false;

            BuildGump();
        }

        public void AddBuff(BuffIcon icon)
        {
            if (icon != null)
            {
                CoolDownBar coolDownBar = new CoolDownBar(TimeSpan.FromMilliseconds(icon.Timer - Time.Ticks), icon.Title.Replace("<br>", " "), ProfileManager.CurrentProfile.ImprovedBuffBarHue, 0, 0, icon.Graphic, icon.Type, true);
                coolDownBar.SetTooltip(icon.Text);
                BuffBarManager.AddCoolDownBar(coolDownBar, _direction, _box);
                _box.Add(coolDownBar);
            }
        }

        public void RemoveBuff(BuffIconType graphic)
        {
            BuffBarManager.RemoveBuffType(graphic);
            BuffBarManager.UpdatePositions(_direction, _box);
        }

        private void SwitchDirections()
        {
            _box.Height = HEIGHT;
            _box.Y = 0;
            if (!_direction)
            {
                _background.Y = HEIGHT - 11;
                Y -= HEIGHT - 11;
            }
            else
            {
                _background.Y = 0;
                Y += HEIGHT - 11;
            }
            _button.Y = _background.Y - 5;
            BuffBarManager.UpdatePositions(_direction, _box);
        }

        protected override void UpdateContents()
        {
            base.UpdateContents();
            _box.Height = HEIGHT;
            _box.Y = 0;
            if (!_direction)
            {
                _background.Y = HEIGHT - 11;
            }
            else
            {
                _background.Y = 0;
            }
            _button.Y = _background.Y - 5;
            BuffBarManager.UpdatePositions(_direction, _box);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _direction = !_direction;
                SwitchDirections();
            }
        }

        private void BuildGump()
        {
            _background = new GumpPic(0, 0, _graphic, 0);
            _background.Width = CoolDownBar.COOL_DOWN_WIDTH;

            _button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                ButtonAction = ButtonAction.Activate
            };

            _box = new DataBox(0, 0, Width, HEIGHT);



            Add(_background);
            Add(_button);
            Add(_box);

            BuffBarManager.Clear();
            if (World.Player != null)
            {
                foreach (KeyValuePair<BuffIconType, BuffIcon> k in World.Player.BuffIcons)
                {
                    AddBuff(k.Value);
                }
            }
            UpdateContents();
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
            writer.WriteAttributeString("updown", _direction.ToString());
            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _graphic = ushort.Parse(xml.GetAttribute("graphic"));
            _direction = bool.Parse(xml.GetAttribute("updown"));
            int.TryParse(xml.GetAttribute("lastX"), out X);
            int.TryParse(xml.GetAttribute("lastY"), out Y);
            RequestUpdateContents();
        }

        public override GumpType GumpType => GumpType.Buff;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        private static class BuffBarManager
        {
            public const int MAX_COOLDOWN_BARS = 20;
            private static CoolDownBar[] coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];
            public static void AddCoolDownBar(CoolDownBar coolDownBar, bool topDown, DataBox _boxContainer)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed && coolDownBars[i].buffIconType == coolDownBar.buffIconType)
                    {
                        coolDownBars[i].Dispose();
                        coolDownBars[i] = coolDownBar;
                        UpdatePositions(topDown, _boxContainer);
                        return;
                    }
                    if (coolDownBars[i] == null || coolDownBars[i].IsDisposed)
                    {
                        coolDownBars[i] = coolDownBar;
                        UpdatePositions(topDown, _boxContainer);
                        return;
                    }
                }
            }

            public static void UpdatePositions(bool topDown, DataBox _boxContainer)
            {
                int actualI = 0;
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed)
                    {
                        if (topDown)
                        {
                            coolDownBars[i].Y = (actualI * (CoolDownBar.COOL_DOWN_HEIGHT + 2)) + 13;
                        }
                        else
                        {
                            coolDownBars[i].Y = _boxContainer.Height - ((actualI + 1) * (CoolDownBar.COOL_DOWN_HEIGHT + 2)) - 11;
                        }
                        actualI++;
                    }
                }
            }

            public static void RemoveBuffType(BuffIconType type)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed)
                    {
                        if (coolDownBars[i].buffIconType == type)
                        {
                            coolDownBars[i].Dispose();
                        }
                    }
                }
            }

            public static void Clear()
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null)
                    {
                        coolDownBars[i].Dispose();
                    }
                }
                coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];
            }
        }
    }
}
