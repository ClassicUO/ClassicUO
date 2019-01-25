#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    class HealthBarGump : Gump
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
        private TextBox _textBox;

        private Button _buttonHeal1, _buttonHeal2;
        private Label _partyNameLabel;

        private Serial _partyMemeberSerial;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;
        private int _oldHits, _oldStam, _oldMana;
        private bool _canChangeName;
        private bool _outOfRange;
        private string _name;

        private enum ButtonParty
        {
            Heal1,
            Heal2
        }

        public HealthBarGump(Mobile mob) : this()
        {
            Mobile = mob;
            _name = Mobile.Name;
            _partyMemeberSerial = Mobile.Serial;
            BuildGump();
        }

        public HealthBarGump() : base(0, 0)
        {
            CanMove = true;
        }

        public Mobile Mobile { get; private set; }

        private void BuildGump()
        {
            LocalSerial = _partyMemeberSerial;

            CanBeSaved = _partyMemeberSerial == World.Player;


            if (World.Party.GetPartyMember(_partyMemeberSerial) != null)
            {
                Add(_background = new GumpPic(0, 0, BACKGROUND_NORMAL, 0) { IsVisible = false });

                if (CanBeSaved)
                {
                    Add(_partyNameLabel = new Label("[* SELF *]", false, 0x0386, font: 3) {X = 16, Y = -2});
                }
                else
                {
                    Add(_partyNameLabel = new Label(_name, false, Notoriety.GetHue(Mobile.NotorietyFlag), 150, 1, FontStyle.Fixed)
                    {
                        X = 16, Y = -2
                    });
                }

                Add(_buttonHeal1 = new Button((int)ButtonParty.Heal1, 0x0938, 0x093A, 0x0938) { ButtonAction = ButtonAction.Activate, X = 16, Y = 20 });
                Add(_buttonHeal2 = new Button((int)ButtonParty.Heal2, 0x0939, 0x093A, 0x0939) { ButtonAction = ButtonAction.Activate, X = 16, Y = 33 });

                Add(_hpLineRed = new GumpPic(34, 20, LINE_RED_PARTY, 0));
                Add(_manaLineRed = new GumpPic(34, 33, LINE_RED_PARTY, 0));
                Add(_stamLineRed = new GumpPic(34, 45, LINE_RED_PARTY, 0));

                Add(_bars[0] = new GumpPicWithWidth(34, 20, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[1] = new GumpPicWithWidth(34, 33, LINE_BLUE_PARTY, 0, 96));
                Add(_bars[2] = new GumpPicWithWidth(34, 45, LINE_BLUE_PARTY, 0, 96));
            }
            else
            {
                if (CanBeSaved)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Add(_background = new GumpPic(0, 0, _oldWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL, 0));

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
                    Hue color = 0;
                    Hue textColor = 0x0386;
                    Hue hitsColor = 0x0386;

                    if (Mobile != null)
                    {
                        hitsColor = 0;
                        color = Notoriety.GetHue(Mobile.NotorietyFlag);

                        if (Mobile.NotorietyFlag == NotorietyFlag.Criminal || Mobile.NotorietyFlag == NotorietyFlag.Gray)
                            color = 0;

                        if (_canChangeName = Mobile.IsRenamable)
                        {
                            textColor = 0x000E;
                        }
                    }

                    Add(_background = new GumpPic(0, 0, 0x0804, color));
                    Add(_hpLineRed = new GumpPic(34, 38, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(34, 38, LINE_BLUE, 0, 0));

                    Add(_textBox = new TextBox(1, width: 150, isunicode: false, hue: textColor)
                    {
                        X = 16,
                        Y = 14,
                        Width = 150,
                        IsEditable = false,
                        AcceptMouseInput = _canChangeName,
                        AcceptKeyboardInput = _canChangeName,
                        Text = _name
                    });

                    if (_canChangeName)
                        _textBox.MouseClick += TextBoxOnMouseClick;
                }
            }
        }

        public void Update()
        {
            Clear();
            BuildGump();
        }

        private void TextBoxOnMouseClick(object sender, MouseEventArgs e)
        {
            if (_canChangeName)
                _textBox.IsEditable = true;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (TargetManager.IsTargeting)
            {
                if (TargetManager.TargetingState == TargetType.Position || TargetManager.TargetingState == TargetType.Object)
                {
                    TargetManager.TargetGameObject(Mobile);
                    Mouse.LastLeftButtonClickTime = 0;
                }
            }
            else if (_canChangeName)
                _textBox.IsEditable = false;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (Mobile != null)
            {
                if (Mobile != World.Player)
                {
                    if (World.Player.InWarMode && World.Player != Mobile)
                    {
                        GameActions.Attack(Mobile);
                    }
                    else
                    {
                        GameActions.DoubleClick(Mobile);
                    }
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
            if (Mobile == null)
                return;

            if ((key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) && _textBox.IsEditable)
            {
                GameActions.Rename(Mobile, _textBox.Text);
                _textBox.IsEditable = false;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            bool inparty = World.Party.GetPartyMember(_partyMemeberSerial) != null;

            Hue color = 0;
            Hue textColor = 0x0386;
            Hue hitsColor = 0x0386;

            if (Mobile == null || Mobile.IsDisposed)
            {
                Mobile = World.Mobiles.Get(LocalSerial);

                if (!_outOfRange && Mobile == null)
                {
                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;

                    _outOfRange = true;

                    if (inparty)
                    {
                        hitsColor = textColor = 912;

                        if(_partyNameLabel.Hue != textColor)
                            _partyNameLabel.Hue = textColor;

                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;

                        _bars[1].IsVisible = false;
                        _bars[2].IsVisible = false;
                    }
                    else
                    {
                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        if (_canChangeName)
                            _textBox.MouseClick -= TextBoxOnMouseClick;
                    }

                    if (_background.Hue != color)
                        _background.Hue = color;

                    if (_hpLineRed.Hue != hitsColor)
                    {
                        _hpLineRed.Hue = hitsColor;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                    }


                    _bars[0].IsVisible = false;                   
                }
            }

            if (Mobile != null && Mobile.HitsMax > 0)
            {
                if (_outOfRange)
                {
                    _outOfRange = false;

                    if (_name != Mobile.Name && !string.IsNullOrEmpty(Mobile.Name))
                        _name = Mobile.Name;

                    hitsColor = 0;
                    color = Notoriety.GetHue(Mobile.NotorietyFlag);

                    if (Mobile.NotorietyFlag == NotorietyFlag.Criminal || Mobile.NotorietyFlag == NotorietyFlag.Gray)
                        color = 0;

                    if (inparty)
                    {
                        textColor = color;
                    }
                    else if (_canChangeName = Mobile.IsRenamable)
                    {
                        textColor = 0x000E;

                        _textBox.MouseClick += TextBoxOnMouseClick;
                    }


                    if (_background.Hue != color)
                        _background.Hue = color;

                    if (inparty)
                    {
                        if (_partyNameLabel.Hue != textColor)
                            _partyNameLabel.Hue = textColor;

                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = true;

                        _bars[1].IsVisible = true;
                        _bars[2].IsVisible = true;
                    }
                    else
                    {
                        if (_textBox.Hue != textColor)
                            _textBox.Hue = textColor;

                        if (_textBox.Text != _name)
                            _textBox.Text = _name;
                    }

                    if (_hpLineRed.Hue != hitsColor)
                    {
                        _hpLineRed.Hue = hitsColor;

                        if (_manaLineRed != null && _stamLineRed != null)
                            _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                    }

                    _bars[0].IsVisible = true;
                }

               

                if (Mobile.IsPoisoned && !_poisoned)
                {
                    if (inparty)
                        _bars[0].Hue = 63;
                    else
                        _bars[0].Graphic = LINE_POISONED;

                    _poisoned = true;
                    _normalHits = false;
                }
                else if (Mobile.IsYellowHits && !_yellowHits)
                {
                    if (inparty)
                        _bars[0].Hue = 353;
                    else
                        _bars[0].Graphic = LINE_YELLOWHITS;
                    _yellowHits = true;
                    _normalHits = false;
                }
                else if (!_normalHits && !Mobile.IsPoisoned && !Mobile.IsYellowHits && (_poisoned || _yellowHits))
                {
                    if (inparty)
                        _bars[0].Hue = 0;
                    else
                        _bars[0].Graphic = LINE_BLUE;
                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;
                }

                int hits = CalculatePercents(Mobile.HitsMax, Mobile.Hits, inparty ? 96 : 109);

                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;
                    _oldHits = hits;
                }
            }


            if (CanBeSaved)
            {             
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    _background.Graphic = World.Player.InWarMode ? BACKGROUND_WAR : BACKGROUND_NORMAL;
                }

                int mana = CalculatePercents(World.Player.ManaMax, World.Player.Mana, inparty ? 96 : 109);
                int stam = CalculatePercents(World.Player.StaminaMax, World.Player.Stamina, inparty ? 96 : 109);

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
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if ((TargetManager.IsTargeting || World.Player.InWarMode) && Mobile != null)
            {
                Mobile.IsSelected = true;
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (Mobile != null && Mobile.IsSelected)
            {
                Mobile.IsSelected = false;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonParty) buttonID)
            {
                case ButtonParty.Heal1:
                    break;
                case ButtonParty.Heal2:

                    break;
            }

            Mouse.CancelDoubleClick = true;
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write(Mobile.Serial);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            _partyMemeberSerial = LocalSerial = reader.ReadUInt32();

            if (LocalSerial == World.Player)
            {
                Mobile = World.Player;
                _name = Mobile.Name;
                BuildGump();
            }
            else
            {
                Dispose();
            }
        }

        private static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = (current * 100) / max;

                if (max > 100)
                    max = 100;

                if (max > 1)
                    max = (maxValue * max) / 100;
            }

            return max;
        }
    }

    //internal class HealthBarGump : Gump
    //{
    //    private const float MAX_BAR_WIDTH = 100.0f;
    //    private GumpPic _background;
    //    //private Texture2D _backgroundBar;
    //    //private Texture2D _healthBar;
    //    //private Texture2D _manaBar;
    //    //private Texture2D _staminaBar;
    //    private TextBox _textboxName;
    //    private float _currentHealthBarLength;
    //    private float _currentManaBarLength;
    //    private float _currentStaminaBarLength;
    //    private bool _isOutRange;
    //    private bool _isYellowHits, _isPoisoned, _isNormal;
    //    private bool _renameEventActive;

    //    public HealthBarGump(Mobile mobile, int x, int y) : this()
    //    {
    //        X = x;
    //        Y = y;
    //        Mobile = mobile;
    //        BuildGump();
    //    }

    //    public HealthBarGump() : base(0, 0)
    //    {
    //        CanMove = true;
    //    }

    //    public Mobile Mobile { get; private set; }

    //    private void BuildGump()
    //    {
    //        LocalSerial = Mobile.Serial;
    //        CanBeSaved = Mobile == World.Player;
    //        _currentHealthBarLength = MAX_BAR_WIDTH;
    //        _currentStaminaBarLength = MAX_BAR_WIDTH;
    //        _currentManaBarLength = MAX_BAR_WIDTH;
    //        //_backgroundBar = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1);
    //        //_healthBar = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1);
    //        //_manaBar = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1);
    //        //_staminaBar = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1);

    //        //_backgroundBar.SetData(new[]
    //        //{
    //        //    Color.Red
    //        //});

    //        //_healthBar.SetData(new[]
    //        //{
    //        //    Color.SteelBlue
    //        //});

    //        //_manaBar.SetData(new[]
    //        //{
    //        //    Color.DarkBlue
    //        //});

    //        //_staminaBar.SetData(new[]
    //        //{
    //        //    Color.Orange
    //        //});

    //        ///
    //        /// Render the gump for player
    //        /// 
    //        if (Mobile == World.Player)
    //        {
    //            AddChildren(_background = new GumpPic(0, 0, 0x0803, 0));
    //            AddChildren(new FrameBorder(38, 14, 100, 7, Color.DarkGray));
    //            AddChildren(new FrameBorder(38, 27, 100, 7, Color.DarkGray));
    //            AddChildren(new FrameBorder(38, 40, 100, 7, Color.DarkGray));
    //        }
    //        ///
    //        /// Render the gump for mobiles
    //        /// If mobile is renamable for example a pet, it registers an event to activate name change
    //        /// to print out its name.
    //        /// 
    //        else
    //        {
    //            AddChildren(_background = new GumpPic(0, 0, 0x0804, 0));

    //            AddChildren(_textboxName = new TextBox(1, 17, 190, 190, false, FontStyle.None, 0x0386)
    //            {
    //                X = 17,
    //                Y = 16,
    //                Width = 190,
    //                Height = 25
    //            });
    //            _textboxName.SetText(Mobile.Name);
    //            _textboxName.IsEditable = false;
    //            Engine.UI.KeyboardFocusControl = null;
    //            AddChildren(new FrameBorder(38, 40, 100, 7, Color.DarkGray));
    //        }

    //        ///
    //        /// Register events
    //        /// 
    //        Mobile.HitsChanged += MobileOnHitsChanged;
    //        Mobile.ManaChanged += MobileOnManaChanged;
    //        Mobile.StaminaChanged += MobileOnStaminaChanged;

    //        if (_textboxName != null)
    //            _textboxName.AcceptMouseInput = false;
    //        MobileOnHitsChanged(null, EventArgs.Empty);
    //        MobileOnManaChanged(null, EventArgs.Empty);
    //        MobileOnStaminaChanged(null, EventArgs.Empty);
    //    }


    //    public override void Save(BinaryWriter writer)
    //    {
    //        base.Save(writer);
            
    //        writer.Write(Mobile.Serial);
    //    }

    //    public override void Restore(BinaryReader reader)
    //    {
    //        base.Restore(reader);
    //        LocalSerial = reader.ReadUInt32();

    //        if (LocalSerial == World.Player)
    //        {
    //            Mobile = World.Player;
    //            BuildGump();
    //        }
    //        else
    //        {
    //            Dispose();
    //        }
    //    }

    //    private void TextboxNameOnMouseClick(object sender, MouseEventArgs e)
    //    {
    //        _textboxName.IsEditable = true;
    //    }

    //    /// <summary>
    //    ///     Event consumes return key for textbox input (name change)
    //    /// </summary>
    //    /// <param name="key"></param>
    //    /// <param name="mod"></param>
    //    protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    //    {
    //        if (key == SDL.SDL_Keycode.SDLK_RETURN && _textboxName.IsEditable)
    //        {
    //            GameActions.Rename(Mobile, _textboxName.Text);
    //            _textboxName.IsEditable = false;
    //            Engine.UI.KeyboardFocusControl = null;
    //        }

    //        base.OnKeyDown(key, mod);
    //    }

    //    /// <summary>
    //    ///     Eventhandler for changing hit points
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MobileOnHitsChanged(object sender, EventArgs e)
    //    {
    //        _currentHealthBarLength = Mobile.Hits * MAX_BAR_WIDTH / (Mobile.HitsMax == 0 ? 1 : Mobile.HitsMax);
    //    }

    //    /// <summary>
    //    ///     Eventhandler for changing mana points
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MobileOnManaChanged(object sender, EventArgs e)
    //    {
    //        _currentManaBarLength = Mobile.Mana * MAX_BAR_WIDTH / (Mobile.ManaMax == 0 ? 1 : Mobile.ManaMax);
    //    }

    //    /// <summary>
    //    ///     Eventhandler for changing stamina points
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MobileOnStaminaChanged(object sender, EventArgs e)
    //    {
    //        _currentStaminaBarLength = Mobile.Stamina * MAX_BAR_WIDTH / (Mobile.StaminaMax == 0 ? 1 : Mobile.StaminaMax);
    //    }

    //    /// <summary>
    //    ///     Methode updates graphics
    //    /// </summary>
    //    /// <param name="totalMS"></param>
    //    /// <param name="frameMS"></param>
    //    public override void Update(double totalMS, double frameMS)
    //    {
    //        if (IsDisposed)
    //            return;

    //        if (Mobile.IsRenamable && !_renameEventActive)
    //        {
    //            _textboxName.AcceptMouseInput = true;
    //            _renameEventActive = true;
    //            _textboxName.MouseClick -= TextboxNameOnMouseClick;
    //            _textboxName.MouseClick += TextboxNameOnMouseClick;
    //        }

    //        ///
    //        /// Checks if entity is player
    //        /// 
    //        if (Mobile == World.Player && Mobile.InWarMode)
    //            _background.Graphic = 0x0807;
    //        else
    //            _background.Graphic = 0x0803;

    //        ///
    //        /// Check if entity is mobile
    //        /// 
    //        if (Mobile != World.Player)
    //        {
    //            _background.Graphic = 0x0804;

    //            ///Checks if mobile is in range and sets its gump grey if not
    //            if (Mobile.Distance > World.ViewRange)
    //            {
    //                if (!_isOutRange)
    //                {
    //                    _background.Hue = 0x038E;

    //                    //_healthBar.SetData(new[]
    //                    //{
    //                    //    Color.DarkGray
    //                    //});

    //                    //_manaBar.SetData(new[]
    //                    //{
    //                    //    Color.DarkGray
    //                    //});

    //                    //_staminaBar.SetData(new[]
    //                    //{
    //                    //    Color.DarkGray
    //                    //});
    //                    _isOutRange = true;
    //                }
    //            }
    //            else
    //            {
    //                if (_isOutRange)
    //                {
    //                    _isOutRange = false;
    //                    MobileOnHitsChanged(null, EventArgs.Empty);
    //                    MobileOnManaChanged(null, EventArgs.Empty);
    //                    MobileOnStaminaChanged(null, EventArgs.Empty);

    //                    //_healthBar.SetData(new[]
    //                    //{
    //                    //    Color.SteelBlue
    //                    //});
    //                }

    //                _background.Hue = Notoriety.GetHue(Mobile.NotorietyFlag);

    //                if (Mobile.IsYellowHits && !_isYellowHits)
    //                {
    //                    //_healthBar.SetData(new[]
    //                    //{
    //                    //    Color.Gold
    //                    //});
    //                    _isYellowHits = true;
    //                    _isNormal = false;
    //                }
    //                else if (Mobile.IsPoisoned && !_isPoisoned)
    //                {
    //                    //_healthBar.SetData(new[]
    //                    //{
    //                    //    Color.Green
    //                    //});
    //                    _isPoisoned = true;
    //                    _isNormal = false;
    //                }
    //                else if (!Mobile.IsPoisoned && !Mobile.IsYellowHits && !_isNormal)
    //                {
    //                    //_healthBar.SetData(new[]
    //                    //{
    //                    //    Color.SteelBlue
    //                    //});
    //                    _isNormal = true;
    //                    _isYellowHits = false;
    //                    _isPoisoned = false;
    //                }
    //            }
    //        }

    //        base.Update(totalMS, frameMS);
    //    }

    //    /// <summary>
    //    ///     Methode draws all the needed bars
    //    /// </summary>
    //    /// <param name="spriteBatch"></param>
    //    /// <param name="position"></param>
    //    /// <param name="hue"></param>
    //    /// <returns></returns>
    //    //public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
    //    //{
    //    //    if (IsDisposed)
    //    //        return false;
    //    //    base.Draw(batcher, position);

    //    //    if (Mobile == World.Player)
    //    //    {
    //    //        ///Draw background bars
    //    //        batcher.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 14, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
    //    //        batcher.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 27, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
    //    //        batcher.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
    //    //        ///Draw stat bars
    //    //        batcher.Draw2D(_healthBar, new Rectangle(X + 38, Y + 14, (int) _currentHealthBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
    //    //        batcher.Draw2D(_manaBar, new Rectangle(X + 38, Y + 27, (int) _currentManaBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
    //    //        batcher.Draw2D(_staminaBar, new Rectangle(X + 38, Y + 40, (int) _currentStaminaBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
    //    //    }
    //    //    else
    //    //    {
    //    //        ///Draw background bars
    //    //        batcher.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
    //    //        ///Draw stat bars
    //    //        batcher.Draw2D(_healthBar, new Rectangle(X + 38, Y + 40, (int) _currentHealthBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
    //    //    }

    //    //    return true;
    //    //}

    //    protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
    //    {
    //        if (button == MouseButton.Left)
    //        {
    //            if (Mobile == World.Player)
    //            {
    //                Engine.UI.Add(new StatusGump
    //                {
    //                    X = ScreenCoordinateX, Y = ScreenCoordinateY
    //                });
    //                Dispose();
    //            }
    //            else
    //            {
    //                if (World.Player.InWarMode)
    //                {
    //                    //attack
    //                    GameActions.Attack(Mobile);
    //                }
    //                else
    //                    GameActions.DoubleClick(Mobile);
    //            }

    //            return true;
    //        }

    //        return false;
    //    }

    //    /// <summary>
    //    ///     Disposes all events and removes the current gump from stack
    //    /// </summary>
    //    public override void Dispose()
    //    {
    //        if (Mobile != null)
    //        {
    //            Mobile.HitsChanged -= MobileOnHitsChanged;
    //            Mobile.ManaChanged -= MobileOnManaChanged;
    //            Mobile.StaminaChanged -= MobileOnStaminaChanged;
    //        }

    //        if (_textboxName != null)
    //            _textboxName.MouseClick -= TextboxNameOnMouseClick;
    //        //_backgroundBar?.Dispose();
    //        //_healthBar?.Dispose();
    //        //_manaBar?.Dispose();
    //        //_staminaBar?.Dispose();
    //        Engine.SceneManager.GetScene<GameScene>().MobileGumpStack.Remove(Mobile);
    //        base.Dispose();
    //    }
    //}
}