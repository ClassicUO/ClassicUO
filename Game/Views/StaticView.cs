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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class StaticView : View
    {
        private readonly bool _isFoliage;
        private bool _isProcessingAlpha;
        private float _alpha;
        private float _timeToProcessAlpha;

        public StaticView(Static st) : base(st)
        {
            _isFoliage = TileData.IsFoliage( st.ItemData.Flags);
            AllowedToDraw = !IsNoDrawable(st.Graphic) && !(_isFoliage && World.MapIndex == 0);
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;
            Static st = (Static) GameObject;

            if (st.Effect == null)
            {
                if (Texture == null || Texture.IsDisposed)
                {
                    ArtTexture texture = Art.GetStaticTexture(GameObject.Graphic);
                    Texture = texture;
                    Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                    FrameInfo.OffsetX = texture.ImageRectangle.X;
                    FrameInfo.OffsetY = texture.ImageRectangle.Y;
                    FrameInfo.Width = texture.ImageRectangle.Width;
                    FrameInfo.Height = texture.ImageRectangle.Height;
                }

                if (_isFoliage)
                {
                    bool check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y;

                    if (!check)
                    {
                        check = World.Player.Position.Y <= st.Position.Y && World.Player.Position.X <= st.Position.X + 1;

                        if (!check)
                            check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y + 1;
                    }

                    if (check)
                    {
                        Rectangle fol = Rectangle.Empty;
                        fol.X = (int)position.X - Bounds.X + 22;
                        fol.Y = (int)position.Y - Bounds.Y + 22;

                        fol.Width = FrameInfo.Width;
                        fol.Height = FrameInfo.Height;

                        if (fol.InRect(World.Player.View.GetOnScreenRectangle()))
                        {
                            if (_timeToProcessAlpha < CoreGame.Ticks)
                            {
                                _timeToProcessAlpha = CoreGame.Ticks + 50;
                                if (!_isProcessingAlpha)
                                {
                                    _alpha += .1f;
                                }

                                if (_alpha >= .6f)
                                {
                                    _isProcessingAlpha = true;
                                    _alpha = .6f;
                                }
                            }
                        }
                        else
                        {
                            if (_alpha != 0.0f && _isProcessingAlpha)
                            {
                                if (_timeToProcessAlpha < CoreGame.Ticks)
                                {
                                    _timeToProcessAlpha = CoreGame.Ticks + 50;


                                    if (_isProcessingAlpha)
                                        _alpha -= .1f;

                                    if (_alpha <= 0.0f)
                                    {
                                        _isProcessingAlpha = false;
                                        _alpha = 0;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_alpha != 0.0f && _isProcessingAlpha)
                        {
                            if (_timeToProcessAlpha < CoreGame.Ticks)
                            {
                                _timeToProcessAlpha = CoreGame.Ticks + 50;


                                if (_isProcessingAlpha)
                                    _alpha -= .1f;

                                if (_alpha <= 0.0f)
                                {
                                    _isProcessingAlpha = false;
                                    _alpha = 0;
                                }
                            }
                        }
                    }
                }

                HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, false, _alpha, false);
                MessageOverHead(spriteBatch, position, Bounds.Y);

                return base.Draw(spriteBatch, position, objectList);
            }

            return !st.Effect.IsDisposed && st.Effect.View.Draw(spriteBatch, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
            //if (Art.Contains(GameObject.Graphic, x, y))
            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }
    }
}