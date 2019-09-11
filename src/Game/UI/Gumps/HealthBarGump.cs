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
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;

//Custom HealthBarGump Notes

//ClassicUO Custom Health Bars by Alan(Syrupz)
//Notoriety Organizer not included in this version until shard owners respond regarding the potential abuse of this.
//Option between bar types(Straight Line(current)/Classic) not included in this version due to majority win (78-22) of a 100 asked.
//Option with buttons for party members not active but included; 1 out of 100 asked wanted buttons, not enough to include but they are correctly set position wise and will require only an increase in BG/Border to add them in.

//Cursor position for renaming pets is a present issue, you CAN rename your pet but the alignment on the X axis needs to be fixed.


namespace ClassicUO.Game.UI.Gumps
{
    internal class HealthBarGump : AnchorableGump
    {
        private const ushort LINE_RED = 0x0805;
        private const ushort LINE_BLUE = 0x0806;
        private const ushort LINE_POISONED = 0x0808;
        private const ushort LINE_YELLOWHITS = 0x0809;

        private const ushort LINE_RED_PARTY = 0x0805; //duplicate of Line_RED
        private const ushort LINE_BLUE_PARTY = 0x0806; //duplicate of Line BLUE

        private readonly GumpPicWithWidth[] _bars = new GumpPicWithWidth[3];
        private GumpPic _hpLineRed, _manaLineRed, _stamLineRed;// _background,
        private AlphaBlendControl _background;
        //BUTTONS
        //private Button _buttonHeal1, _buttonHeal2;
        //
        private bool _canChangeName;
        private bool _isDead;
        private string _name;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;

        private bool _targetBroke;

        private TextBox _textBox;
        private readonly RenderedText _renderedText;
        private const int MIN_WIDTH = 120;

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
            
            Hue hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (Hue)0x0481;
            _renderedText = RenderedText.Create(String.Empty, hue, 0xFF, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 100, 30, true);

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

            _hpLineRed = _manaLineRed = _stamLineRed = null;// _background =
            //BUTTONS
            //_buttonHeal1 = _buttonHeal2 = null;
            //
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
                    _outOfRange = true;

                    if (inparty)
                    {
                        hitsColor = textColor = 912;

                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;
                        //BUTTONS
                        //_buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;
                        //
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
                Mobile mobile = entity.Serial.IsMobile ? (Mobile)entity : null;

                if (!_isDead && entity != World.Player && (mobile != null && mobile.IsDead) && Engine.Profile.Current.CloseHealthBarType == 2) // is dead
                {
                    Dispose();

                    return;
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
                        //BUTTONS
                        //_buttonHeal1.IsVisible = _buttonHeal2.IsVisible = true;
                        //
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

                Hue barColor = entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue)0 : Notoriety.GetHue(mobile.NotorietyFlag);
                Hue bgcolor = 0x0386;

                if (_background.Hue != bgcolor)
                    _background.Hue = bgcolor;

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

                int barW = inparty ? 100 : 109; //works for 100 hp and strengthed to 120 hp - 96 was causing party bars to indicate red on bar at 100 hp.

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, barW);
               
                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;
                    _oldHits = hits;
                }


                if ((inparty || CanBeSaved) && mobile != null)
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
                    Height = 25;
                    Width = 25;

                    Add(_background = new AlphaBlendControl(0.4f) { Width = Width, Height = Height });
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
        //BUTTONS
        /*public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonParty)buttonID)
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
        }*/
        //
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

