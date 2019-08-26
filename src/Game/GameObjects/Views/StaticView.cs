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

using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static
    {
        private int _canBeTransparent;
        private Graphic _oldGraphic;

        public bool CharacterIsBehindFoliage { get; set; }

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
                r = false;
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
                r = false;

            return r;
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            ResetHueVector();

            if (Texture == null || Texture.IsDisposed || _oldGraphic != Graphic)
            {
                _oldGraphic = Graphic;

                ArtTexture texture = FileManager.Art.GetTexture(Graphic);
                Texture = texture;
                Bounds.X = (Texture.Width >> 1) - 22;
                Bounds.Y = Texture.Height - 44;
                Bounds.Width = Texture.Width;
                Bounds.Height = texture.Height;

                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;

                FrameInfo.X = (Texture.Width >> 1) - 22 - texture.ImageRectangle.X;
                FrameInfo.Y = Texture.Height - 44 - texture.ImageRectangle.Y;
            }

            if (ItemData.IsFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    if (AlphaHue != Constants.FOLIAGE_ALPHA)
                        ProcessAlpha(Constants.FOLIAGE_ALPHA);
                }
                else
                {
                    if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }


            if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue, ItemData.IsPartialHue, 0);

            Engine.DebugInfo.StaticsRendered++;

            //if ((StaticFilters.IsTree(Graphic) || ItemData.IsFoliage || StaticFilters.IsRock(Graphic)))
            //{
            //    batcher.DrawSpriteShadow(Texture, posX - Bounds.X , posY - Bounds.Y /*- 10*/, false);
            //}

            if (base.Draw(batcher, posX, posY))
            {
                if (ItemData.IsLight)
                {
                    Engine.SceneManager.GetScene<GameScene>()
                          .AddLight(this, this, posX + 22, posY + 22);
                }

                return true;
            }

            return false;
        }


        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this || CharacterIsBehindFoliage)
                return;

            if (DrawTransparent)
            {
                int d = Distance;
                int maxD = Engine.Profile.Current.CircleOfTransparencyRadius + 1;

                if (d <= maxD && d <= 3)
                    return;
            }

            if (SelectedObject.IsPointInStatic(Texture, x - Bounds.X, y - Bounds.Y))
                SelectedObject.Object = this;
        }
    }
}