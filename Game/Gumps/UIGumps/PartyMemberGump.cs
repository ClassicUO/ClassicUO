using System;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class PartyMemberGump : Gump
    {
        private readonly Texture2D _healthBar;
        private readonly Label _healthLabel;
        private readonly Texture2D _manaBar;
        private readonly Label _manaLabel;
        private readonly float _maxBarWidth;
        private readonly PartyMember _partyMember;
        private readonly Button _pinButton;
        private readonly Texture2D _staminaBar;
        private readonly Label _staminaLabel;
        private float _currentHealthBarLength;
        private float _currentManaBarLength;
        private float _currentStaminaBarLength;
        private bool _isPinned;

        public PartyMemberGump(PartyMember member) : base(member.Serial, 0)
        {
            _isPinned = false;
            _partyMember = member;
            _maxBarWidth = 120.00f;
            _currentHealthBarLength = _maxBarWidth;
            _currentStaminaBarLength = _maxBarWidth;
            _currentManaBarLength = _maxBarWidth;
            _healthLabel = new Label("0/0", true, 1151, font: 3) {X = 70, Y = 24};
            _staminaLabel = new Label("0/0", true, 1151, font: 3) {X = 70, Y = 39};
            _manaLabel = new Label("0/0", true, 1151, font: 3) {X = 70, Y = 54};
            _healthBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _staminaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _manaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _healthBar.SetData(new[] {Color.DarkRed});
            _staminaBar.SetData(new[] {Color.Orange});
            _manaBar.SetData(new[] {Color.DarkBlue});
            CanMove = true;
            AcceptMouseInput = true;
            X = 300;
            Y = 300;

            //AddChildren(new GumpPicTiled(0, 0, 150, 100, 0x0A40) { IsTransparent = true });
            AddChildren(new Label(_partyMember.Name, true, 1153, font: 3) {X = 5, Y = 5});
            AddChildren(_pinButton = new Button((int) Buttons.Pin, 0x2330, 0x2331, 0x2331) {X = 150, Y = 5, ButtonAction = ButtonAction.Activate});
            AddChildren(new Button((int) Buttons.Heal, 0x938, 0x2C93, 0x2C94) {X = 5, Y = 30, ButtonAction = ButtonAction.Activate});
            AddChildren(new Button((int) Buttons.Cure, 0x939, 0x2C89, 0x2C8A) {X = 5, Y = 45, ButtonAction = ButtonAction.Activate});
            AddChildren(new Button((int) Buttons.Bandage, 0x93A, 0x2C89, 0x2C8A) {X = 5, Y = 60, ButtonAction = ButtonAction.Activate});
            //AddChildren(new GumpPic(65, 30, 0x7582, 0));
            //Bar Borders
            AddChildren(new FrameBorder(22, 26, 124, 18, Color.DarkGray));
            AddChildren(new FrameBorder(22, 41, 124, 18, Color.DarkGray));
            AddChildren(new FrameBorder(22, 56, 124, 18, Color.DarkGray));
            //Bar letters
            AddChildren(_healthLabel);
            AddChildren(_staminaLabel);
            AddChildren(_manaLabel);

            //_partyMember.Mobile.HitsChanged += OnHitsChanged;
            //_partyMember.Mobile.StaminaChanged +=OnStaminaChanged;
            //_partyMember.Mobile.ManaChanged +=OnManaChanged;
            //_partyMember.Mobile.PositionChanged += Mobile_PositionChanged;
            MouseDown += PartyMemberGump_MouseDown;
        }

        private void PartyMemberGump_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.ButtonState == ButtonState.Pressed && e.Button == MouseButton.Left)
                foreach (PartyMemberGump partyMemberGump in UIManager.GetByLocalSerial<PartyMemberGump>().Children)
                    Console.WriteLine("gfsdfgdfgd");
        }

        //private void OnHitsChanged(object sender, EventArgs e)
        //{
        //    _currentHealthBarLength = _partyMember.Mobile.Hits / _partyMember.Mobile.HitsMax * _maxBarWidth;
        //}
        //private void OnStaminaChanged(object sender, EventArgs e)
        //{
        //    _currentStaminaBarLength = _partyMember.Mobile.Stamina / _partyMember.Mobile.StaminaMax * _maxBarWidth;
        //}
        //private void OnManaChanged(object sender, EventArgs e)
        //{
        //    _currentManaBarLength = _partyMember.Mobile.Mana / _partyMember.Mobile.ManaMax * _maxBarWidth;
        //}
        //private void Mobile_PositionChanged(object sender, EventArgs e)
        //{
        //    if (World.Player.Position.X > _partyMember.Mobile.Position.X)
        //    {

        //    }

        //}

        public override void Update(double totalMS, double frameMS)
        {
            if (_partyMember.Mobile != null)
            {
                //Sets current bar length
                _currentHealthBarLength = _partyMember.Mobile.Hits * _maxBarWidth / _partyMember.Mobile.HitsMax;
                _currentStaminaBarLength = _partyMember.Mobile.Stamina * _maxBarWidth / _partyMember.Mobile.StaminaMax;
                _currentManaBarLength = _partyMember.Mobile.Mana * _maxBarWidth / _partyMember.Mobile.ManaMax;
                //Updates current labels
                _healthLabel.Text = concatLabel(_partyMember.Mobile.Hits.ToString(), _partyMember.Mobile.HitsMax.ToString());
                _staminaLabel.Text = concatLabel(_partyMember.Mobile.Stamina.ToString(), _partyMember.Mobile.StaminaMax.ToString());
                _manaLabel.Text = concatLabel(_partyMember.Mobile.Mana.ToString(), _partyMember.Mobile.ManaMax.ToString());
                //Sets correct label location
                _healthLabel.X = 85 - _healthLabel.Width / 2;
                _staminaLabel.X = 85 - _staminaLabel.Width / 2;
                _manaLabel.X = 85 - _manaLabel.Width / 2;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            spriteBatch.Draw2D(_healthBar, new Rectangle(X + 25, Y + 30, (int) _currentHealthBarLength, 10), RenderExtentions.GetHueVector(0, true, 0.4f, true));
            spriteBatch.Draw2D(_staminaBar, new Rectangle(X + 25, Y + 46, (int) _currentStaminaBarLength, 10), RenderExtentions.GetHueVector(0, true, 0.4f, true));
            spriteBatch.Draw2D(_manaBar, new Rectangle(X + 25, Y + 61, (int) _currentManaBarLength, 10), RenderExtentions.GetHueVector(0, true, 0.2f, true));

            return base.Draw(spriteBatch, position);
        }

        public override void Dispose()
        {
            PartySystem.PartyMemberGumpStack.Remove(_partyMember);
            base.Dispose();
        }

        private string concatLabel(string a, string b)
        {
            return $"{a}/{b}";
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Heal:

                    //
                    break;
                case Buttons.Cure:

                    //
                    break;
                case Buttons.Bandage:

                    //
                    break;
                case Buttons.Pin:

                    if (!_isPinned)
                    {
                        _pinButton.ButtonGraphicNormal = 0x2331;
                        _isPinned = true;
                        CanMove = false;
                        CanCloseWithRightClick = false;
                    }
                    else
                    {
                        _pinButton.ButtonGraphicNormal = 0x2330;
                        _isPinned = false;
                        CanMove = true;
                        CanCloseWithRightClick = true;
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Heal = 1,
            Cure,
            Bandage,
            Pin
        }

        private enum Direction
        {
            North = 0x1194,
            NorthEast = 0x1195,
            East = 0x1196,
            SouthEast = 0x1197,
            South = 0x1198,
            SouthWest = 0x1199,
            West = 0x119A,
            NorthWest = 0x119B
        }
    }

    internal class FrameBorder : GumpControl
    {
        private readonly Texture2D _border;

        public FrameBorder(int x, int y, int w, int h, Color color)
        {
            AcceptMouseInput = true;
            CanMove = true;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _border = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _border.SetData(new[] {color});
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            spriteBatch.Draw2D(_border, new Rectangle((int) position.X, (int) position.Y + _border.Height, Width, _border.Height), Vector3.Zero);
            spriteBatch.Draw2D(_border, new Rectangle((int) position.X, (int) position.Y + Height - _border.Height * 2 + 1, Width + 1, _border.Height), Vector3.Zero);
            spriteBatch.Draw2D(_border, new Rectangle((int) position.X - _border.Width + 1, (int) position.Y + _border.Height, _border.Width, Height - _border.Height * 2), Vector3.Zero);
            spriteBatch.Draw2D(_border, new Rectangle((int) position.X + Width - _border.Width + 1, (int) position.Y + 2, _border.Width, Height - _border.Height * 2), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }
    }
}