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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    internal class MovingEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public MovingEffectView(MovingEffect effect) : base(effect)
        {
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList list)
        {
            if (GameObject.IsDisposed)
                return false;
            MovingEffect effect = (MovingEffect)GameObject;

            if (effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = effect.AnimationGraphic;
                Texture = Art.GetStaticTexture(effect.AnimationGraphic);
                Bounds = new Rectangle(0, 0, Texture.Width, Texture.Height);
            }

            Bounds.X = (int)-effect.Offset.X;
            Bounds.Y = (int)(effect.Offset.Z - effect.Offset.Y);
            Rotation = effect.AngleToTarget;
            HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue);

            return base.Draw(batcher, position, list);
        }
    }
}