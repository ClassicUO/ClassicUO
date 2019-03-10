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

using System;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal partial class TextOverhead
    {
        private readonly RenderedText _text;

        protected bool EdgeDetection { get; set; }

       

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDisposed)
            {
                return false;
            }

            Texture.Ticks = Engine.Ticks;

            if (IsSelected && _text.Hue != 0x0035)
            {
                _text.Hue = 0x0035;
                _text.CreateTexture();
                Texture = _text.Texture;
            }
            else if (!IsSelected && Hue != _text.Hue)
            {
                _text.Hue = Hue;
                _text.CreateTexture();
                Texture = _text.Texture;
            }


            HueVector = ShaderHuesTraslator.GetHueVector(0);

            if (EdgeDetection)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                int width = Texture.Width - Bounds.X;
                int height = Texture.Height - Bounds.Y;

                if (position.X < Bounds.X)
                    position.X = Bounds.X;
                else if (position.X > Engine.Profile.Current.GameWindowSize.X * gs.Scale - width)
                    position.X = Engine.Profile.Current.GameWindowSize.X * gs.Scale - width;

                if (position.Y - Bounds.Y < 0)
                    position.Y = Bounds.Y;
                else if (position.Y > Engine.Profile.Current.GameWindowSize.Y * gs.Scale - height)
                    position.Y = Engine.Profile.Current.GameWindowSize.Y * gs.Scale - height;
            }

            bool ok = base.Draw(batcher, position, objectList);


            //if (_edge == null)
            //{
            //    _edge = new Texture2D(batcher.GraphicsDevice, 1, 1);
            //    _edge.SetData(new Color[] { Color.LightBlue });
            //}

            //batcher.DrawRectangle(_edge, new Rectangle((int)position.X - Bounds.X, (int)position.Y - Bounds.Y, _text.Width, _text.Height), Vector3.Zero);

            return ok;
        }

        //private static Texture2D _edge;

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex, bool istransparent)
        {          
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;

            if (Texture.Contains(x, y))
                list.Add(this, vertex[0].Position);            
        }
    }
}