            int namewidth = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, _name);
            
            if (World.Party.Contains(LocalSerial))
            {
                Height = 60;
                Width = 120;

                Add(new GameBorder(0, 0, 120, 60, 1 / 2));
                Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });
                if (CanBeSaved)
                {

                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, style: FontStyle.Cropped, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 60,
                        IsEditable = false,
                        CanMove = true,                        
                    });
                }
                else
                {
                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, style: FontStyle.Cropped | FontStyle.BlackBorder, hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 60,
                        IsEditable = false,
                        CanMove = true,                      
                    });
                }

                
                //BUTTONS
                //Add(_buttonHeal1 = new Button((int)ButtonParty.Heal1, 0x0938, 0x093A, 0x0938) { ButtonAction = ButtonAction.Activate, X = -12, Y = 27 });
                //Add(_buttonHeal2 = new Button((int)ButtonParty.Heal2, 0x0939, 0x093A, 0x0939) { ButtonAction = ButtonAction.Activate, X = -12, Y = 40 });
                //
                Add(_hpLineRed = new GumpPic(6, 27, LINE_RED_PARTY, 0));
                Add(_manaLineRed = new GumpPic(6, 36, LINE_RED_PARTY, 0));
                Add(_stamLineRed = new GumpPic(6, 45, LINE_RED_PARTY, 0));

                Add(_bars[0] = new GumpPicWithWidth(6, 27, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[1] = new GumpPicWithWidth(6, 36, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[2] = new GumpPicWithWidth(6, 45, LINE_BLUE_PARTY, 0, 96));
            }
            else
            {
                if (CanBeSaved)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Height = 60;
                    Width = 120;

                    Add(new GameBorder(0, 0, 120, 60, 1 / 2));
                    Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });

                    // add backgrounds
                    Add(_hpLineRed = new GumpPic(6, 27, LINE_RED, 0));
                    Add(new GumpPic(6, 36, LINE_RED, 0));
                    Add(new GumpPic(6, 45, LINE_RED, 0));

                    // add over
                    Add(_bars[0] = new GumpPicWithWidth(6, 27, LINE_BLUE, 0, 0));
                    Add(_bars[1] = new GumpPicWithWidth(6, 36, LINE_BLUE, 0, 0));
                    Add(_bars[2] = new GumpPicWithWidth(6, 45, LINE_BLUE, 0, 0));
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

                    Hue barColor = entity == null || entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue)0 : Notoriety.GetHue(mobile.NotorietyFlag);


                    Height = 36;
                    Width = 120;

                    Add(new GameBorder(0, 0, 120, 36, 1 / 2));
                    Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });
                    Add(_hpLineRed = new GumpPic(6, 21, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(6, 21, LINE_BLUE, 0, 0));
                                                   
                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, hue: Notoriety.GetHue(mobile.NotorietyFlag), style: FontStyle.Fixed)
                    {
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 36, 
                        IsEditable = false,
                        AcceptMouseInput = _canChangeName,
                        AcceptKeyboardInput = _canChangeName,
                        SafeCharactersOnly = true,
                        WantUpdateSize = false,
                        CanMove = true,                        
                    });
                    if (_canChangeName) _textBox.MouseUp += TextBoxOnMouseUp;
                }
            }

            bool inparty = World.Party.Contains(LocalSerial);
            _renderedText.MaxWidth = namewidth;

            _renderedText.Text = _name;

            Width = _background.Width = Math.Max(_renderedText.Width + 5, MIN_WIDTH);

            if (LocalSerial == World.Player || inparty)
            {
                Height = _background.Height = _renderedText.Height + 39;
            }
            else
            {
                Height = _background.Height = _renderedText.Height + 15;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            Texture2D color = Textures.GetTexture(Color.Gray);
            base.Draw(batcher, x, y);
            color = Textures.GetTexture(Color.Black);
            
            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;
            
            bool inparty = World.Party.Contains(LocalSerial);
            if (LocalSerial == World.Player || inparty)
            {
                batcher.DrawRectangle(color, x, y, 120, 60, ref _hueVector);
                _renderedText.Draw(batcher, x + 2 + renderedTextOffset, y + 4, Width, Height, 0, 0);
            }
            else
            {
                batcher.DrawRectangle(color, x, y, 120, 36, ref _hueVector);
                _renderedText.Draw(batcher, x + 2 + renderedTextOffset, y, Width, Height, 0, 0);
            }
            return true;
        }
        private void TextBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButton.Left)
                return;

            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;

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
            var entity = World.Get(LocalSerial);

            if (entity != null)
            {
                if (entity != World.Player)
                {
                    if (World.Player.InWarMode && World.Player != entity)
                        GameActions.Attack(entity);
                    else if (button == MouseButton.Left) GameActions.DoubleClick(entity);
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
