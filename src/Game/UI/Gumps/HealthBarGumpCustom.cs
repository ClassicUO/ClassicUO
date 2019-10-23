#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;
using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;


namespace ClassicUO.Game.UI.Gumps
{
    internal class HealthBarGumpCustom : AnchorableGump
    {
        private readonly LineCHB[] _bars = new LineCHB[3];
        private readonly LineCHB[] _border = new LineCHB[4];

        private LineCHB _hpLineRed, _manaLineRed, _stamLineRed, _outline;

        private bool _canChangeName;
        private bool _isDead;
        private string _name;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;

        private bool _targetBroke;

        private TextBoxCHB _textBox;

        private const int HPB_WIDTH = 120;
        private const int HPB_HEIGHT_MULTILINE = 60;
        private const int HPB_HEIGHT_SINGLELINE = 36;
        private const int HPB_BORDERSIZE = 1;
        private const int HPB_OUTLINESIZE = 1;

        private const int HPB_BAR_SPACELEFT = (HPB_WIDTH - HPB_BAR_WIDTH) / 2;

        private const int HPB_BAR_WIDTH = 100;
        private const int HPB_BAR_HEIGHT = 8;

        private static Color HPB_COLOR_DRAW_RED = Color.Red;
        private static Color HPB_COLOR_DRAW_BLUE = Color.DodgerBlue;
        private static Color HPB_COLOR_DRAW_BLACK = Color.Black;

        private static readonly Texture2D HPB_COLOR_BLUE = Textures.GetTexture(Color.DodgerBlue);
        private static readonly Texture2D HPB_COLOR_GRAY = Textures.GetTexture(Color.Gray);
        private static readonly Texture2D HPB_COLOR_RED = Textures.GetTexture(Color.Red);
        private static readonly Texture2D HPB_COLOR_YELLOW = Textures.GetTexture(Color.Orange);
        private static readonly Texture2D HPB_COLOR_POISON = Textures.GetTexture(Color.LimeGreen);
        private static readonly Texture2D HPB_COLOR_BLACK = Textures.GetTexture(Color.Black);

        public AlphaBlendControl _background;
        public HealthBarGumpCustom(Entity entity) : this()
        {
            if (entity == null)
            {
                Dispose();

                return;
            }


            _name = entity.Name;
            _isDead = entity.Serial.IsMobile && ((Mobile)entity).IsDead;
            LocalSerial = entity.Serial;

            BuildCustomGump();
        }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        public HealthBarGumpCustom(Serial mob) : this(World.Get(mob))
        {
        }
        public HealthBarGumpCustom() : base(0, 0)
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
        public void Update()
        {
            Clear();
            Children.Clear();

            _background = null;
            _hpLineRed = _manaLineRed = _stamLineRed = null;

            _textBox?.Dispose();
            _textBox = null;

            BuildCustomGump();
            Initialize();
        }
        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            bool inparty = World.Party.Contains(LocalSerial);


            Hue textColor = 0x0386;

            Entity entity = World.Get(LocalSerial);

            if (entity == null || entity.IsDestroyed)
            {
                if (LocalSerial != World.Player && (Engine.Profile.Current.CloseHealthBarType == 1 ||
                                                    Engine.Profile.Current.CloseHealthBarType == 2 && World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000)))
                {
                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                    if (!inparty)
                    {
                        Dispose();

                        return;
                    }
                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                }

                if (_isDead)
                    _isDead = false;

