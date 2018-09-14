#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class AnimatedEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public AnimatedEffectView(AnimatedItemEffect effect) : base(effect)
        {
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList<GameObject> objectList)
        {
            if (((AnimatedItemEffect)GameObject).IsMoving)
            {
                if (PreDraw(position))
                    return true;
            }

            return DrawInternal(spriteBatch, position, objectList);
        }

        public override bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList<GameObject> objectList)
        {
            if (GameObject.IsDisposed)
                return false;

            AnimatedItemEffect effect = (AnimatedItemEffect)GameObject;
            if (effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                if (Texture != null && !Texture.IsDisposed)
                    Texture.Dispose();

                _displayedGraphic = effect.AnimationGraphic;
                Texture = IO.Resources.Art.GetStaticTexture(effect.AnimationGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4, Texture.Width, Texture.Height);
            }

            HueVector = RenderExtentions.GetHueVector(GameObject.Hue);

            return base.Draw(spriteBatch, position, objectList);
        }

        protected override void MousePick(MouseOverList<GameObject> list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;

            if (Texture.Contains(x, y)) // if (Art.Contains(GameObject.Graphic, x, y))
            {
                list.Add(GameObject, vertex[0].Position);
            }
        }
    }
}