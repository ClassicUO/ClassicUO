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

using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class LightningEffect
    {
        private static readonly Point[] _offsets =
        {
            new Point(48, 0), new Point(68, 0), new Point(92, 0), new Point(72, 0), new Point(48, 0), new Point(56, 0), new Point(76, 0), new Point(76, 0), new Point(92, 0), new Point(80, 0)
        };
        private Graphic _displayed = Graphic.INVALID;


        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            ResetHueVector();

            if (AnimationGraphic != _displayed || Texture == null || Texture.IsDisposed)
            {
                _displayed = AnimationGraphic;

                if (_displayed > 0x4E29)
                    return false;

                Texture = FileManager.Gumps.GetTexture(_displayed);
                Point offset = _offsets[_displayed - 20000];
                Bounds = new Rectangle(offset.X, Texture.Height - 33 + offset.Y, Texture.Width, Texture.Height);
            }

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

            return base.Draw(batcher, posX, posY);
        }
    }
}