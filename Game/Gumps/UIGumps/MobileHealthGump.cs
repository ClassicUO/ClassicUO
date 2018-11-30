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
using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class MobileHealthGump : Gump
    {
        private const float MAX_BAR_WIDTH = 100.0f;
        private readonly GumpPic _background;
        private readonly Texture2D _backgroundBar;
        private readonly Texture2D _healthBar;
        private readonly Texture2D _manaBar;
        private readonly Texture2D _staminaBar;
        private readonly TextBox _textboxName;
        private float _currentHealthBarLength;
        private float _currentManaBarLength;
        private float _currentStaminaBarLength;
        private bool _isOutRange;
        private bool _isYellowHits, _isPoisoned, _isNormal;
        private bool _renameEventActive;
        private Label _text;

        public MobileHealthGump(Mobile mobile, int x, int y) : base(mobile.Serial, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            Mobile = mobile;
            _currentHealthBarLength = MAX_BAR_WIDTH;
            _currentStaminaBarLength = MAX_BAR_WIDTH;
            _currentManaBarLength = MAX_BAR_WIDTH;
            _backgroundBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _healthBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _manaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _staminaBar = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);

            _backgroundBar.SetData(new[]
            {
                Color.Red
            });

            _healthBar.SetData(new[]
            {
                Color.SteelBlue
            });

            _manaBar.SetData(new[]
            {
                Color.DarkBlue
            });

            _staminaBar.SetData(new[]
            {
                Color.Orange
            });

            ///
            /// Render the gump for player
            /// 
            if (Mobile == World.Player)
            {
                AddChildren(_background = new GumpPic(0, 0, 0x0803, 0));
                AddChildren(new FrameBorder(38, 14, 100, 7, Color.DarkGray));
                AddChildren(new FrameBorder(38, 27, 100, 7, Color.DarkGray));
                AddChildren(new FrameBorder(38, 40, 100, 7, Color.DarkGray));
            }
            ///
            /// Render the gump for mobiles
            /// If mobile is renamable for example a pet, it registers an event to activate name change
            /// to print out its name.
            /// 
            else
            {
                AddChildren(_background = new GumpPic(0, 0, 0x0804, 0));

                AddChildren(_textboxName = new TextBox(1, 17, 190, 190, false, FontStyle.None, 0x0386)
                {
                    X = 17, Y = 16, Width = 190, Height = 25
                });
                _textboxName.SetText(Mobile.Name);
                _textboxName.IsEditable = false;
                UIManager.KeyboardFocusControl = null;
                AddChildren(new FrameBorder(38, 40, 100, 7, Color.DarkGray));
            }

            ///
            /// Register events
            /// 
            Mobile.HitsChanged += MobileOnHitsChanged;
            Mobile.ManaChanged += MobileOnManaChanged;
            Mobile.StaminaChanged += MobileOnStaminaChanged;

            if (_textboxName != null)
                _textboxName.AcceptMouseInput = false;
            MobileOnHitsChanged(null, EventArgs.Empty);
            MobileOnManaChanged(null, EventArgs.Empty);
            MobileOnStaminaChanged(null, EventArgs.Empty);
        }

        public Mobile Mobile { get; }

        private void TextboxNameOnMouseClick(object sender, MouseEventArgs e)
        {
            _textboxName.IsEditable = true;
        }

        /// <summary>
        ///     Event consumes return key for textbox input (name change)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN && _textboxName.IsEditable)
            {
                GameActions.Rename(Mobile, _textboxName.Text);
                _textboxName.IsEditable = false;
                UIManager.KeyboardFocusControl = null;
            }

            base.OnKeyDown(key, mod);
        }

        /// <summary>
        ///     Eventhandler for changing hit points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnHitsChanged(object sender, EventArgs e)
        {
            _currentHealthBarLength = Mobile.Hits * MAX_BAR_WIDTH / (Mobile.HitsMax == 0 ? 1 : Mobile.HitsMax);
        }

        /// <summary>
        ///     Eventhandler for changing mana points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnManaChanged(object sender, EventArgs e)
        {
            _currentManaBarLength = Mobile.Mana * MAX_BAR_WIDTH / (Mobile.ManaMax == 0 ? 1 : Mobile.ManaMax);
        }

        /// <summary>
        ///     Eventhandler for changing stamina points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MobileOnStaminaChanged(object sender, EventArgs e)
        {
            _currentStaminaBarLength = Mobile.Stamina * MAX_BAR_WIDTH / (Mobile.StaminaMax == 0 ? 1 : Mobile.StaminaMax);
        }

        /// <summary>
        ///     Methode updates graphics
        /// </summary>
        /// <param name="totalMS"></param>
        /// <param name="frameMS"></param>
        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (Mobile.IsRenamable && !_renameEventActive)
            {
                _textboxName.AcceptMouseInput = true;
                _renameEventActive = true;
                _textboxName.MouseClick -= TextboxNameOnMouseClick;
                _textboxName.MouseClick += TextboxNameOnMouseClick;
            }

            ///
            /// Checks if entity is player
            /// 
            if (Mobile == World.Player && Mobile.InWarMode)
                _background.Graphic = 0x0807;
            else
                _background.Graphic = 0x0803;

            ///
            /// Check if entity is mobile
            /// 
            if (Mobile != World.Player)
            {
                _background.Graphic = 0x0804;

                ///Checks if mobile is in range and sets its gump grey if not
                if (Mobile.Distance > World.ViewRange)
                {
                    if (!_isOutRange)
                    {
                        _background.Hue = 0x038E;

                        _healthBar.SetData(new[]
                        {
                            Color.DarkGray
                        });

                        _manaBar.SetData(new[]
                        {
                            Color.DarkGray
                        });

                        _staminaBar.SetData(new[]
                        {
                            Color.DarkGray
                        });
                        _isOutRange = true;
                    }
                }
                else
                {
                    if (_isOutRange)
                    {
                        _isOutRange = false;
                        MobileOnHitsChanged(null, EventArgs.Empty);
                        MobileOnManaChanged(null, EventArgs.Empty);
                        MobileOnStaminaChanged(null, EventArgs.Empty);

                        _healthBar.SetData(new[]
                        {
                            Color.SteelBlue
                        });
                    }

                    _background.Hue = Notoriety.GetHue(Mobile.NotorietyFlag);

                    if (Mobile.IsYellowHits && !_isYellowHits)
                    {
                        _healthBar.SetData(new[]
                        {
                            Color.Gold
                        });
                        _isYellowHits = true;
                        _isNormal = false;
                    }
                    else if (Mobile.IsPoisoned && !_isPoisoned)
                    {
                        _healthBar.SetData(new[]
                        {
                            Color.Green
                        });
                        _isPoisoned = true;
                        _isNormal = false;
                    }
                    else if (!Mobile.IsPoisoned && !Mobile.IsYellowHits && !_isNormal)
                    {
                        _healthBar.SetData(new[]
                        {
                            Color.SteelBlue
                        });
                        _isNormal = true;
                        _isYellowHits = false;
                        _isPoisoned = false;
                    }
                }
            }

            base.Update(totalMS, frameMS);
        }

        /// <summary>
        ///     Methode draws all the needed bars
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

            if (Mobile == World.Player)
            {
                ///Draw background bars
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 14, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 27, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
                ///Draw stat bars
                spriteBatch.Draw2D(_healthBar, new Rectangle(X + 38, Y + 14, (int) _currentHealthBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
                spriteBatch.Draw2D(_manaBar, new Rectangle(X + 38, Y + 27, (int) _currentManaBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
                spriteBatch.Draw2D(_staminaBar, new Rectangle(X + 38, Y + 40, (int) _currentStaminaBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
            }
            else
            {
                ///Draw background bars
                spriteBatch.Draw2D(_backgroundBar, new Rectangle(X + 38, Y + 40, (int) MAX_BAR_WIDTH, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.4f, true));
                ///Draw stat bars
                spriteBatch.Draw2D(_healthBar, new Rectangle(X + 38, Y + 40, (int) _currentHealthBarLength, 7), ShaderHuesTraslator.GetHueVector(0, true, 0.1f, true));
            }

            return true;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (Mobile == World.Player)
                {
                    switch (Service.Get<Configuration.Settings>().StatusGumpStyle.ToLower())
                    {
                        case "classic":
                            UIManager.Add(new StatusGumpClassic() { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                            break;
                        case "modern":
                            UIManager.Add(new StatusGumpModern() { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                            break;
                        case "outlands":
                            UIManager.Add(new StatusGumpOutlands() { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                            break;
                        default:
                            break;
                    }

                    Dispose();
                }
                else
                {
                    if (World.Player.InWarMode)
                    {
                        //attack
                    }
                    else
                        GameActions.DoubleClick(Mobile);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Disposes all events and removes the current gump from stack
        /// </summary>
        public override void Dispose()
        {
            Mobile.HitsChanged -= MobileOnHitsChanged;
            Mobile.ManaChanged -= MobileOnManaChanged;
            Mobile.StaminaChanged -= MobileOnStaminaChanged;

            if (_textboxName != null)
                _textboxName.MouseClick -= TextboxNameOnMouseClick;
            _backgroundBar.Dispose();
            _healthBar.Dispose();
            _manaBar.Dispose();
            _staminaBar.Dispose();
            Service.Get<SceneManager>().GetScene<GameScene>().MobileGumpStack.Remove(Mobile);
            base.Dispose();
        }
    }
}