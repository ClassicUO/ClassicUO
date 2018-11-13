using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using SDL2;



namespace ClassicUO.Game.Gumps.UIGumps
{

    class MobileHealthGump : Gump
    {
        private Mobile _mobile;
        private readonly Texture2D _backgroundBar;
        private readonly Texture2D _healthBar;
        private readonly Texture2D _staminaBar;
        private readonly Texture2D _manaBar;
        private readonly float _maxBarWidth;
        private float _currentHealthBarLength;
        private float _currentManaBarLength;
        private float _currentStaminaBarLength;
        private GumpPic _background;
        private Label _text;
        private TextBox _textboxName;
        private bool _renameEventActive;


        public MobileHealthGump(Mobile mobile, int x, int y)
            : base(mobile.Serial, 0)
        {

            X = x;
            Y = y;
            CanMove = true;
            _mobile = mobile;
            _maxBarWidth = 100.00f;
            _currentHealthBarLength = _maxBarWidth;
            _currentStaminaBarLength = _maxBarWidth;
            _currentManaBarLength = _maxBarWidth;
            _backgroundBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _healthBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _manaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _staminaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _backgroundBar.SetData(new[] { Color.Red });
            _healthBar.SetData(new[] { Color.SteelBlue });
            _manaBar.SetData(new[] { Color.DarkBlue });
            _staminaBar.SetData(new[] { Color.Orange });

            ///
            /// Render the gump for player
            /// 
            if (_mobile == World.Player)
            {
                AddChildren(_background = new GumpPic(0, 0, 0x0803, 0));
                AddChildren(new FrameBorder(37, 12, 101, 10, Color.DarkGray));
                AddChildren(new FrameBorder(37, 25, 101, 10, Color.DarkGray));
                AddChildren(new FrameBorder(37, 38, 101, 10, Color.DarkGray));

            }
            ///
            /// Render the gump for mobiles
            /// If mobile is renamable for example a pet, it registers an event to activate name change
            /// to print out its name.
            /// 
            else
            {
                AddChildren(_background = new GumpPic(0, 0, 0x0804, 0));
                AddChildren(_textboxName = new TextBox(1, 17, 190, 190, false, FontStyle.None, 0x0386) { X = 17, Y = 16, Width = 190, Height = 25 });
                _textboxName.SetText(_mobile.Name);
                _textboxName.IsEditable = false;
                UIManager.KeyboardFocusControl = null;
                AddChildren(new FrameBorder(37, 38, 101, 10, Color.DarkGray));
            }
            ///
            /// Register events
            /// 
            _mobile.HitsChanged += MobileOnHitsChanged;
            _mobile.ManaChanged += MobileOnManaChanged;
            _mobile.StaminaChanged += MobileOnStaminaChanged;


            _textboxName.AcceptMouseInput = false;

        }

        private void TextboxNameOnMouseClick(object sender, MouseEventArgs e)
        {
            _textboxName.IsEditable = true;
        }


        /// <summary>
        /// Event consumes return key for textbox input (name change)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN && _textboxName.IsEditable)
            {
                GameActions.Rename(_mobile, _textboxName.Text);
                _textboxName.IsEditable = false;
                UIManager.KeyboardFocusControl = null;


            }
            base.OnKeyDown(key, mod);
        }

        /// <summary>
        /// Eventhandler for changing hit points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnHitsChanged(object sender, EventArgs e)
        {
            _currentHealthBarLength = _mobile.Hits * _maxBarWidth / _mobile.HitsMax;
        }
        /// <summary>
        /// Eventhandler for changing mana points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnManaChanged(object sender, EventArgs e)
        {
            _currentManaBarLength = _mobile.Mana * _maxBarWidth / _mobile.ManaMax;
        }
        /// <summary>
        /// Eventhandler for changing stamina points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnStaminaChanged(object sender, EventArgs e)
        {
            _currentStaminaBarLength = _mobile.Stamina * _maxBarWidth / _mobile.StaminaMax;
        }

