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

// Health Bar Gump Custom v.1 by Syrupz(Alan)
//
// The goal of this was to simply modernize the Health Bar Gumps while still giving people
// an option to continue using the classic Health Bar Gumps. The option to overide bar types
// be it (straight line(custom) or graphic(classic) is not directly included in this version
// however it will use your default settings(art modifications) if needed.
//
// I purposefully did not included my Notoriety Organizer to this version as I am still waiting
// to hear back from shard owners &/OR KaRaShO to respond regarding the potential abuse of this.
// Also I made a poll of 100(exactly) people whether or not they wanted healthbar buttons to be
// added into the custom bars when in party mode, the results were 93-7 in favor of not having
// them included. On my current version I do have this added if more desire for this ever comes.
//
// There should be no problems with this version as I've thoroughly tested it but if anything does 
// crash or seems like a bug/issue, please DM me on Discord at Alan#0084 and I will get it fixed.
// At present the only known issue is when using the Custom Health Bars and renaming a pet. The text
// is rendered so the option to edit that has been added as a bandaid but not fully integrated. You
// can rename pets, the entry cursor will reside to the left side of the bar but it will not remove
// the current name. You will need to type enter and re-pull the bar to see the name change.
//
// Lastly, I want to give thanks to KaRaShO, Roxya, Stalli, Gaechti, and Link for giving me tips on how
// to get these Health Bars to function per my own vision; gratitude.

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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class HealthBarGumpCustom : AnchorableGump
    {
        public RenderedText _renderedText;
        public AlphaBlendControl _background;

        private const ushort LINE_RED = 0x0805;
        private const ushort LINE_BLUE = 0x0806;
        private const ushort LINE_POISONED = 0x0808;
        private const ushort LINE_YELLOWHITS = 0x0809;
        public const int MIN_WIDTH = 120;

        private readonly GumpPicWithWidth[] _bars = new GumpPicWithWidth[3];
        private GumpPic _hpLineRed, _manaLineRed, _stamLineRed;

        private bool _canChangeName;
        private bool _isDead;
        private string _name;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private bool _outOfRange;

        private bool _targetBroke;

        private TextBox _textBox;

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
            Hue hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (Hue)0x0481;
            _renderedText = RenderedText.Create(String.Empty, hue, 0xFF, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 100, 30, true);

            Entity = entity;

            BuildCustomGump();
        }

        public Entity Entity { get; }

        public HealthBarGumpCustom(Serial mob) : this(World.Mobiles.Get(mob))
        {
        }

        public HealthBarGumpCustom() : base(0, 0)
        {
            AcceptKeyboardInput = true;
            CanMove = true;
            AnchorGroupName = "customhealthbar";
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
        //protected override void CloseWithRightClick()
        //{
        //   CanCloseWithRightClick = false;            
        //}

        public void Update()
        {
            Clear();
            Children.Clear();

            _hpLineRed = _manaLineRed = _stamLineRed = null;
            _textBox = null;

            BuildCustomGump();
            Initialize();
        }
        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Entity == null || Entity.IsDestroyed)
                Dispose();

            if (IsDisposed)
                return;

            bool inparty = World.Party.Contains(LocalSerial);

            Hue textColor = 0x0386;
            Hue hitsColor = 0x0386;

            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (mobile == null || mobile.IsDestroyed)
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

            if (mobile != null && !mobile.IsDestroyed)
            {

                if (!_isDead && mobile != World.Player && mobile.IsDead && Engine.Profile.Current.CloseHealthBarType == 2) // is dead
                {
                    Dispose();

                    return;
                }

                if (!mobile.IsDead && _isDead) _isDead = false;

                if (!string.IsNullOrEmpty(mobile.Name) && _name != mobile.Name)
                {
                    _name = mobile.Name;
                    if (_textBox != null)
                        _textBox.Text = _name;

                }

                if (_outOfRange)
                {
                    if (mobile.HitsMax == 0)
                        GameActions.RequestMobileStatus(mobile);

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

                Hue barColor = mobile == World.Player || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue)0 : Notoriety.GetHue(mobile.NotorietyFlag);
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

                int barW = inparty ? 108 : 108;

                int hits = CalculatePercents(mobile.HitsMax, mobile.Hits, barW);

                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;
                    _oldHits = hits;
                }

                if ((inparty || CanBeSaved) && mobile != null && _bars[1] != null && _bars[2] != null)
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


                if (Engine.UI.MouseOverControl != null && Engine.UI.MouseOverControl.RootParent == this)
                {
                    SelectedObject.HealthbarObject = mobile;
                    SelectedObject.Object = mobile;
                    SelectedObject.LastObject = mobile;
                }
            }


            if (CanBeSaved)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;
                }
            }
        }


        public override void Dispose()
        {
            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (FileManager.ClientVersion >= ClientVersions.CV_200 && World.InGame && mobile != null)
                NetClient.Socket.Send(new PCloseStatusBarGump(mobile));

            if (SelectedObject.HealthbarObject == mobile && mobile != null)
                SelectedObject.HealthbarObject = null;

            base.Dispose();
        }

        private void BuildCustomGump()
        {
            CanBeSaved = LocalSerial == World.Player;

            WantUpdateSize = false;

            Mobile mobile = World.Mobiles.Get(LocalSerial);

            int namewidth = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, _name);

            if (World.Party.Contains(LocalSerial))
            {
                Height = 60;
                Width = 120;

                if (CanBeSaved && _textBox == null)
                {
                    Add(new GameBorder(0, 0, 120, 60, 1 / 2));
                    Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });
                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, style: FontStyle.Cropped, hue: Notoriety.GetHue(mobile?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 60,
                        IsEditable = false,
                        CanMove = true,
                        //Text = "[* SELF *]"

                    });
                }
                else
                {
                    Add(new GameBorder(0, 0, 120, 60, 1 / 2));
                    Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });
                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, style: FontStyle.Cropped | FontStyle.BlackBorder, hue: Notoriety.GetHue(mobile?.NotorietyFlag ?? NotorietyFlag.Gray))
                    {
                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 60,
                        IsEditable = false,
                        CanMove = true,
                        //Text = _name
                    });

                }

                Add(_hpLineRed = new GumpPic(6, 27, LINE_RED, 0));
                Add(_manaLineRed = new GumpPic(6, 36, LINE_RED, 0));
                Add(_stamLineRed = new GumpPic(6, 45, LINE_RED, 0));

                Add(_bars[0] = new GumpPicWithWidth(6, 27, LINE_BLUE, 0, 108));
                Add(_bars[1] = new GumpPicWithWidth(6, 36, LINE_BLUE, 0, 108));
                Add(_bars[2] = new GumpPicWithWidth(6, 45, LINE_BLUE, 0, 108));

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

                    Add(_hpLineRed = new GumpPic(6, 27, LINE_RED, 0));
                    Add(new GumpPic(6, 36, LINE_RED, 0));
                    Add(new GumpPic(6, 45, LINE_RED, 0));

                    Add(_bars[0] = new GumpPicWithWidth(6, 27, LINE_BLUE, 0, 108));
                    Add(_bars[1] = new GumpPicWithWidth(6, 36, LINE_BLUE, 0, 108));
                    Add(_bars[2] = new GumpPicWithWidth(6, 45, LINE_BLUE, 0, 108));



                }
                else
                {
                    Hue textColor = 0x0386;
                    Hue hitsColor = 0x0386;

                    if (mobile != null)
                    {
                        hitsColor = 0;
                        _canChangeName = mobile.IsRenamable;

                        if (_canChangeName)
                            textColor = 0x000E;
                    }

                    Hue barColor = mobile == null || mobile == World.Player || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue)0 : Notoriety.GetHue(mobile.NotorietyFlag);

                    Height = 36;
                    Width = 120;

                    Add(new GameBorder(0, 0, 120, 36, 1 / 2));
                    Add(_background = new AlphaBlendControl(0.2f) { Width = Width, Height = Height });
                    Add(_hpLineRed = new GumpPic(6, 21, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(6, 21, LINE_BLUE, 0, 108));

                    Add(_textBox = new TextBox(3, width: 120, isunicode: true, hue: Notoriety.GetHue(mobile.NotorietyFlag), style: FontStyle.Fixed)
                    {

                        X = 0,
                        Y = 0,
                        Width = 120,
                        Height = 36,
                        IsEditable = true,
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
            bool inparty = World.Party.Contains(LocalSerial);
            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;
            color = Textures.GetTexture(Color.Black);

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

            Width = _renderedText.MaxWidth;
            Height = _renderedText.MaxHeight;

            Point p = Mouse.LDroppedOffset;

            if (Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) >= 1) return;
            if (TargetManager.IsTargeting)
            {
                TargetManager.TargetGameObject(World.Get(LocalSerial));
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName && !_targetBroke)
            {
                _renderedText.IsEditable = true;
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
                _renderedText.IsEditable = false;
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
                _renderedText.IsEditable = false;
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
    }
}
