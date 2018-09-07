#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class GameTextView : View
    {
        private string _text;

        public GameTextView(GameText parent) : base(parent)
        {
            _text = parent.Text;

            Texture = GameTextRenderer.CreateTexture(GameObject); //TextureManager.GetOrCreateStringTextTexture(GameObject);
            //Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height, Texture.Width, Texture.Height);
        }

        public new GameText GameObject => (GameText)base.GameObject;


        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
            => DrawInternal(spriteBatch, position);


        public override bool DrawInternal(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            if (_text != GameObject.Text || Texture == null || Texture.IsDisposed)
            {
                if (Texture != null && !Texture.IsDisposed)
                {
                    Texture.Dispose();

                    if (string.IsNullOrEmpty(GameObject.Text))
                    {
                        GameObject.Dispose();
                        return false;
                    }
                }
                Texture = GameTextRenderer.CreateTexture(GameObject);
                //Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height, Texture.Width, Texture.Height);

                _text = GameObject.Text;
            }

            Texture.Ticks = World.Ticks;
            //HueVector = RenderExtentions.GetHueVector(0, GameObject.IsPartialHue, false, false);

            Rectangle src = new Rectangle();
            Rectangle dest = new Rectangle((int)position.X, (int)position.Y, GameObject.Width, GameObject.Height);

            src.X = 0; src.Y = 0;

            int maxX = src.X + dest.Width;
            if (maxX <= GameObject.Width)
                src.Width = dest.Width;
            else
            {
                src.Width = GameObject.Width - src.X;
                dest.Width = src.Width;
            }

            int maxY = src.Y + dest.Height;
            if (maxY <= GameObject.Height)
                src.Height = dest.Height;
            else
            {
                src.Height = GameObject.Height - src.Y;
                dest.Height = src.Height;
            }

            return GameObject.Parent == null ? Service.Get<SpriteBatchUI>().Draw2D(Texture, dest, src, Vector3.Zero) : base.Draw(spriteBatch, position);
        }


    }
}
