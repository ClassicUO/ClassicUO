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

using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static
    {
        private readonly int _canBeTransparent;
        private readonly bool _isFoliage, _isPartialHue;

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

        public override bool Draw(Batcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            if (Texture == null || Texture.IsDisposed || _oldGraphic != Graphic)
            {
                _oldGraphic = Graphic;

                ArtTexture texture = FileManager.Art.GetTexture(Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;

                FrameInfo.X = (Texture.Width >> 1) - 22 - texture.ImageRectangle.X;
                FrameInfo.Y = Texture.Height - 44 - texture.ImageRectangle.Y;
            }

            if (_isFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    if (AlphaHue != 76)
                        ProcessAlpha(76);
                }
                else
                {
                    if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }


            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
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
            {
                ushort hue = Hue;
                bool isPartial = _isPartialHue;

                if (Engine.Profile.Current.HighlightGameObjects && IsSelected)
                {
                    hue = 0x0023;
                    isPartial = false;
                }

                ShaderHuesTraslator.GetHueVector(ref HueVector, hue, isPartial, 0);
            }

            Engine.DebugInfo.StaticsRendered++;
            base.Draw(batcher, posX, posY);

            //SpriteRenderer.DrawStaticArt(Graphic, Hue, (int) position.X, (int) position.Y);

            if (ItemData.IsLight)
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }


        public override void Select(int x, int y)
        {
            if (SelectedObject.IsPointInStatic(Graphic, x - Bounds.X, y - Bounds.Y)) SelectedObject.Object = this;
        }

        //public override void MousePick(MouseOverList list, SpriteVertex[] vertex, bool istransparent)
        //{



        //    //int x = list.MousePosition.X - (int) vertex[0].Position.X;
        //    //int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
        //    //if (!istransparent && Texture.Contains(x, y))
        //    //    list.Add(this, vertex[0].Position);
        //}
    }
}