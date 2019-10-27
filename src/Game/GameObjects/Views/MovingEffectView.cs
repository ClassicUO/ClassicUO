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
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class MovingEffect
    {
        private Graphic _displayedGraphic = Graphic.INVALID;


        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            ResetHueVector();

            if (AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = AnimationGraphic;
                Texture = FileManager.Art.GetTexture(AnimationGraphic);
                Bounds.X = 0;
                Bounds.Y = 0;
                Bounds.Width = Texture.Width;
                Bounds.Height = Texture.Height;
            }

            posX += (int) Offset.X;
            posY = (int) ( posY + Offset.Y + Offset.Z);

            //ArtTexture texture = (ArtTexture) Texture;

            //posX += (texture.Width >> 1) - 22 - texture.ImageRectangle.X;
            //posY += (texture.Height >> 0) - 44 - texture.ImageRectangle.Y;

            //posX += 22;
            //posY += 22;
            //Bounds.X = (int) -Offset.X + 22;
            //Bounds.Y = (int) (Offset.Z - Offset.Y) + 22;
            //Rotation = AngleToTarget;

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
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
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue);

            Engine.DebugInfo.EffectsRendered++;

            //base.Draw(batcher, posX, posY);
            if (FixedDir)
                batcher.DrawSprite(Texture, posX, posY, false, ref HueVector);
            else
                //batcher.Draw2D(Texture, posX, posY, Texture.Width, Texture.Height, 
                //               posX + texture.ImageRectangle.X, posY + texture.ImageRectangle.Y, texture.ImageRectangle.Width, texture.ImageRectangle.Height,
                //               ref HueVector,
                //               AngleToTarget);
                batcher.DrawSpriteRotated(Texture, posX, posY, ref HueVector, AngleToTarget);

            Select(posX, posY);
            Texture.Ticks = Engine.Ticks;

            ref readonly StaticTiles data = ref FileManager.TileData.StaticData[_displayedGraphic];

            if (data.IsLight && (Source is Item || Source is Static || Source is Multi))
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}