        /// <summary>
        /// Methode updates graphics
        /// </summary>
        /// <param name="totalMS"></param>
        /// <param name="frameMS"></param>
        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_mobile.IsRenamable && !_renameEventActive)
            {
                _textboxName.AcceptMouseInput = true;
                _renameEventActive = true;
                _textboxName.MouseClick -= TextboxNameOnMouseClick;
                _textboxName.MouseClick += TextboxNameOnMouseClick;
            }
            ///
            /// Checks if entity is player
            /// 
            if (_mobile == World.Player && _mobile.InWarMode)
            {
                _background.Graphic = 0x0807;
            }
            else
            {
                _background.Graphic = 0x0803;

            }
            ///
            /// Check if entity is mobile
            /// 
            if (_mobile != World.Player)
            {
                _background.Graphic = 0x0804;


                ///Checks if mobile is in range and sets its gump grey if not
                if (_mobile.Distance > World.ViewRange)
                {
                    _background.Hue = 0x038E;
                    _healthBar.SetData(new[] { Color.DarkGray });
                    _manaBar.SetData(new[] { Color.DarkGray });
                    _staminaBar.SetData(new[] { Color.DarkGray });
                }
                else
                {

                    //Check mobile's flag and set the bar's color
                    switch (_mobile.NotorietyFlag)
                    {
                        case NotorietyFlag.Invulnerable:
                            _background.Hue = 50; //default 50 : yellow
                            break;
                        case NotorietyFlag.Innocent:
                            _background.Hue = 190; //default 190 : blue
                            break;
                        case NotorietyFlag.Ally:
                            _background.Hue = 64; //default 64 : blue
                            break;
                        case NotorietyFlag.Murderer:
                            _background.Hue = 35; //default 190 : red
                            break;
                        default: //gray,enemy,criminal,unknown 
                            _background.Hue = 0;
                            break;

                    }



                    if (_mobile.IsYellowHits)
                    {
                        _healthBar.SetData(new[] { Color.Gold });
                    }
                    else if (_mobile.IsPoisoned)
                    {
                        _healthBar.SetData(new[] { Color.Green });
                    }
                    else if (!_mobile.IsPoisoned)
                    {
                        _healthBar.SetData(new[] { Color.SteelBlue });
                    }
                }
            }

            base.Update(totalMS, frameMS);
        }

        /// <summary>
        /// Methode draws all the needed bars 
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="position"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (IsDisposed)
                return false;

            base.Draw(spriteBatch, position);
            if (_mobile == World.Player)
            {
                ///Draw background bars
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 14, (int)_maxBarWidth, 7), RenderExtentions.GetHueVector(0, true, 0.4f, true));
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 27, (int)_maxBarWidth, 7), RenderExtentions.GetHueVector(0, true, 0.4f, true));
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int)_maxBarWidth, 7), RenderExtentions.GetHueVector(0, true, 0.4f, true));
                ///Draw stat bars
                spriteBatch.Draw2D(_healthBar, new Rectangle(X + 38, Y + 14, (int)_currentHealthBarLength, 7), RenderExtentions.GetHueVector(0, true, 0.1f, true));
                spriteBatch.Draw2D(_manaBar, new Rectangle(X + 38, Y + 27, (int)_currentManaBarLength, 7), RenderExtentions.GetHueVector(0, true, 0.1f, true));
                spriteBatch.Draw2D(_staminaBar, new Rectangle(X + 38, Y + 40, (int)_currentStaminaBarLength, 7), RenderExtentions.GetHueVector(0, true, 0.1f, true));

            }
            else
            {
                ///Draw background bars
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int)_maxBarWidth, 7), RenderExtentions.GetHueVector(0, true, 0.4f, true));
                ///Draw stat bars
                spriteBatch.Draw2D(_healthBar, new Rectangle(X + 38, Y + 40, (int)_currentHealthBarLength, 7), RenderExtentions.GetHueVector(0, true, 0.1f, true));

            }
            return true;
        }

        /// <summary>
        /// Disposes all events and removes the current gump from stack
        /// </summary>
        public override void Dispose()
        {
            _mobile.HitsChanged -= MobileOnHitsChanged;
            _mobile.ManaChanged -= MobileOnManaChanged;
            _mobile.StaminaChanged -= MobileOnStaminaChanged;

            if (_textboxName != null)
                _textboxName.MouseClick -= TextboxNameOnMouseClick;

            _backgroundBar.Dispose();
            _healthBar.Dispose();
            _manaBar.Dispose();
            _staminaBar.Dispose();

            Mobile.MobileGumpStack.Remove(_mobile);
            base.Dispose();
        }
    }
}
