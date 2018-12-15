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
        private float _alpha;
        private float _timeToProcessAlpha;

        public StaticView(Static st) : base(st)
        {
            _isFoliage = TileData.IsFoliage( st.ItemData.Flags);
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(st.Graphic);

            if (TileData.IsTranslucent(st.ItemData.Flags))
                _alpha = 0.5f;
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            Static st = (Static) GameObject;
          
            if (Texture == null || Texture.IsDisposed)
            {
                ArtTexture texture = Art.GetStaticTexture(GameObject.Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.X = texture.ImageRectangle.X;
                FrameInfo.Y = texture.ImageRectangle.Y;
                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;
            }

            if (_isFoliage)
            {
                bool check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y;
                bool isnrect = false;

                if (!check)
                {
                    check = World.Player.Position.Y <= st.Position.Y && World.Player.Position.X <= st.Position.X + 1;

                    if (!check)
                        check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y + 1;
                }

                if (check)
                {
                    Rectangle fol = Rectangle.Empty;
                    fol.X = (int) position.X - Bounds.X + 22;
                    fol.Y = (int) position.Y - Bounds.Y + 22;

                    fol.Width = FrameInfo.Width;
                    fol.Height = FrameInfo.Height;

                    if (fol.InRect(World.Player.View.GetOnScreenRectangle()))
                    {
                        isnrect = true;

                        if (_timeToProcessAlpha < Engine.Ticks)
                        {
                            _timeToProcessAlpha = Engine.Ticks + Constants.ALPHA_TIME;

                            _alpha += .1f;

                            if (_alpha >= Constants.FOLIAGE_ALPHA)
                            {
                                _alpha = Constants.FOLIAGE_ALPHA;
                            }
                        }
                    }
                }

                if (_alpha > 0.0f && !isnrect)
                {
                    if (_timeToProcessAlpha < Engine.Ticks)
                    {
                        _timeToProcessAlpha = Engine.Ticks + Constants.ALPHA_TIME;

                        _alpha -= .1f;

                        if (_alpha < 0.0f)
                            _alpha = 0;
                    }
                }
            }

            HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, false, _alpha, false);
            MessageOverHead(batcher, position, Bounds.Y - 44);

            return base.Draw(batcher, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }
    }
}