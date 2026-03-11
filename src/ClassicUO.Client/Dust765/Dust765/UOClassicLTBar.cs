#region license

// Copyright (C) 2020 project dust765
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

using ClassicUO.Input;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Assets;

using ClassicUO.Renderer;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.Dust765
{
    internal class UCCLTBar : Control
    {
        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color) : base
            (
                x,
                y,
                w,
                h,
                color
            )
            {
                LineWidth = w;

                LineColor = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });

                CanMove = true;
            }

            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                batcher.Draw
                (
                    LineColor,
                    new Rectangle
                    (
                        x,
                        y,
                        LineWidth,
                        Height
                    ),
                    hueVector
                );

                return true;
            }
        }

        private LineCHB _line;

        public int LineWidth
        {
            get => _line.LineWidth;
            set => _line.LineWidth = value;
        }

        public Texture2D LineColor
        {
            get => _line.LineColor;
            set => _line.LineColor = value;
        }

        public UCCLTBar(int ix, int iy, /*string itext, ushort ihue,*/ int lw, int lh, uint lcolor/*, string text, ushort tcolor*/)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            if (ProfileManager.CurrentProfile.UOClassicCombatLTBar_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            _line = new LineCHB(ix, iy, lw, lh, lcolor)
            {

            };
            _line.Width = LineWidth;

            Add(_line);
        }

        public override void Update()
        {
            base.Update();
        }
    }
    internal class UOClassicCombatLTBar : Gump
    {
        //MAIN UI CONSTRUCT
        private readonly AlphaBlendControl _background;

        //HEADER ICON
        private GumpPic _headerIcon;

        //LTNAME
        protected StbTextBox _textBoxLT;

        //LTSERIAL
        private uint LTSerial = 0;
        // serial for which we already sent a name-request so we don't spam
        private uint _nameRequestedForSerial = 0;
        //private Entity LTEntity = null;

        //UCCLTBarS
        private UCCLTBar _hpLineRedLT, _manaLineRedLT, _stamLineRedLT, _outlineLT;
        private readonly UCCLTBar[] _barsLT = new UCCLTBar[3];
        private readonly UCCLTBar[] _borderLT = new UCCLTBar[4];

        //CONSTANTS
        private static Color LINE_COLOR_RED = Color.Red;
        private static Color LINE_COLOR_BLUE = Color.DodgerBlue;
        private static Color LINE_COLOR_BLACK = Color.Black;

        private static readonly Texture2D LINE_COLOR_DRAW_BLUE = SolidColorTextureCache.GetTexture(Color.DodgerBlue);
        //private static readonly Texture2D HPB_COLOR_GRAY = SolidColorTextureCache.GetTexture(Color.Gray);
        private static readonly Texture2D LINE_COLOR_DRAW_RED = SolidColorTextureCache.GetTexture(Color.Red);
        private static readonly Texture2D LINE_COLOR_DRAW_YELLOW = SolidColorTextureCache.GetTexture(Color.Orange);
        private static readonly Texture2D LINE_COLOR_DRAW_POISON = SolidColorTextureCache.GetTexture(Color.LimeGreen);
        private static readonly Texture2D LINE_COLOR_DRAW_BLACK = SolidColorTextureCache.GetTexture(Color.Black);
        private static readonly Texture2D LINE_COLOR_DRAW_PARA = SolidColorTextureCache.GetTexture(Color.MediumPurple);

        private const int HPB_BORDERSIZE = 1;
        private const int HPB_OUTLINESIZE = 1;
        internal const int HPB_WIDTH = 120;
        internal const int HPB_BAR_WIDTH = 100;
        internal const int HPB_HEIGHT_SINGLELINE = 36;
        internal const int HPB_BAR_HEIGHT = 8;
        private const int HPB_BAR_SPACELEFT = (HPB_WIDTH - HPB_BAR_WIDTH) / 2;
        //MAIN UI CONSTRUCT

        public UOClassicCombatLTBar() : base(0, 0)
        {
            UOClassicCombatLTBar existing;

            while ((existing = UIManager.GetGump<UOClassicCombatLTBar>()) != null)
            {
                existing.Dispose();
            }

            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            if (ProfileManager.CurrentProfile.UOClassicCombatLTBar_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            //MAIN CONSTRUCT
            Width = HPB_WIDTH;
            Height = HPB_HEIGHT_SINGLELINE;

            Add(_background = new AlphaBlendControl()
            {
                Alpha = 0.6f,
                Width = Width,
                Height = Height
            });

            Add(_background);

            //UCCLTBarS
            Add(_borderLT[0] = new UCCLTBar(0, 0, HPB_WIDTH, HPB_BORDERSIZE, LINE_COLOR_BLACK.PackedValue));
            Add(_borderLT[1] = new UCCLTBar(0, HPB_HEIGHT_SINGLELINE - HPB_BORDERSIZE, HPB_WIDTH, HPB_BORDERSIZE, LINE_COLOR_BLACK.PackedValue));
            Add(_borderLT[2] = new UCCLTBar(0, 0, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, LINE_COLOR_BLACK.PackedValue));
            Add(_borderLT[3] = new UCCLTBar(HPB_WIDTH - HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, LINE_COLOR_BLACK.PackedValue));

            Add(_outlineLT = new UCCLTBar(HPB_BAR_SPACELEFT - HPB_OUTLINESIZE, 21 - HPB_OUTLINESIZE, HPB_BAR_WIDTH + HPB_OUTLINESIZE * 2, HPB_BAR_HEIGHT + HPB_OUTLINESIZE * 2, LINE_COLOR_BLACK.PackedValue));
            Add(_hpLineRedLT = new UCCLTBar(HPB_BAR_SPACELEFT, 21, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_RED.PackedValue));
            //Add(_manaLineRedLT = new UCCLTBar(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_RED.PackedValue));
            //Add(_stamLineRedLT = new UCCLTBar(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_RED.PackedValue));

            Add(_barsLT[0] = new UCCLTBar(HPB_BAR_SPACELEFT, 21, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_BLUE.PackedValue));
            //Add(_barsLT[1] = new UCCLTBar(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_BLUE.PackedValue));
            //Add(_barsLT[2] = new UCCLTBar(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, LINE_COLOR_BLUE.PackedValue));

            //HEADER ICON
            Add
            (
                _headerIcon = new GumpPic(16, -16, 0x757D, 0) //ALTERNATIVE 0x756D
                {
                    AcceptMouseInput = false,
                }
            );
            Add(_headerIcon);

            //NAME
            Add
            (
                _textBoxLT = new StbTextBox
                (
                    1, 32, HPB_WIDTH, true, FontStyle.Cropped | FontStyle.BlackBorder, 0x40, TEXT_ALIGN_TYPE.TS_CENTER
                )
                {
                    X = 0,
                    Y = 3,
                    Width = HPB_BAR_WIDTH,
                    IsEditable = false,
                    CanMove = true,
                    
                    AcceptKeyboardInput = false,
                    AcceptMouseInput = false
                }
            );

            //COPY PASTED
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;
        }
        //MAIN
        public override void Update()
        {
            // if the feature was disabled while the gump is still open, self-dispose and stop
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UOClassicCombatLTBar)
            {
                Dispose();
                return;
            }

            if (World.Player == null || World.Player.IsDestroyed /*|| World.Player.IsDead*/)
            {
                base.Update();
                return;
            }

            Entity entity = World.Get(TargetManager.LastTargetInfo.Serial);

            if (entity != null)
            {

                LTSerial = TargetManager.LastTargetInfo.Serial;

                // prefer an externally-supplied name (scripts/plugins)…
                string name = !string.IsNullOrEmpty(TargetManager.LastTargetInfo.Name)
                    ? TargetManager.LastTargetInfo.Name
                    : entity.Name;

                // if the name is still empty (0x2D arrived but 0x11 with name didn't yet),
                // use Send_StatusRequest (0x34) directly — no side effects, no popup menus
                // only once per serial to avoid spam; guard against zero/invalid serial
                if (string.IsNullOrEmpty(name) && LTSerial != 0 && SerialHelper.IsMobile(LTSerial))
                {
                    if (_nameRequestedForSerial != LTSerial)
                    {
                        _nameRequestedForSerial = LTSerial;
                        NetClient.Socket.Send_StatusRequest(LTSerial);
                    }
                }

                _textBoxLT.Text = name;
                _textBoxLT.Hue = Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray);

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, HPB_BAR_WIDTH);

                Mobile mobile = entity as Mobile;

                if (mobile == null)
                {
                    base.Update();
                    return;
                }

                if (mobile.IsDead || mobile.IsDestroyed || mobile.Hits == 0)
                    hits = 0;

                //int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, HPB_BAR_WIDTH);
                //int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, HPB_BAR_WIDTH);

                //_hpLineRedLT.LineWidth = HPB_BAR_WIDTH;
                //_manaLineRedLT.LineWidth = HPB_BAR_WIDTH;
                //_stamLineRedLT.LineWidth = HPB_BAR_WIDTH;

                _barsLT[0].LineWidth = hits;
                //_barsLT[1].LineWidth = mana;
                //_barsLT[2].LineWidth = stam;

                if (mobile.IsPoisoned)
                {
                    _barsLT[0].LineColor = LINE_COLOR_DRAW_POISON;
                    _borderLT[0].LineColor = LINE_COLOR_DRAW_POISON;

                    if (_borderLT.Length >= 3)
                    {
                        _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_POISON;
                    }
                }
                else if (mobile.IsParalyzed)
                {
                    _barsLT[0].LineColor = LINE_COLOR_DRAW_PARA;
                    _borderLT[0].LineColor = LINE_COLOR_DRAW_PARA;

                    if (_borderLT.Length >= 3)
                    {
                        _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_PARA;
                    }
                }
                else if (mobile.IsYellowHits)
                {
                    _barsLT[0].LineColor = LINE_COLOR_DRAW_YELLOW;
                    _borderLT[0].LineColor = LINE_COLOR_DRAW_YELLOW;

                    if (_borderLT.Length >= 3)
                    {
                        _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_YELLOW;
                    }
                }
                else
                {
                    _barsLT[0].LineColor = LINE_COLOR_DRAW_BLUE;

                    if (CanMove)
                    {
                        _borderLT[0].LineColor = LINE_COLOR_DRAW_BLACK;

                        if (_borderLT.Length >= 3)
                        {
                            _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_BLACK;
                        }
                    }
                    else
                    {
                        _borderLT[0].LineColor = LINE_COLOR_DRAW_RED;

                        if (_borderLT.Length >= 3)
                        {
                            _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_RED;
                        }
                    }
                }
            }
            else
            {
                // no world entity – if a name was provided externally, show it
                _textBoxLT.Text = TargetManager.LastTargetInfo.Name ?? string.Empty;
                _textBoxLT.Hue = Notoriety.GetHue(NotorietyFlag.Gray);

                // reset so next time the entity appears we request the name again if needed
                _nameRequestedForSerial = 0;

                _barsLT[0].LineWidth = 0;

                _barsLT[0].LineColor = LINE_COLOR_DRAW_BLUE;

                if (CanMove)
                {
                    _borderLT[0].LineColor = LINE_COLOR_DRAW_BLACK;

                    if (_borderLT.Length >= 3)
                    {
                        _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_BLACK;
                    }
                }
                else
                {
                    _borderLT[0].LineColor = LINE_COLOR_DRAW_RED;

                    if (_borderLT.Length >= 3)
                    {
                        _borderLT[1].LineColor = _borderLT[2].LineColor = _borderLT[3].LineColor = LINE_COLOR_DRAW_RED;
                    }
                }
            }
            base.Update();
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatLTBarLocation = Location;
        }
        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UOClassicCombatLTBar)
            {
                return;
            }

            if (button != MouseButtonType.Left)
            {
                return;
            }

            if (TargetManager.IsTargeting)
            {
                
                TargetManager.Target(LTSerial);

                //already last target
                /*
                Entity ent = World.Get(LTSerial);

                if (ent == null)
                {
                    TargetManager.LastTargetInfo.Serial = LTSerial;
                    GameActions.Print($"Changing last target to {LTEntity.Name}");
                    GameActions.Print(World.Player, $"Target: {LTEntity.Name}");
                    TargetManager.CancelTarget();
                }
                */
                Mouse.LastLeftButtonClickTime = 0;
            }

            base.OnMouseDown(x, y, button);
        }
        protected override void OnMouseOver(int x, int y)
        {
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UOClassicCombatLTBar)
            {
                base.OnMouseOver(x, y);
                return;
            }

            Entity entity = World.Get(LTSerial);

            if (entity != null)
            {
                SelectedObject.HealthbarObject = entity;
                SelectedObject.Object = entity;
            }

            base.OnMouseOver(x, y);
        }
        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UOClassicCombatLTBar)
            {
                return false;
            }

            if (button != MouseButtonType.Left)
            {
                return false;
            }

            if (TargetManager.IsTargeting)
            {
                return false;
            }

            if (CanMove)
            {
                ProfileManager.CurrentProfile.UOClassicCombatLTBar_Locked = false;
                CanMove = false;
                AcceptMouseInput = false;
            }
            else
            {
                ProfileManager.CurrentProfile.UOClassicCombatLTBar_Locked = true;
                CanMove = true;
                AcceptMouseInput = false;
            }

            return true;
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        protected static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                {
                    max = 100;
                }

                if (max > 1)
                {
                    max = maxValue * max / 100;
                }
            }

            return max;
        }
    }
}