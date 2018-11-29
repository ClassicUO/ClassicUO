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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Views
{
    public class AnimatedEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public AnimatedEffectView(AnimatedItemEffect effect) : base(effect)
        {
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;
            AnimatedItemEffect effect = (AnimatedItemEffect)GameObject;

            if (effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = effect.AnimationGraphic;
                Texture = Art.GetStaticTexture(effect.AnimationGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44, Texture.Width, Texture.Height);
            }

            Bounds.X = Texture.Width / 2 - 22 - (int)effect.Offset.X;
            Bounds.Y = Texture.Height - 44 + (int)(effect.Offset.Z - effect.Offset.Y);

            var flags = TileData.StaticData[_displayedGraphic].Flags;


            bool isPartial = TileData.IsPartialHue(flags);
            bool isTransparent = TileData.IsTransparent(flags);
            HueVector = ShaderHuesTraslator.GetHueVector(effect.Hue, isPartial, isTransparent ? .5f : 0, false);

            switch (effect.Blend)
            {
                case GraphicEffectBlendMode.Multiply:
                    spriteBatch.SetBlendMode(Blend.Zero, Blend.SourceColor);
                    base.Draw(spriteBatch, position, objectList);
                    spriteBatch.SetBlendMode(Blend.One, Blend.InverseSourceAlpha);
                    break;
                case GraphicEffectBlendMode.Screen:
                case GraphicEffectBlendMode.ScreenMore:
                    spriteBatch.SetBlendMode(Blend.One, Blend.One);
                    base.Draw(spriteBatch, position, objectList);
                    spriteBatch.SetBlendMode(Blend.One, Blend.InverseSourceAlpha);
                    break;
                case GraphicEffectBlendMode.ScreenLess:
                    spriteBatch.SetBlendMode(Blend.DestinationColor, Blend.InverseSourceAlpha);
                    base.Draw(spriteBatch, position, objectList);
                    spriteBatch.SetBlendMode(Blend.One, Blend.InverseSourceAlpha);
                    break;
                case GraphicEffectBlendMode.NormalHalfTransparent:
                    spriteBatch.SetBlendMode(Blend.DestinationColor, Blend.SourceColor);
                    base.Draw(spriteBatch, position, objectList);
                    spriteBatch.SetBlendMode(Blend.One, Blend.InverseSourceAlpha);
                    break;
                case GraphicEffectBlendMode.ShadowBlue:
                    spriteBatch.SetBlendMode(Blend.SourceColor, Blend.InverseSourceColor, BlendFunction.ReverseSubtract);
                    base.Draw(spriteBatch, position, objectList);
                    spriteBatch.SetBlendMode(Blend.One, Blend.InverseSourceAlpha);
                    break;
                default:
                    base.Draw(spriteBatch, position, objectList);
                    break;
            }


            return true;
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