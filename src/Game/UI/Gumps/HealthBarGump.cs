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

using System;
using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class HealthBarGump : AnchorableGump
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
        private bool _canChangeName;
        private bool _isDead;
        private string _name;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;

        private bool _targetBroke;

        //private Label _partyNameLabel;
        private TextBox _textBox;


        public HealthBarGump(Entity entity) : this()
        {
            if (entity == null)
            {
                Dispose();

                return;
            }

            _name = entity.Name;
            _isDead = entity.Serial.IsMobile && ((Mobile)entity).IsDead;
            LocalSerial = entity.Serial;

            BuildGump();
        }

        public HealthBarGump(Serial mob) : this(World.Get(mob))
        {
        }

        public HealthBarGump() : base(0, 0)
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

            _background = _hpLineRed = _manaLineRed = _stamLineRed = null;
            _buttonHeal1 = _buttonHeal2 = null;
            _textBox = null;

            BuildGump();
            Initialize();
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed /* || (_textBox != null && _textBox.IsDisposed)*/)
                return;

            bool inparty = World.Party.Contains(LocalSerial);


            Hue textColor = 0x0386;
            Hue hitsColor = 0x0386;

            Entity entity = World.Get(LocalSerial);

            if (entity == null || entity.IsDestroyed)
            {
                if (LocalSerial != World.Player && (Engine.Profile.Current.CloseHealthBarType == 1 ||
                                                    Engine.Profile.Current.CloseHealthBarType == 2 && World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000)))
                {
                    Dispose();

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

                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;

                        _bars[1].IsVisible = false;
                        _bars[2].IsVisible = false;
                    }
                    else
                    {
                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        if (_canChangeName)
                            _textBox.MouseUp -= TextBoxOnMouseUp;
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
                Mobile mobile = entity.Serial.IsMobile ? (Mobile) entity : null;

                if (!_isDead && entity != World.Player && (mobile != null && mobile.IsDead) && Engine.Profile.Current.CloseHealthBarType == 2) // is dead
                {
                    Dispose();

                    return;
                }

                if (!(mobile != null && mobile.IsDead) && _isDead) _isDead = false;

                if (!string.IsNullOrEmpty(entity.Name) && _name != entity.Name)
                {
                    _name = entity.Name;
                    if(_textBox != null)
                        _textBox.Text = _name;
                }

                if (_outOfRange)
                {
                    if (entity.HitsMax == 0)
                        GameActions.RequestMobileStatus(entity);

                    _outOfRange = false;

                    hitsColor = 0;

                    if (inparty && mobile != null)
                        textColor = Notoriety.GetHue(mobile.NotorietyFlag); //  _barColor;
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
                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = true;

                        _bars[1].IsVisible = true;
                        _bars[2].IsVisible = true;
                    }

                    if (_hpLineRed.Hue != hitsColor)
                    {
                        _hpLineRed.Hue = hitsColor;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                    }

                    _bars[0].IsVisible = true;
                }

                Hue barColor = entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue) 0 : Notoriety.GetHue(mobile.NotorietyFlag);

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


                if ( (inparty || CanBeSaved) && mobile != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, barW);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, barW);

                    if (mana != _oldMana)
                    {
                        _bars[1].Percent = mana;
                        _oldMana = mana;
                    }

                    if (stam != _oldStam)
                    {
                        _bars[2].Percent = stam;
                        _oldStam = stam;
                    }
                }


                if ( /*!Mobile.IsSelected &&*/ Engine.UI.MouseOverControl != null && Engine.UI.MouseOverControl.RootParent == this)
                {
                    //Mobile.IsSelected = true;
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

                    _background.Graphic = World.Player.InWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL;
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

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonParty) buttonID)
            {
                case ButtonParty.Heal1:
                    GameActions.CastSpell(29);
                    World.Party.PartyHealTimer = Engine.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;

                case ButtonParty.Heal2:
                    GameActions.CastSpell(11);
                    World.Party.PartyHealTimer = Engine.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;
            }

            Mouse.CancelDoubleClick = true;
            Mouse.LastLeftButtonClickTime = 0;
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

        private void BuildGump()
        {
            CanBeSaved = LocalSerial == World.Player;

            WantUpdateSize = false;


            var entity = World.Get(LocalSerial);

            if (World.Party.Contains(LocalSerial))
            {
                Add(_background = new GumpPic(0, 0, BACKGROUND_NORMAL, 0)
                {
                    Alpha = 1
                });
                Width = 115;
                Height = 55;

                if (CanBeSaved)
                {
                    Add(_textBox = new TextBox(3, width: 120, isunicode: false, style: FontStyle.Fixed, hue: Notoriety.GetHue(World.Player.NotorietyFlag))
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
                    Add(_textBox = new TextBox(3, width: 109, isunicode: false, style: FontStyle.Fixed | FontStyle.BlackBorder, hue: Notoriety.GetHue(  (entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = -2,
                        IsEditable = false,
                        CanMove = true,
                        Text = _name
                    });
                }

                Add(_buttonHeal1 = new Button((int) ButtonParty.Heal1, 0x0938, 0x093A, 0x0938) {ButtonAction = ButtonAction.Activate, X = 0, Y = 20});
                Add(_buttonHeal2 = new Button((int) ButtonParty.Heal2, 0x0939, 0x093A, 0x0939) {ButtonAction = ButtonAction.Activate, X = 0, Y = 33});

                Add(_hpLineRed = new GumpPic(18, 20, LINE_RED_PARTY, 0));
                Add(_manaLineRed = new GumpPic(18, 33, LINE_RED_PARTY, 0));
                Add(_stamLineRed = new GumpPic(18, 45, LINE_RED_PARTY, 0));

                Add(_bars[0] = new GumpPicWithWidth(18, 20, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[1] = new GumpPicWithWidth(18, 33, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[2] = new GumpPicWithWidth(18, 45, LINE_BLUE_PARTY, 0, 96));
            }
            else
            {
                if (CanBeSaved)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Add(_background = new GumpPic(0, 0, _oldWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL, 0));

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
                    Hue textColor = 0x0386;
                    Hue hitsColor = 0x0386;

                    Mobile mobile = entity != null && entity.Serial.IsMobile ? (Mobile) entity : null;

                    if (entity != null)
                    {
                        hitsColor = 0;
                        _canChangeName = mobile != null && mobile.IsRenamable;

                        if (_canChangeName)
                            textColor = 0x000E;
                    }

                    Hue barColor = entity == null || entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue) 0 : Notoriety.GetHue(mobile.NotorietyFlag);

                    Add(_background = new GumpPic(0, 0, 0x0804, barColor));
                    Add(_hpLineRed = new GumpPic(34, 38, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(34, 38, LINE_BLUE, 0, 0));

                    Width = _background.Texture.Width;
                    Height = _background.Texture.Height;

                    Add(_textBox = new TextBox(1, width: 120, isunicode: false, hue: textColor, style: FontStyle.Fixed)
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

            if ( /*(TargetManager.IsTargeting || World.Player.InWarMode) && */entity != null)
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

        private enum ButtonParty
        {
            Heal1,
            Heal2
        }
    }
}