                if (!_outOfRange)
                {
                    _outOfRange = true;

                    if (inparty)
                    {
                        textColor = 912;

                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        _bars[1].IsVisible = false;
                        _bars[2].IsVisible = false;
                    }
                    else
                    {
                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        if (_canChangeName)
                            _textBox.MouseUp -= TextBoxOnMouseUp;
                        _textBox.IsEditable = false;
                    }

                    if (_background.Hue != 0)
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
                Mobile mobile = entity.Serial.IsMobile ? (Mobile)entity : null;

                if (!_isDead && entity != World.Player && (mobile != null && mobile.IsDead) && Engine.Profile.Current.CloseHealthBarType == 2) // is dead
                {
                    if (!inparty)
                    {
                        Dispose();

                        return;
                    }
                }

                if (entity is Mobile mm && _canChangeName != mm.IsRenamable)
                {
                    _canChangeName = mm.IsRenamable;
                    _textBox.MouseUp -= TextBoxOnMouseUp;

                    if (_canChangeName)
                    {
                        _textBox.MouseUp += TextBoxOnMouseUp;
                    }
                    else
                        _textBox.IsEditable = false;
                }

                if (!(mobile != null && mobile.IsDead) && _isDead) _isDead = false;
                
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

                    textColor = Notoriety.GetHue(mobile.NotorietyFlag);

                    if (inparty && mobile != null)
                    {
                        textColor = Notoriety.GetHue(mobile.NotorietyFlag);
                    }
                    else
                    {
                        _canChangeName = mobile != null && mobile.IsRenamable;
                        
                        if (_canChangeName)
                        {
                            textColor = 0x000E;
                            _textBox.MouseUp += TextBoxOnMouseUp;
                        }
                    }

                    if (_textBox.Hue != textColor)
                        _textBox.Hue = textColor;

                    if (inparty)
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

                Hue barColor = entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? Notoriety.GetHue(mobile.NotorietyFlag) : Notoriety.GetHue(mobile.NotorietyFlag);

                if (_background.Hue != barColor)
                {

                    if (inparty && mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (!Engine.Profile.Current.CBBlackBGToggled)
                    {
                        _background.Hue = barColor;
                    }
                }

                if (_background.Hue != 912)
                {
                    if (inparty && mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (Engine.Profile.Current.CBBlackBGToggled)
                    {
                        _background.Hue = 912;
                    }
                }

                if (mobile != null && mobile.IsPoisoned && !_poisoned)
                {
                    _bars[0].LineColor = HPB_COLOR_POISON;

                    _poisoned = true;
                    _normalHits = false;
                }
                else if (mobile != null && mobile.IsYellowHits && !_yellowHits)
                {

                    _bars[0].LineColor = HPB_COLOR_YELLOW;

                    _yellowHits = true;
                    _normalHits = false;
                }
                else if (!_normalHits && (mobile != null && !mobile.IsPoisoned && !mobile.IsYellowHits) && (_poisoned || _yellowHits))
                {

                    _bars[0].LineColor = HPB_COLOR_BLUE;

                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;
                }

                int barW = HPB_BAR_WIDTH;

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, barW);

                if (hits != _oldHits)
                {
                    _bars[0].LineWidth = hits;
                    _oldHits = hits;
                }

                if ((inparty || CanBeSaved) && mobile != null && _bars != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, barW);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, barW);

                    if (mana != _oldMana)
                    {
                        _bars[1].LineWidth = mana;
                        _oldMana = mana;
                    }

                    if (stam != _oldStam)
                    {
                        _bars[2].LineWidth = stam;
                        _oldStam = stam;
                    }
                }

                if (Engine.UI.MouseOverControl != null && Engine.UI.MouseOverControl.RootParent == this)
                {
                    SelectedObject.HealthbarObject = entity;
                    SelectedObject.Object = entity;
                    SelectedObject.LastObject = entity;
                }
            }

