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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    internal class StaticView : View
    {
        private readonly bool _isFoliage, _isPartialHue;
        private float _alpha;
        private float _timeToProcessAlpha;
        private int _canBeTransparent;

        private Graphic _oldGraphic;

        public StaticView(Static st) : base(st)
        {
            _isFoliage = st.ItemData.IsFoliage;
            _isPartialHue = st.ItemData.IsPartialHue;
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(st.Graphic);

            if (st.ItemData.IsTranslucent)
                _alpha = 0.5f;

            if (st.ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (st.ItemData.IsRoof || (st.ItemData.IsSurface && st.ItemData.IsBackground) || st.ItemData.IsWall)
                _canBeTransparent = 1;
            else if (st.ItemData.Height == 5 && st.ItemData.IsSurface && !st.ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            Static st = (Static) GameObject;

            if (Texture == null || Texture.IsDisposed || _oldGraphic != GameObject.Graphic)
            {
                _oldGraphic = GameObject.Graphic;

                ArtTexture texture = FileManager.Art.GetTexture(GameObject.Graphic);
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

            if (Engine.Profile.Current.UseCircleOfTransparency)
            {
                int z = World.Player.Z + 5;

                if (!(GameObject.Z <= z - st.ItemData.Height || z < st.Z && (_canBeTransparent & 0xFF) == 0))
                {
                    int distanceMax = Engine.Profile.Current.CircleOfTransparencyRadius;
                    int distance = GameObject.Distance;

                    if (distance <= distanceMax)
                        _alpha = 1.0f - 1.0f / (distanceMax / (float) distance);
                    else if (_alpha != 0.0f)
                        _alpha = 0;
                }
            }

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && GameObject.Distance > World.ViewRange)
                HueVector = new Vector3(0x038E, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, _isPartialHue, _alpha, false);
            MessageOverHead(batcher, position, Bounds.Y - 44);
            Engine.DebugInfo.StaticsRendered++;
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