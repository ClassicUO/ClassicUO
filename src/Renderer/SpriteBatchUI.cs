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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    //public class SpriteBatchUI : SpriteBatch3D
    partial class Batcher2D
    {
        private readonly SpriteVertex[] _vertexBufferUI = new SpriteVertex[4];

        //public SpriteBatchUI(GraphicsDevice device) : base(device)
        //{
        //}

        public unsafe bool Draw2D(Texture2D texture, Point position, Vector3 hue)
        {
            fixed (SpriteVertex* ptr = _vertexBufferUI)
            {
                ptr[0].Position.X = position.X;
                ptr[0].Position.Y = position.Y;
                ptr[0].Position.Z = 0;
                ptr[0].Normal.X = 0;
                ptr[0].Normal.Y = 0;
                ptr[0].Normal.Z = 1;
                ptr[0].TextureCoordinate = Vector3.Zero;
                ptr[1].Position.X = position.X + texture.Width;
                ptr[1].Position.Y = position.Y;
                ptr[1].Position.Z = 0;
                ptr[1].Normal.X = 0;
                ptr[1].Normal.Y = 0;
                ptr[1].Normal.Z = 1;
                ptr[1].TextureCoordinate.X = 1;
                ptr[1].TextureCoordinate.Y = 0;
                ptr[1].TextureCoordinate.Z = 0;
                ptr[2].Position.X = position.X;
                ptr[2].Position.Y = position.Y + texture.Height;
                ptr[2].Position.Z = 0;
                ptr[2].Normal.X = 0;
                ptr[2].Normal.Y = 0;
                ptr[2].Normal.Z = 1;
                ptr[2].TextureCoordinate.X = 0;
                ptr[2].TextureCoordinate.Y = 1;
                ptr[2].TextureCoordinate.Z = 0;
                ptr[3].Position.X = position.X + texture.Width;
                ptr[3].Position.Y = position.Y + texture.Height;
                ptr[3].Position.Z = 0;
                ptr[3].Normal.X = 0;
                ptr[3].Normal.Y = 0;
                ptr[3].Normal.Z = 1;
                ptr[3].TextureCoordinate.X = 1;
                ptr[3].TextureCoordinate.Y = 1;
                ptr[3].TextureCoordinate.Z = 0;
                ptr[0].Hue = ptr[1].Hue = ptr[2].Hue = ptr[3].Hue = hue;
            }

            return DrawSprite(texture, _vertexBufferUI, Techniques.Hued);
        }

        public unsafe bool Draw2D(Texture2D texture, Point position, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float) texture.Width;
            float maxX = (sourceRect.X + sourceRect.Width) / (float) texture.Width;
            float minY = sourceRect.Y / (float) texture.Height;
            float maxY = (sourceRect.Y + sourceRect.Height) / (float) texture.Height;

            fixed (SpriteVertex* ptr = _vertexBufferUI)
            {
                ptr[0].Position.X = position.X;
                ptr[0].Position.Y = position.Y;
                ptr[0].Position.Z = 0;
                ptr[0].Normal.X = 0;
                ptr[0].Normal.Y = 0;
                ptr[0].Normal.Z = 1;
                ptr[0].TextureCoordinate.X = minX;
                ptr[0].TextureCoordinate.Y = minY;
                ptr[0].TextureCoordinate.Z = 0;
                ptr[1].Position.X = position.X + sourceRect.Width;
                ptr[1].Position.Y = position.Y;
                ptr[1].Position.Z = 0;
                ptr[1].Normal.X = 0;
                ptr[1].Normal.Y = 0;
                ptr[1].Normal.Z = 1;
                ptr[1].TextureCoordinate.X = maxX;
                ptr[1].TextureCoordinate.Y = minY;
                ptr[1].TextureCoordinate.Z = 0;
                ptr[2].Position.X = position.X;
                ptr[2].Position.Y = position.Y + sourceRect.Height;
                ptr[2].Position.Z = 0;
                ptr[2].Normal.X = 0;
                ptr[2].Normal.Y = 0;
                ptr[2].Normal.Z = 1;
                ptr[2].TextureCoordinate.X = minX;
                ptr[2].TextureCoordinate.Y = maxY;
                ptr[2].TextureCoordinate.Z = 0;
                ptr[3].Position.X = position.X + sourceRect.Width;
                ptr[3].Position.Y = position.Y + sourceRect.Height;
                ptr[3].Position.Z = 0;
                ptr[3].Normal.X = 0;
                ptr[3].Normal.Y = 0;
                ptr[3].Normal.Z = 1;
                ptr[3].TextureCoordinate.X = maxX;
                ptr[3].TextureCoordinate.Y = maxY;
                ptr[3].TextureCoordinate.Z = 0;
                ptr[0].Hue = ptr[1].Hue = ptr[2].Hue = ptr[3].Hue = hue;
            }

            return DrawSprite(texture, _vertexBufferUI, Techniques.Hued);
        }

        public unsafe bool Draw2D(Texture2D texture, Rectangle destRect, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float) texture.Width, maxX = (sourceRect.X + sourceRect.Width) / (float) texture.Width;
            float minY = sourceRect.Y / (float) texture.Height, maxY = (sourceRect.Y + sourceRect.Height) / (float) texture.Height;

            fixed (SpriteVertex* ptr = _vertexBufferUI)
            {
                ptr[0].Position.X = destRect.X;
                ptr[0].Position.Y = destRect.Y;
                ptr[0].Position.Z = 0;
                ptr[0].Normal.X = 0;
                ptr[0].Normal.Y = 0;
                ptr[0].Normal.Z = 1;
                ptr[0].TextureCoordinate.X = minX;
                ptr[0].TextureCoordinate.Y = minY;
                ptr[0].TextureCoordinate.Z = 0;
                ptr[1].Position.X = destRect.X + destRect.Width;
                ptr[1].Position.Y = destRect.Y;
                ptr[1].Position.Z = 0;
                ptr[1].Normal.X = 0;
                ptr[1].Normal.Y = 0;
                ptr[1].Normal.Z = 1;
                ptr[1].TextureCoordinate.X = maxX;
                ptr[1].TextureCoordinate.Y = minY;
                ptr[1].TextureCoordinate.Z = 0;
                ptr[2].Position.X = destRect.X;
                ptr[2].Position.Y = destRect.Y + destRect.Height;
                ptr[2].Position.Z = 0;
                ptr[2].Normal.X = 0;
                ptr[2].Normal.Y = 0;
                ptr[2].Normal.Z = 1;
                ptr[2].TextureCoordinate.X = minX;
                ptr[2].TextureCoordinate.Y = maxY;
                ptr[2].TextureCoordinate.Z = 0;
                ptr[3].Position.X = destRect.X + destRect.Width;
                ptr[3].Position.Y = destRect.Y + destRect.Height;
                ptr[3].Position.Z = 0;
                ptr[3].Normal.X = 0;
                ptr[3].Normal.Y = 0;
                ptr[3].Normal.Z = 1;
                ptr[3].TextureCoordinate.X = maxX;
                ptr[3].TextureCoordinate.Y = maxY;
                ptr[3].TextureCoordinate.Z = 0;
                ptr[0].Hue = ptr[1].Hue = ptr[2].Hue = ptr[3].Hue = hue;
            }

            return DrawSprite(texture, _vertexBufferUI, Techniques.Hued);
        }

        public unsafe bool Draw2D(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            fixed (SpriteVertex* ptr = _vertexBufferUI)
            {
                ptr[0].Position.X = destRect.X;
                ptr[0].Position.Y = destRect.Y;
                ptr[0].Position.Z = 0;
                ptr[0].Normal.X = 0;
                ptr[0].Normal.Y = 0;
                ptr[0].Normal.Z = 1;
                ptr[0].TextureCoordinate = Vector3.Zero;
                ptr[1].Position.X = destRect.X + destRect.Width;
                ptr[1].Position.Y = destRect.Y;
                ptr[1].Position.Z = 0;
                ptr[1].Normal.X = 0;
                ptr[1].Normal.Y = 0;
                ptr[1].Normal.Z = 1;
                ptr[1].TextureCoordinate.X = 1;
                ptr[1].TextureCoordinate.Y = 0;
                ptr[1].TextureCoordinate.Z = 0;
                ptr[2].Position.X = destRect.X;
                ptr[2].Position.Y = destRect.Y + destRect.Height;
                ptr[2].Position.Z = 0;
                ptr[2].Normal.X = 0;
                ptr[2].Normal.Y = 0;
                ptr[2].Normal.Z = 1;
                ptr[2].TextureCoordinate.X = 0;
                ptr[2].TextureCoordinate.Y = 1;
                ptr[2].TextureCoordinate.Z = 0;
                ptr[3].Position.X = destRect.X + destRect.Width;
                ptr[3].Position.Y = destRect.Y + destRect.Height;
                ptr[3].Position.Z = 0;
                ptr[3].Normal.X = 0;
                ptr[3].Normal.Y = 0;
                ptr[3].Normal.Z = 1;
                ptr[3].TextureCoordinate.X = 1;
                ptr[3].TextureCoordinate.Y = 1;
                ptr[3].TextureCoordinate.Z = 0;
                ptr[0].Hue = ptr[1].Hue = ptr[2].Hue = ptr[3].Hue = hue;
            }

            return DrawSprite(texture, _vertexBufferUI, Techniques.Hued);
        }

        public bool Draw2DTiled(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            /* float drawCountX = destRect.Width / (float)texture.Width;
            float drawCountY = destRect.Height / (float)texture.Height;

            fixed (SpriteVertex* ptr = _vertexBufferUI)
            {
                ptr[0].Position.X = destRect.X;
                ptr[0].Position.Y = destRect.Y;
                ptr[0].Position.Z = 0;
                ptr[0].Normal.X = 0;
                ptr[0].Normal.Y = 0;
                ptr[0].Normal.Z = 1;
                ptr[0].TextureCoordinate = Vector3.Zero;

                ptr[1].Position.X = destRect.Right;
                ptr[1].Position.Y = destRect.Y;
                ptr[1].Position.Z = 0;
                ptr[1].Normal.X = 0;
                ptr[1].Normal.Y = 0;
                ptr[1].Normal.Z = 1;
                ptr[1].TextureCoordinate.X = drawCountX;
                ptr[1].TextureCoordinate.Y = 0;
                ptr[1].TextureCoordinate.Z = 0;

                ptr[2].Position.X = destRect.X;
                ptr[2].Position.Y = destRect.Bottom;
                ptr[2].Position.Z = 0;
                ptr[2].Normal.X = 0;
                ptr[2].Normal.Y = 0;
                ptr[2].Normal.Z = 1;
                ptr[2].TextureCoordinate.X = 0;
                ptr[2].TextureCoordinate.Y = drawCountY;
                ptr[2].TextureCoordinate.Z = 0;

                ptr[3].Position.X = destRect.Right;
                ptr[3].Position.Y = destRect.Bottom;
                ptr[3].Position.Z = 0;
                ptr[3].Normal.X = 0;
                ptr[3].Normal.Y = 0;
                ptr[3].Normal.Z = 1;
                ptr[3].TextureCoordinate.X = drawCountX;
                ptr[3].TextureCoordinate.Y = drawCountY;
                ptr[3].TextureCoordinate.Z = 0;
                ptr[0].Hue = ptr[1].Hue = ptr[2].Hue = ptr[3].Hue = hue;
            }

            return DrawSprite(texture, _vertexBufferUI); */
            int y = destRect.Y;
            int h = destRect.Height;

            while (h > 0)
            {
                int x = destRect.X;
                int w = destRect.Width;
                Rectangle sRect = new Rectangle(0, 0, texture.Width, h < texture.Height ? h : texture.Height);

                while (w > 0)
                {
                    if (w < texture.Width)
                        sRect.Width = w;
                    Draw2D(texture, new Point(x, y), sRect, hue);
                    w -= texture.Width;
                    x += texture.Width;
                }

                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }

        public bool DrawRectangle(Texture2D texture, Rectangle rectangle, Vector3 hue)
        {
            Draw2D(texture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), hue);
            Draw2D(texture, new Rectangle(rectangle.Right, rectangle.Y, 1, rectangle.Height), hue);
            Draw2D(texture, new Rectangle(rectangle.X, rectangle.Bottom, rectangle.Width, 1), hue);
            Draw2D(texture, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), hue);
            return true;
        }

    }
}