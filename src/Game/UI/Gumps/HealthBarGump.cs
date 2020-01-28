#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
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
using System.IO;
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    abstract class BaseHealthBarGump : AnchorableGump
    {
        private bool _targetBroke;
        protected bool _isDead;
        protected string _name;
        protected bool _canChangeName;
        protected TextBox _textBox;

        protected BaseHealthBarGump(Entity entity) : this(0, 0)
        {
            if (entity == null || entity.IsDestroyed)
            {
                Dispose();
                return;
            }

            LocalSerial = entity.Serial;
            CanCloseWithRightClick = true;
            _name = entity.Name;
            _isDead = entity is Mobile mm && mm.IsDead;

            BuildGump();
        }

        protected BaseHealthBarGump(uint serial) : this(World.Get(serial))
        {

        }

        protected BaseHealthBarGump(uint local, uint server) : base(local, server)
        {
            CanMove = true;
            AnchorGroupName = "healthbar";
        }

        public override int GroupMatrixWidth
        {
            get => Width;
            protected set { }
        }

        public override int GroupMatrixHeight
        {
            get => Height;
            protected set { }
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_HEALTHBAR;

        protected abstract void BuildGump();

        public abstract void Update();


        public override void Dispose()
        {
            var entity = World.Get(LocalSerial);

            if (Client.Version >= ClientVersion.CV_200 && World.InGame && entity != null)
                NetClient.Socket.Send(new PCloseStatusBarGump(entity));

            if (SelectedObject.HealthbarObject == entity && entity != null)
                SelectedObject.HealthbarObject = null;
            base.Dispose();
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(LocalSerial);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            LocalSerial = reader.ReadUInt32();

            if (LocalSerial == World.Player)
            {
                _name = World.Player.Name;
                BuildGump();
            }
            else
                Dispose();
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
        }


        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (LocalSerial == World.Player)
            {
                _name = World.Player.Name;
                BuildGump();
            }
            else 
                Dispose();
        }

        protected void TextBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
                return;

            Point p = Mouse.LDroppedOffset;

            if (Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) >= 1)
                return;

            if (TargetManager.IsTargeting)
            {
                TargetManager.Target(LocalSerial);
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName && !_targetBroke)
            {
                _textBox.IsEditable = true;
                _textBox.SetKeyboardFocus();
            }

            _targetBroke = false;
        }

        protected static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                    max = 100;

                if (max > 1)
                    max = maxValue * max / 100;
            }

            return max;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return;

            if (TargetManager.IsTargeting)
            {
                _targetBroke = true;
                TargetManager.Target(LocalSerial);
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName)
            {
                _textBox.IsEditable = false;
                UIManager.SystemChat.SetFocus();
            }

            base.OnMouseDown(x, y, button);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return false;

            var entity = World.Get(LocalSerial);

            if (entity != null)
            {
                if (entity != World.Player)
                {
                    if (World.Player.InWarMode)
                        GameActions.Attack(entity);
                    else if (!GameActions.OpenCorpse(entity))
                        GameActions.DoubleClick(entity);
                }
                else
                {
                    StatusGumpBase.AddStatusGump(ScreenCoordinateX, ScreenCoordinateY);
                    Dispose();
                }
            }

            return true;
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            var entity = World.Get(LocalSerial);

            if (entity == null || SerialHelper.IsItem(entity.Serial))
                return;

            if ((key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) && _textBox.IsEditable)
            {
                GameActions.Rename(entity, _textBox.Text);
                UIManager.SystemChat?.SetFocus();
                _textBox.IsEditable = false;
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            var entity = World.Get(LocalSerial);

            if (entity != null)
            {
                SelectedObject.HealthbarObject = entity;
                SelectedObject.Object = entity;
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            var entity = World.Get(LocalSerial);

            if (entity != null && SelectedObject.HealthbarObject == entity)
            {
                SelectedObject.HealthbarObject = null;
                SelectedObject.Object = null;
            }
        }

        protected bool CheckIfAnchoredElseDispose()
        {
            if (UIManager.AnchorManager[this] == null && (this.LocalSerial != World.Player))
            {
                Dispose();

                return true;
            }

            return false;
        }
        
    }

    internal class HealthBarGumpCustom : BaseHealthBarGump
    {
        #region Health Bar Gump Custom

        // Health Bar Gump Custom v.1c by Syrupz(Alan)
        //
        // The goal of this was to simply modernize the Health Bar Gumps while still giving people
        // an option to continue using the classic Health Bar Gumps. The option to overide bar types
        // be it (straight line(custom) or graphic(classic) is directly included in this version
        // with no need to change art files in UO directory.
        //
        // Please report any problems with this to Alan#0084 on Discord and I will promptly work on fixing said issues.
        //
        // Lastly, I want to give a special thanks to Gaechti for helping me stress test this
        // and helping me work and organizing this in a timely fashion to get this released.
        // I would like to also thank KaRaShO, Roxya, Stalli, and Link for their input, tips, 
        // and advice to approach certain challenges that arose throughout development.
        // in different manners to get these Health Bars to function per my own vision; gratitude.
        //
        // Health Bar Gump Custom v.1c by Syrupz(Alan)

        #endregion

        private readonly LineCHB[] _bars = new LineCHB[3];
        private readonly LineCHB[] _border = new LineCHB[4];

        private LineCHB _hpLineRed, _manaLineRed, _stamLineRed, _outline;
        protected AlphaBlendControl _background;


        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;

        internal const int HPB_WIDTH = 120;
        internal const int HPB_HEIGHT_MULTILINE = 60;
        internal const int HPB_HEIGHT_SINGLELINE = 36;
        private const int HPB_BORDERSIZE = 1;
        private const int HPB_OUTLINESIZE = 1;


        internal const int HPB_BAR_WIDTH = 100;
        private const int HPB_BAR_HEIGHT = 8;
        private const int HPB_BAR_SPACELEFT = (HPB_WIDTH - HPB_BAR_WIDTH) / 2;


        private static Color HPB_COLOR_DRAW_RED = Color.Red;
        private static Color HPB_COLOR_DRAW_BLUE = Color.DodgerBlue;
        private static Color HPB_COLOR_DRAW_BLACK = Color.Black;

        private static readonly Texture2D HPB_COLOR_BLUE = Texture2DCache.GetTexture(Color.DodgerBlue);
        private static readonly Texture2D HPB_COLOR_GRAY = Texture2DCache.GetTexture(Color.Gray);
        private static readonly Texture2D HPB_COLOR_RED = Texture2DCache.GetTexture(Color.Red);
        private static readonly Texture2D HPB_COLOR_YELLOW = Texture2DCache.GetTexture(Color.Orange);
        private static readonly Texture2D HPB_COLOR_POISON = Texture2DCache.GetTexture(Color.LimeGreen);
        private static readonly Texture2D HPB_COLOR_BLACK = Texture2DCache.GetTexture(Color.Black);

        public HealthBarGumpCustom(Entity entity) : base(entity)
        {

        }

        public HealthBarGumpCustom(uint serial) : base(serial)
        {
        }

        public HealthBarGumpCustom() : base(0, 0)
        {

        }


        public override void Update()
        {
            Clear();
            Children.Clear();

            _background = null;
            _hpLineRed = _manaLineRed = _stamLineRed = null;

            _textBox = null;

            BuildGump();
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            bool inparty = World.Party.Contains(LocalSerial);


            ushort textColor = 0x0386;

            Entity entity = World.Get(LocalSerial);

            if (entity == null || entity.IsDestroyed)
            {
                if (LocalSerial != World.Player && (ProfileManager.Current.CloseHealthBarType == 1 ||
                                                    ProfileManager.Current.CloseHealthBarType == 2 && World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000)))
                {
                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                    if (!inparty && CheckIfAnchoredElseDispose())
                    {
                        return;
                    }
                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                }

                if (_isDead)
                    _isDead = false;

                if (!_outOfRange)
                {
                    _outOfRange = true;
                    textColor = 912;


                    if (inparty)
                    {
                        if (_textBox != null && _textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = false;
                            _bars[2].IsVisible = false;
                        }
                    }
                    else
                    {
                        if (_textBox != null)
                        {
                            if (_textBox.Hue != textColor)
                                _textBox.Hue = textColor;

                            if (_canChangeName)
                                _textBox.MouseUp -= TextBoxOnMouseUp;
                            _textBox.IsEditable = false;
                        }
                    }

                    if (_background.Hue != 912)
                        _background.Hue = 912;

                    if (_hpLineRed.LineColor != HPB_COLOR_GRAY)
                    {
                        _hpLineRed.LineColor = HPB_COLOR_GRAY;
                        _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_GRAY;
                    }

                    _bars[0].IsVisible = false;
                }
            }

            if (entity != null && !entity.IsDestroyed)
            {
                Mobile mobile = SerialHelper.IsMobile(entity.Serial) ? (Mobile) entity : null;

                if (!_isDead && entity != World.Player && (mobile != null && mobile.IsDead) && ProfileManager.Current.CloseHealthBarType == 2) // is dead
                {
                    if (!inparty && CheckIfAnchoredElseDispose())
                    {
                        return;
                    }
                }

                if (entity is Mobile mm && _canChangeName != mm.IsRenamable)
                {
                    _canChangeName = mm.IsRenamable;

                    if (_textBox != null)
                    {
                        _textBox.MouseUp -= TextBoxOnMouseUp;

                        if (_canChangeName)
                        {
                            _textBox.MouseUp += TextBoxOnMouseUp;
                        }
                        else
                            _textBox.IsEditable = false;
                    }
                }

                if (!(mobile != null && mobile.IsDead) && _isDead)
                    _isDead = false;

                if (!string.IsNullOrEmpty(entity.Name) && _name != entity.Name)
                {
                    _name = entity.Name;
                    if (_textBox != null)
                        _textBox.Text = _name;
                }

                if (_outOfRange)
                {
                    if (entity.HitsMax == 0)
                        GameActions.RequestMobileStatus(entity);

                    _outOfRange = false;


                    _canChangeName = mobile != null && mobile.IsRenamable;

                    if (_canChangeName)
                    {
                        textColor = 0x000E;
                        if (_textBox != null)
                            _textBox.MouseUp += TextBoxOnMouseUp;
                    }

                    if (inparty && _bars.Length >= 2 && _bars[1] != null)
                    {
                        _bars[1].IsVisible = true;
                        _bars[2].IsVisible = true;
                    }

                    if (_hpLineRed.LineColor != HPB_COLOR_RED)
                    {
                        _hpLineRed.LineColor = HPB_COLOR_RED;
                        _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_RED;
                    }

                    _bars[0].IsVisible = true;
                }



                if (mobile != null)
                {
                    textColor = Notoriety.GetHue(mobile.NotorietyFlag);
                }

                if (_textBox != null && _textBox.Hue != textColor)
                    _textBox.Hue = textColor;

                ushort barColor = mobile != null ? Notoriety.GetHue(mobile.NotorietyFlag) : (ushort) 912;

                if (_background.Hue != barColor)
                {
                    if (mobile != null && mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (!ProfileManager.Current.CBBlackBGToggled)
                    {
                        _background.Hue = barColor;
                    }
                }

                if ((mobile != null && mobile.IsDead) || ProfileManager.Current.CBBlackBGToggled)
                {
                    if (_background.Hue != 912)
                        _background.Hue = 912;
                }


                if (mobile != null)
                {
                    if (mobile.IsPoisoned && !_poisoned)
                    {
                        _bars[0].LineColor = HPB_COLOR_POISON;

                        _poisoned = true;
                        _normalHits = false;
                    }
                    else if (mobile.IsYellowHits && !_yellowHits)
                    {
                        _bars[0].LineColor = HPB_COLOR_YELLOW;

                        _yellowHits = true;
                        _normalHits = false;
                    }
                    else if (!_normalHits && (!mobile.IsPoisoned && !mobile.IsYellowHits) && (_poisoned || _yellowHits))
                    {
                        _bars[0].LineColor = HPB_COLOR_BLUE;

                        _poisoned = false;
                        _yellowHits = false;
                        _normalHits = true;
                    }
                }


                int hits = CalculatePercents(entity.HitsMax, entity.Hits, HPB_BAR_WIDTH);

                if (hits != _bars[0].LineWidth)
                {
                    _bars[0].LineWidth = hits;
                }

                if ((inparty || LocalSerial == World.Player) && mobile != null && _bars != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, HPB_BAR_WIDTH);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, HPB_BAR_WIDTH);

                    if (_bars.Length >= 2 && _bars[1] != null && mana != _bars[1].LineWidth)
                    {
                        _bars[1].LineWidth = mana;
                    }

                    if (_bars.Length >= 2 && _bars[2] != null && stam != _bars[2].LineWidth)
                    {
                        _bars[2].LineWidth = stam;
                    }
                }

                if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.RootParent == this)
                {
                    SelectedObject.HealthbarObject = entity;
                    SelectedObject.Object = entity;
                    SelectedObject.LastObject = entity;
                }
            }

            if (LocalSerial == World.Player)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    if (World.Player.InWarMode)
                    {
                        _border[0].LineColor = HPB_COLOR_RED;

                        if (_border.Length >= 3)
                        {
                            _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_RED;
                        }
                    }
                    else
                    {
                        _border[0].LineColor = HPB_COLOR_BLACK;

                        if (_border.Length >= 3)
                        {
                            _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;
                        }
                    }
                }
            }
        }

        protected override void BuildGump()
        {
            WantUpdateSize = false;

            var entity = World.Get(LocalSerial);


            if (World.Party.Contains(LocalSerial))
            {
                Height = HPB_HEIGHT_MULTILINE;
                Width = HPB_WIDTH;
                Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });


                if (LocalSerial == World.Player)
                {
                    Add(_textBox = new TextBoxCHB(1, 32, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Cropped | FontStyle.BlackBorder, hue: Notoriety.GetHue(World.Player.NotorietyFlag))
                    {
                        X = 8,
                        Y = 3,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });
                }
                else
                {
                    Add(_textBox = new TextBoxCHB(1, 32, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Cropped | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 8,
                        Y = 3,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });

                }

                Add(_outline = new LineCHB(HPB_BAR_SPACELEFT - HPB_OUTLINESIZE, 27 - HPB_OUTLINESIZE, HPB_BAR_WIDTH + (HPB_OUTLINESIZE * 2), (HPB_BAR_HEIGHT * 3) + 2 + (HPB_OUTLINESIZE * 2), HPB_COLOR_DRAW_BLACK.PackedValue));
                Add(_hpLineRed = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                Add(_manaLineRed = new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                Add(_stamLineRed = new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));

                Add(_bars[0] = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });
                Add(_bars[1] = new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });
                Add(_bars[2] = new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });

                Add(_border[0] = new LineCHB(0, 0, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                Add(_border[1] = new LineCHB(0, HPB_HEIGHT_MULTILINE - HPB_BORDERSIZE, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                Add(_border[2] = new LineCHB(0, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));
                Add(_border[3] = new LineCHB(HPB_WIDTH - HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));
            }
            else
            {
                if (LocalSerial == World.Player)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Height = HPB_HEIGHT_MULTILINE;
                    Width = HPB_WIDTH;
                    Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                    Add(_textBox = new TextBoxCHB(1, 32, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Cropped | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray), maxWidth: Width)
                    {
                        X = 8,
                        Y = 3,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });

                    Add(_outline = new LineCHB(HPB_BAR_SPACELEFT - HPB_OUTLINESIZE, 27 - HPB_OUTLINESIZE, HPB_BAR_WIDTH + (HPB_OUTLINESIZE * 2), (HPB_BAR_HEIGHT * 3) + 2 + (HPB_OUTLINESIZE * 2), HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_hpLineRed = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                    Add(new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                    Add(new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));

                    Add(_bars[0] = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });
                    Add(_bars[1] = new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });
                    Add(_bars[2] = new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });

                    Add(_border[0] = new LineCHB(0, 0, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[1] = new LineCHB(0, HPB_HEIGHT_MULTILINE - HPB_BORDERSIZE, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[2] = new LineCHB(0, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[3] = new LineCHB(HPB_WIDTH - HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));

                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = _oldWarMode ? HPB_COLOR_RED : HPB_COLOR_BLACK;
                }
                else
                {
                    Mobile mobile = entity != null && SerialHelper.IsMobile(entity.Serial) ? (Mobile) entity : null;

                    if (entity != null)
                    {
                        _canChangeName = mobile != null && mobile.IsRenamable;
                    }

                    Height = HPB_HEIGHT_SINGLELINE;
                    Width = HPB_WIDTH;
                    Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                    Add(_outline = new LineCHB(HPB_BAR_SPACELEFT - HPB_OUTLINESIZE, 21 - HPB_OUTLINESIZE, HPB_BAR_WIDTH + (HPB_OUTLINESIZE * 2), HPB_BAR_HEIGHT + (HPB_OUTLINESIZE * 2), HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_hpLineRed = new LineCHB(HPB_BAR_SPACELEFT, 21, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));

                    Add(_bars[0] = new LineCHB(HPB_BAR_SPACELEFT, 21, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });

                    Add(_border[0] = new LineCHB(0, 0, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[1] = new LineCHB(0, HPB_HEIGHT_SINGLELINE - HPB_BORDERSIZE, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[2] = new LineCHB(0, 0, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[3] = new LineCHB(HPB_WIDTH - HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, HPB_COLOR_DRAW_BLACK.PackedValue));


                    Add(_textBox = new TextBoxCHB(1, 32, width: HPB_BAR_WIDTH, isunicode: true, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray), style: FontStyle.Cropped | FontStyle.BlackBorder)
                    {
                        X = 8,
                        Y = 0,
                        Width = HPB_WIDTH,
                        Height = 15,
                        IsEditable = false,
                        AcceptMouseInput = _canChangeName,
                        AcceptKeyboardInput = _canChangeName,
                        SafeCharactersOnly = true,
                        WantUpdateSize = false,
                        CanMove = true,
                        Text = _name
                    });
                    if (_canChangeName)
                        _textBox.MouseUp += TextBoxOnMouseUp;
                }
            }


            if (entity == null)
            {
                _textBox.Hue = _background.Hue = 912;

                if (_hpLineRed.LineColor != HPB_COLOR_GRAY)
                {
                    _hpLineRed.LineColor = HPB_COLOR_GRAY;
                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                    if (_manaLineRed != null && _stamLineRed != null)
                        _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_GRAY;
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color) : base(x, y, w, h, color)
            {
                LineWidth = w;
                LineColor = Texture2DCache.GetTexture(new Color() { PackedValue = color });

                CanMove = true;
            }
            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }
            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, Alpha);

                return batcher.Draw2D(LineColor, x, y, LineWidth, Height, ref _hueVector);
            }
        }

        private class TextBoxCHB : TextBox
        {
            public TextBoxCHB(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0) : base(font, maxcharlength, maxWidth, width, isunicode, style, hue, TEXT_ALIGN_TYPE.TS_CENTER)
            {
            }
            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                TxEntry.RenderText.Draw(batcher, x + TxEntry.Offset, y);

                if (IsEditable && HasKeyboardFocus)
                    TxEntry.RenderCaret.Draw(batcher, x + TxEntry.Offset + TxEntry.CaretPosition.X, y + TxEntry.CaretPosition.Y);

                return base.Draw(batcher, x, y);
            }
        }

    }

    internal class HealthBarGump : BaseHealthBarGump
    {
        private const ushort BACKGROUND_NORMAL = 0x0803;
        private const ushort BACKGROUND_WAR = 0x0807;
        private const ushort LINE_RED = 0x0805;
        private const ushort LINE_BLUE = 0x0806;
        private const ushort LINE_POISONED = 0x0808;
        private const ushort LINE_YELLOWHITS = 0x0809;

        private const ushort LINE_RED_PARTY = 0x0028;
        private const ushort LINE_BLUE_PARTY = 0x0029;

        private readonly GumpPicWithWidth[] _bars = new GumpPicWithWidth[3];
        private GumpPic _background, _hpLineRed, _manaLineRed, _stamLineRed;

        private Button _buttonHeal1, _buttonHeal2;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;


        public HealthBarGump(Entity entity) : this()
        {
            if (entity == null && CheckIfAnchoredElseDispose())
            {
                return;
            }

            _name = entity.Name;
            _isDead = SerialHelper.IsMobile(entity.Serial) && ((Mobile) entity).IsDead;
            LocalSerial = entity.Serial;

            BuildGump();
        }

        public HealthBarGump(uint serial) : base(serial)
        {
        }

        public HealthBarGump() : base(0, 0)
        {

        }

        public override int GroupMatrixWidth
        {
            get => Width;
            protected set { }
        }

        public override int GroupMatrixHeight
        {
            get => Height;
            protected set { }
        }

        public override void Update()
        {
            Clear();
            Children.Clear();

            _background = _hpLineRed = _manaLineRed = _stamLineRed = null;
            _buttonHeal1 = _buttonHeal2 = null;

            _textBox = null;

            BuildGump();
        }

        protected override void BuildGump()
        {
            WantUpdateSize = false;

            var entity = World.Get(LocalSerial);

            if (World.Party.Contains(LocalSerial))
            {
                Add(_background = new GumpPic(0, 0, BACKGROUND_NORMAL, 0)
                {
                    ContainsByBounds = true,
                    Alpha = 1
                });
                Width = 115;
                Height = 55;

                if (LocalSerial == World.Player)
                {
                    Add(_textBox = new TextBox(3, 32, width: 120, isunicode: false, style: FontStyle.Fixed, hue: Notoriety.GetHue(World.Player.NotorietyFlag))
                    {
                        X = 0,
                        Y = -2,
                        IsEditable = false,
                        CanMove = true,
                        Text = "[* SELF *]"
                    });
                }
                else
                {
                    Add(_textBox = new TextBox(3, 32, width: 109, isunicode: false, style: FontStyle.Fixed | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = -2,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });
                }

                Add(_buttonHeal1 = new Button((int) ButtonParty.Heal1, 0x0938, 0x093A, 0x0938) { ButtonAction = ButtonAction.Activate, X = 0, Y = 20 });
                Add(_buttonHeal2 = new Button((int) ButtonParty.Heal2, 0x0939, 0x093A, 0x0939) { ButtonAction = ButtonAction.Activate, X = 0, Y = 33 });

                Add(_hpLineRed = new GumpPic(18, 20, LINE_RED_PARTY, 0));
                Add(_manaLineRed = new GumpPic(18, 33, LINE_RED_PARTY, 0));
                Add(_stamLineRed = new GumpPic(18, 45, LINE_RED_PARTY, 0));

                Add(_bars[0] = new GumpPicWithWidth(18, 20, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[1] = new GumpPicWithWidth(18, 33, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[2] = new GumpPicWithWidth(18, 45, LINE_BLUE_PARTY, 0, 96));
            }
            else
            {
                if (LocalSerial == World.Player)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Add(_background = new GumpPic(0, 0, _oldWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL, 0) { ContainsByBounds = true });

                    Width = _background.Texture.Width;
                    Height = _background.Texture.Height;

                    // add backgrounds
                    Add(_hpLineRed = new GumpPic(34, 12, LINE_RED, 0));
                    Add(new GumpPic(34, 25, LINE_RED, 0));
                    Add(new GumpPic(34, 38, LINE_RED, 0));

                    // add over
                    Add(_bars[0] = new GumpPicWithWidth(34, 12, LINE_BLUE, 0, 0));
                    Add(_bars[1] = new GumpPicWithWidth(34, 25, LINE_BLUE, 0, 0));
                    Add(_bars[2] = new GumpPicWithWidth(34, 38, LINE_BLUE, 0, 0));
                }
                else
                {
                    ushort textColor = 0x0386;
                    ushort hitsColor = 0x0386;

                    Mobile mobile = entity != null && SerialHelper.IsMobile(entity.Serial) ? (Mobile) entity : null;

                    if (entity != null)
                    {
                        hitsColor = 0;
                        _canChangeName = mobile != null && mobile.IsRenamable;

                        if (_canChangeName)
                            textColor = 0x000E;
                    }

                    ushort barColor = entity == null || entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (ushort) 0 : Notoriety.GetHue(mobile.NotorietyFlag);

                    Add(_background = new GumpPic(0, 0, 0x0804, barColor) { ContainsByBounds = true });
                    Add(_hpLineRed = new GumpPic(34, 38, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(34, 38, LINE_BLUE, 0, 0));

                    Width = _background.Texture.Width;
                    Height = _background.Texture.Height;

                    Add(_textBox = new TextBox(1, 32, width: 120, isunicode: false, hue: textColor, style: FontStyle.Fixed)
                    {
                        X = 16,
                        Y = 14,
                        Width = 120,
                        Height = 15,
                        IsEditable = false,
                        AcceptMouseInput = _canChangeName,
                        AcceptKeyboardInput = _canChangeName,
                        SafeCharactersOnly = true,
                        WantUpdateSize = false,
                        CanMove = true,
                        Text = _name
                    });
                    if (_canChangeName)
                        _textBox.MouseUp += TextBoxOnMouseUp;
                }
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed /* || (_textBox != null && _textBox.IsDisposed)*/)
                return;

            bool inparty = World.Party.Contains(LocalSerial);


            ushort textColor = 0x0386;
            ushort hitsColor = 0x0386;

            Entity entity = World.Get(LocalSerial);

            if (entity == null || entity.IsDestroyed)
            {
                if (LocalSerial != World.Player && (ProfileManager.Current.CloseHealthBarType == 1 ||
                                                    ProfileManager.Current.CloseHealthBarType == 2 && World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000)))
                {
                    if (CheckIfAnchoredElseDispose())
                        return;
                }

                if (_isDead)
                    _isDead = false;

                if (!_outOfRange)
                {
                    //_poisoned = false;
                    //_yellowHits = false;
                    //_normalHits = true;

                    _outOfRange = true;

                    if (inparty)
                    {
                        hitsColor = textColor = 912;

                        if (_textBox != null && _textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = false;
                            _bars[2].IsVisible = false;
                        }
                    }
                    else
                    {
                        if (_textBox != null)
                        {
                            if (_textBox.Hue != textColor)
                                _textBox.Hue = textColor;

                            if (_canChangeName)
                                _textBox.MouseUp -= TextBoxOnMouseUp;
                            _textBox.IsEditable = false;
                        }
                    }

                    if (_background.Hue != 0)
                        _background.Hue = 0;

                    if (_hpLineRed.Hue != hitsColor)
                    {
                        _hpLineRed.Hue = hitsColor;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                    }

                    _bars[0].IsVisible = false;
                }
            }

            if (entity != null && !entity.IsDestroyed)
            {
                Mobile mobile = SerialHelper.IsMobile(entity.Serial) ? (Mobile) entity : null;

                if (!_isDead && entity != World.Player && (mobile != null && mobile.IsDead) && ProfileManager.Current.CloseHealthBarType == 2) // is dead
                {
                    if (CheckIfAnchoredElseDispose())
                        return;
                }

                if (entity is Mobile mm && _canChangeName != mm.IsRenamable)
                {
                    _canChangeName = mm.IsRenamable;

                    if (_textBox != null)
                    {
                        _textBox.MouseUp -= TextBoxOnMouseUp;

                        if (_canChangeName)
                        {
                            _textBox.MouseUp += TextBoxOnMouseUp;
                        }
                        else
                            _textBox.IsEditable = false;
                    }
                }

                if (!(mobile != null && mobile.IsDead) && _isDead)
                    _isDead = false;

                if (!string.IsNullOrEmpty(entity.Name) && _name != entity.Name)
                {
                    _name = entity.Name;
                    if (_textBox != null)
                        _textBox.Text = _name;
                }

                if (_outOfRange)
                {
                    if (entity.HitsMax == 0)
                        GameActions.RequestMobileStatus(entity);

                    _outOfRange = false;

                    _canChangeName = !inparty && mobile != null && mobile.IsRenamable;

                    if (_canChangeName)
                    {
                        if (_textBox != null)
                        {
                            _textBox.MouseUp -= TextBoxOnMouseUp;
                            _textBox.MouseUp += TextBoxOnMouseUp;
                        }
                    }

                    hitsColor = 0;

                    if (inparty)
                    {
                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = true;

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = true;
                            _bars[2].IsVisible = true;
                        }
                    }

                    if (_hpLineRed.Hue != hitsColor)
                    {
                        _hpLineRed.Hue = hitsColor;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                    }

                    _bars[0].IsVisible = true;
                }

                if (inparty && mobile != null)
                    textColor = Notoriety.GetHue(mobile.NotorietyFlag);
                else
                {
                    if (_canChangeName)
                    {
                        textColor = 0x000E;
                    }
                }


                if (_textBox != null && _textBox.Hue != textColor)
                    _textBox.Hue = textColor;

                ushort barColor = entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (ushort) 0 : Notoriety.GetHue(mobile.NotorietyFlag);

                if (_background.Hue != barColor)
                    _background.Hue = barColor;

                if (mobile != null && mobile.IsPoisoned && !_poisoned)
                {
                    if (inparty)
                        _bars[0].Hue = 63;
                    else
                        _bars[0].Graphic = LINE_POISONED;

                    _poisoned = true;
                    _normalHits = false;
                }
                else if (mobile != null && mobile.IsYellowHits && !_yellowHits)
                {
                    if (inparty)
                        _bars[0].Hue = 353;
                    else
                        _bars[0].Graphic = LINE_YELLOWHITS;
                    _yellowHits = true;
                    _normalHits = false;
                }
                else if (!_normalHits && (mobile != null && !mobile.IsPoisoned && !mobile.IsYellowHits) && (_poisoned || _yellowHits))
                {
                    if (inparty)
                        _bars[0].Hue = 0;
                    else
                        _bars[0].Graphic = LINE_BLUE;
                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;
                }

                int barW = inparty ? 96 : 109;

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, barW);


                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;
                    _oldHits = hits;
                }


                if ((inparty || LocalSerial == World.Player) && mobile != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, barW);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, barW);

                    if (mana != _oldMana && _bars.Length >= 2 && _bars[1] != null)
                    {
                        _bars[1].Percent = mana;
                        _oldMana = mana;
                    }

                    if (stam != _oldStam && _bars.Length >= 2 && _bars[2] != null)
                    {
                        _bars[2].Percent = stam;
                        _oldStam = stam;
                    }
                }


                if ( /*!Mobile.IsSelected &&*/ UIManager.MouseOverControl != null && UIManager.MouseOverControl.RootParent == this)
                {
                    //Mobile.IsSelected = true;
                    SelectedObject.HealthbarObject = entity;
                    SelectedObject.Object = entity;
                    SelectedObject.LastObject = entity;
                }
            }

            if (LocalSerial == World.Player)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    _background.Graphic = World.Player.InWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL;

                }
            }
        }

        public override void Dispose()
        {
            var entity = World.Get(LocalSerial);

            if (Client.Version >= ClientVersion.CV_200 && World.InGame && entity != null)
                NetClient.Socket.Send(new PCloseStatusBarGump(entity));

            if (SelectedObject.HealthbarObject == entity && entity != null)
                SelectedObject.HealthbarObject = null;
            base.Dispose();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonParty) buttonID)
            {
                case ButtonParty.Heal1:
                    GameActions.CastSpell(29);
                    World.Party.PartyHealTimer = Time.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;

                case ButtonParty.Heal2:
                    GameActions.CastSpell(11);
                    World.Party.PartyHealTimer = Time.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;
            }

            Mouse.CancelDoubleClick = true;
            Mouse.LastLeftButtonClickTime = 0;
        }


        private enum ButtonParty
        {
            Heal1,
            Heal2
        }
    }
}