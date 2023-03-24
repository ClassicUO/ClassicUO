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
        private GumpPic _background;
        private Button _button;
        private bool _direction = true;
        private ushort _graphic = 2091;
        private DataBox _box;

        public ImprovedBuffGump() : base(0, 0)
        {
            X = 100;
            Y = 100;
            Width = CoolDownBar.COOL_DOWN_WIDTH;
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            BuildGump();

            if (World.Player != null)
            {
                foreach (KeyValuePair<BuffIconType, BuffIcon> k in World.Player.BuffIcons)
                {
                    AddBuff(k.Value);
                }
            }
        }

        public void AddBuff(BuffIcon icon)
        {
            CoolDownBar coolDownBar = new CoolDownBar(TimeSpan.FromMilliseconds(icon.Timer - Time.Ticks), icon.Title.Replace("<br>", " "), ProfileManager.CurrentProfile.ImprovedBuffBarHue, 0, 0, icon.Graphic, icon.Type);

            BuffBarManager.AddCoolDownBar(coolDownBar, _direction);
            _box.Add(coolDownBar);
        }

        public void RemoveBuff(BuffIconType graphic)
        {
            BuffBarManager.RemoveBuffType(graphic);
        }

        protected override void UpdateContents()
        {
            base.UpdateContents();

            BuffBarManager.UpdatePositions(_direction);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _direction = !_direction;
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
            _background.Width = CoolDownBar.COOL_DOWN_WIDTH;

            Add
            (
                _button = new Button(0, 0x7585, 0x7589, 0x7589)
                {
                    ButtonAction = ButtonAction.Activate
                }
            );


            _button.X = -5;
            _button.Y = -5;


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
            writer.WriteAttributeString("updown", _direction.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _graphic = ushort.Parse(xml.GetAttribute("graphic"));
            _direction = bool.Parse(xml.GetAttribute("updown"));
            RequestUpdateContents();
        }

        public override GumpType GumpType => GumpType.Buff;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        private static class BuffBarManager
        {
            private const int MAX_COOLDOWN_BARS = 20;
            private static CoolDownBar[] coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];
            public static void AddCoolDownBar(CoolDownBar coolDownBar, bool bottomUp)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] == null || coolDownBars[i].IsDisposed)
                    {
                        if (bottomUp)
                            coolDownBar.Y = (i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)) + 15;
                        else
                            coolDownBar.Y = -(i * (CoolDownBar.COOL_DOWN_HEIGHT + 5) + 35);

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
                        if (bottomUp)
                        {
                            coolDownBars[i].Y = (i * (CoolDownBar.COOL_DOWN_HEIGHT + 2)) + 15;
                        }
                        else
                        {
                            coolDownBars[i].Y = -(i * (CoolDownBar.COOL_DOWN_HEIGHT + 2) + 35);
                        }
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
                            coolDownBars[i].Dispose();
                    }
                }
            }
        }
    }
}