            if (CanBeSaved)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = World.Player.InWarMode ? HPB_COLOR_RED : HPB_COLOR_BLACK;
                }
            }
        }
        public override void Dispose()
        {
            var entity = World.Get(LocalSerial);

            if (FileManager.ClientVersion >= ClientVersions.CV_200 && World.InGame && entity != null)
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
                BuildCustomGump();
            }
            else
                Dispose();
        }
        private void BuildCustomGump()
        {
            CanBeSaved = LocalSerial == World.Player;

            WantUpdateSize = false;

            var entity = World.Get(LocalSerial);


            if (World.Party.Contains(LocalSerial))
            {
                Height = HPB_HEIGHT_MULTILINE;
                Width = HPB_WIDTH;
                Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true});


                if (CanBeSaved)
                {
                    Add(_textBox = new TextBoxCHB(3, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Fixed | FontStyle.BlackBorder, hue: Notoriety.GetHue(World.Player.NotorietyFlag))
                    {
                        X = 0,
                        Y = 0,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });
                }
                else
                {
                    Add(_textBox = new TextBoxCHB(3, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Fixed | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = 0,
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
                if (CanBeSaved)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Height = HPB_HEIGHT_MULTILINE;
                    Width = HPB_WIDTH;
                    Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                    Add(_textBox = new TextBoxCHB(3, width: HPB_BAR_WIDTH, isunicode: true, style: FontStyle.Fixed | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray),maxWidth: Width)
                    {
                        X = 0,
                        Y = 0,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });

                    Add(_outline = new LineCHB(HPB_BAR_SPACELEFT - HPB_OUTLINESIZE, 27 - HPB_OUTLINESIZE, HPB_BAR_WIDTH + (HPB_OUTLINESIZE * 2), (HPB_BAR_HEIGHT * 3) + 2 + (HPB_OUTLINESIZE * 2), HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_hpLineRed = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                    Add(new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));
                    Add(new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_RED.PackedValue));

                    Add(_bars[0] = new LineCHB(HPB_BAR_SPACELEFT, 27, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue){ LineWidth = 0});
                    Add(_bars[1] = new LineCHB(HPB_BAR_SPACELEFT, 36, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue){ LineWidth = 0});
                    Add(_bars[2] = new LineCHB(HPB_BAR_SPACELEFT, 45, HPB_BAR_WIDTH, HPB_BAR_HEIGHT, HPB_COLOR_DRAW_BLUE.PackedValue) { LineWidth = 0 });

                    Add(_border[0] = new LineCHB(0, 0, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[1] = new LineCHB(0, HPB_HEIGHT_MULTILINE - HPB_BORDERSIZE, HPB_WIDTH, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[2] = new LineCHB(0, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));
                    Add(_border[3] = new LineCHB(HPB_WIDTH - HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));

                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = _oldWarMode ? HPB_COLOR_RED : HPB_COLOR_BLACK;
                }
                else
                {
                    Hue textColor = 0x0386;
                    Hue hitsColor = 0x0386;

                    Mobile mobile = entity != null && entity.Serial.IsMobile ? (Mobile)entity : null;

                    if (entity != null)
                    {
                        hitsColor = 0;
                        _canChangeName = mobile != null && mobile.IsRenamable;

                        if (_canChangeName)
                            textColor = 0x000E;
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

                    Add(_textBox = new TextBoxCHB(3, width: HPB_BAR_WIDTH, isunicode: true, hue: Notoriety.GetHue(mobile.NotorietyFlag), style: FontStyle.Fixed | FontStyle.BlackBorder)
                    {
                        X = 0,
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

                    if (_canChangeName) _textBox.MouseUp += TextBoxOnMouseUp;
                }
            }
        }
        private void TextBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButton.Left)
                return;

            Point p = Mouse.LDroppedOffset;

            if (Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) >= 1) return;

            if (TargetManager.IsTargeting)
            {
                TargetManager.TargetGameObject(World.Get(LocalSerial));
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName && !_targetBroke)
            {
                _textBox.IsEditable = true;
                _textBox.SetKeyboardFocus();
            }

            _targetBroke = false;
        }
        private static int CalculatePercents(int max, int current, int maxValue)
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
        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            if (TargetManager.IsTargeting)
            {
                _targetBroke = true;
                TargetManager.TargetGameObject(World.Get(LocalSerial));
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName)
            {
                _textBox.IsEditable = false;
                Engine.UI.SystemChat.SetFocus();
            }
        }
        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return false;

            var entity = World.Get(LocalSerial);

            if (entity != null)
            {
                if (entity != World.Player)
                {
                    if (World.Player.InWarMode)
                        GameActions.Attack(entity);
                    else
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

            if (entity == null || entity.Serial.IsItem)
                return;

            if ((key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) && _textBox.IsEditable)
            {
                GameActions.Rename(entity, _textBox.Text);
                Engine.UI.SystemChat?.SetFocus();
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


        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color) : base(x, y, w, h, color)
            {
                LineWidth = w;
                LineColor = Textures.GetTexture(new Color() { PackedValue = color });

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
}
