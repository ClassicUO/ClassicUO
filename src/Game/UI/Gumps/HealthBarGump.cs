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

using System.IO;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

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

        private Serial _partyMemeberSerial;
        private Label _partyNameLabel;
        private TextBox _textBox;

        private bool _targetBroke = false;

        public HealthBarGump(Mobile mob) : this()
        {
            Mobile = mob;
            _name = Mobile.Name;
            _partyMemeberSerial = Mobile.Serial;

            _isDead = mob.IsDead;

            BuildGump();
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

        public Mobile Mobile { get; private set; }

        private Hue _barColor => Mobile == null || Mobile == World.Player || Mobile.NotorietyFlag == NotorietyFlag.Criminal || Mobile.NotorietyFlag == NotorietyFlag.Gray ? (Hue) 0 : Notoriety.GetHue(Mobile.NotorietyFlag);

        public void Update()
        {
            Clear();
            Mobile = World.Mobiles.Get(LocalSerial);
            BuildGump();
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            bool inparty = World.Party.GetPartyMember(_partyMemeberSerial) != null;

            Hue textColor = 0x0386;
            Hue hitsColor = 0x0386;

            Mobile = World.Mobiles.Get(LocalSerial);

            if (Mobile == null || Mobile.IsDestroyed)
            {
                if (Engine.Profile.Current.CloseHealthBarType == 1 ||
                    Engine.Profile.Current.CloseHealthBarType == 2 && World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000))
                {
                    Dispose();

                    return;
                }

                if (_isDead)
                    _isDead = false;

                if (!_outOfRange)
                {
                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;

                    _outOfRange = true;

                    if (inparty)
                    {
                        hitsColor = textColor = 912;

                        if (_partyNameLabel.Hue != textColor)
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

            if (Mobile != null && !Mobile.IsDestroyed)
            {
                if (!_isDead && Mobile.IsDead && Engine.Profile.Current.CloseHealthBarType == 2) // is dead
                {
                    Dispose();
                    return;
                }

                if (!Mobile.IsDead && _isDead) _isDead = false;

                if (_outOfRange)
                {
                    if (Mobile.HitsMax == 0)
                        GameActions.RequestMobileStatus(Mobile);

                    _outOfRange = false;

                    if (_name != Mobile.Name && !string.IsNullOrEmpty(Mobile.Name))
                        _name = Mobile.Name;

                    hitsColor = 0;

                    if (inparty)
                        textColor = _barColor;
                    else
                    {
                        _canChangeName = Mobile.IsRenamable;

                        if (_canChangeName)
                        {
                            textColor = 0x000E;
                            _textBox.MouseClick += TextBoxOnMouseClick;
                        }
                    }

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

                if (_background.Hue != _barColor)
                    _background.Hue = _barColor;

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

        public override void Dispose()
        {
            if (FileManager.ClientVersion >= ClientVersions.CV_200 && World.InGame && Mobile != null) NetClient.Socket.Send(new PCloseStatusBarGump(Mobile));

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
                Dispose();
        }

        private void BuildGump()
        {
            LocalSerial = _partyMemeberSerial;

            CanBeSaved = _partyMemeberSerial == World.Player;

            WantUpdateSize = false;

            if (World.Party.GetPartyMember(_partyMemeberSerial) != null)
            {
                Add(_background = new GumpPic(0, 0, BACKGROUND_NORMAL, 0)
                {
                    IsTransparent = true,
                    Alpha = 1
                });
                Width = 115;
                Height = 55;

                if (CanBeSaved)
                    Add(_partyNameLabel = new Label("[* SELF *]", false, 0x0386, font: 3) {X = 0, Y = -2});
                else
                {
                    Add(_partyNameLabel = new Label(_name, false, Notoriety.GetHue(Mobile?.NotorietyFlag ?? NotorietyFlag.Gray), 150, 3, FontStyle.Fixed)
                    {
                        X = 0,
                        Y = -2
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

                    if (Mobile != null)
                    {
                        hitsColor = 0;
                        _canChangeName = Mobile.IsRenamable;

                        if (_canChangeName)
                            textColor = 0x000E;
                    }

                    Add(_background = new GumpPic(0, 0, 0x0804, _barColor));
                    Add(_hpLineRed = new GumpPic(34, 38, LINE_RED, hitsColor));
                    Add(_bars[0] = new GumpPicWithWidth(34, 38, LINE_BLUE, 0, 0));

                    Width = _background.Texture.Width;
                    Height = _background.Texture.Height;

                    Add(_textBox = new TextBox(1, width: 120, isunicode: false, hue: textColor, style: FontStyle.Fixed)
                    {
                        X = 16,
                        Y = 14,
                        Width = 120,
                        Height = 30,
                        IsEditable = false,
                        AcceptMouseInput = _canChangeName,
                        AcceptKeyboardInput = _canChangeName,
                        SafeCharactersOnly = true,
                        Text = _name
                    });

                    if (_canChangeName)
                        _textBox.MouseClick += TextBoxOnMouseClick;
                }
            }
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

        private void TextBoxOnMouseClick(object sender, MouseEventArgs e)
        {
            if (TargetManager.IsTargeting)
            {
                TargetManager.TargetGameObject(Mobile);
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName && !_targetBroke)
            {
                _textBox.IsEditable = true;
                _textBox.SetKeyboardFocus();
            }

            _targetBroke = false;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (TargetManager.IsTargeting)
            {
                _targetBroke = true;
                TargetManager.TargetGameObject(Mobile);
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
            if (Mobile != null)
            {
                if (Mobile != World.Player)
                {
                    if (World.Player.InWarMode && World.Player != Mobile)
                        GameActions.Attack(Mobile);
                    else if (button == MouseButton.Left) GameActions.DoubleClick(Mobile);
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
                Engine.UI.SystemChat?.SetFocus();
                _textBox.IsEditable = false;
            }
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if ((TargetManager.IsTargeting || World.Player.InWarMode) && Mobile != null)
                SelectedObject.Object = Mobile;
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (Mobile != null && Mobile.IsSelected)
                SelectedObject.Object = null;
        }

        private enum ButtonParty
        {
            Heal1,
            Heal2
        }
    }
}