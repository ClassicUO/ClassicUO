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
    public class SpriteBatchUI : SpriteBatch3D
    {
        private readonly SpriteVertex[] _vertexBufferUI = new SpriteVertex[4];

        public SpriteBatchUI(GraphicsDevice device) : base(device)
        {
        }

        public bool Draw2D(Texture2D texture, Vector3 position, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = position.X;
            _vertexBufferUI[0].Position.Y = position.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;
            _vertexBufferUI[1].Position.X = position.X + texture.Width;
            _vertexBufferUI[1].Position.Y = position.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = position.X;
            _vertexBufferUI[2].Position.Y = position.Y + texture.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = position.X + texture.Width;
            _vertexBufferUI[3].Position.Y = position.Y + texture.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Vector3 position, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float) texture.Width;
            float maxX = (sourceRect.X + sourceRect.Width) / (float) texture.Width;
            float minY = sourceRect.Y / (float) texture.Height;
            float maxY = (sourceRect.Y + sourceRect.Height) / (float) texture.Height;
            _vertexBufferUI[0].Position.X = position.X;
            _vertexBufferUI[0].Position.Y = position.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;
            _vertexBufferUI[1].Position.X = position.X + sourceRect.Width;
            _vertexBufferUI[1].Position.Y = position.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = position.X;
            _vertexBufferUI[2].Position.Y = position.Y + sourceRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = position.X + sourceRect.Width;
            _vertexBufferUI[3].Position.Y = position.Y + sourceRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Rectangle destRect, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float) texture.Width, maxX = (sourceRect.X + sourceRect.Width) / (float) texture.Width;
            float minY = sourceRect.Y / (float) texture.Height, maxY = (sourceRect.Y + sourceRect.Height) / (float) texture.Height;
            _vertexBufferUI[0].Position.X = destRect.X;
            _vertexBufferUI[0].Position.Y = destRect.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;
            _vertexBufferUI[1].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[1].Position.Y = destRect.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = destRect.X;
            _vertexBufferUI[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = destRect.X;
            _vertexBufferUI[0].Position.Y = destRect.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;
            _vertexBufferUI[1].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[1].Position.Y = destRect.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = destRect.X;
            _vertexBufferUI[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2DTiled(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            int y = destRect.Y;
            int h = destRect.Height;
            Rectangle sRect;

            while (h > 0)
            {
                int x = destRect.X;
                int w = destRect.Width;

                if (h < texture.Height)
                    sRect = new Rectangle(0, 0, texture.Width, h);
                else
                    sRect = new Rectangle(0, 0, texture.Width, texture.Height);

                while (w > 0)
                {
                    if (w < texture.Width) sRect.Width = w;
                    Draw2D(texture, new Vector3(x, y, 0), sRect, hue);
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
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Right, rectangle.Y), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Y), hue);

            return true;
        }

        public bool DrawLine(Texture2D texture, Vector2 start, Vector2 end, Vector3 hue)
        {
            int offX = start.X == end.X ? 1 : 0;
            int offY = start.Y == end.Y ? 1 : 0;
            _vertexBufferUI[0].Position.X = start.X;
            _vertexBufferUI[0].Position.Y = start.Y;
            _vertexBufferUI[0].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[0].TextureCoordinate = new Vector3(0, 0, 0);
            _vertexBufferUI[1].Position.X = end.X + offX;
            _vertexBufferUI[1].Position.Y = start.Y + offY;
            _vertexBufferUI[1].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[1].TextureCoordinate = new Vector3(1, 0, 0);
            _vertexBufferUI[2].Position.X = start.X + offX;
            _vertexBufferUI[2].Position.Y = end.Y + offY;
            _vertexBufferUI[2].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[2].TextureCoordinate = new Vector3(0, 1, 0);
            _vertexBufferUI[3].Position.X = end.X;
            _vertexBufferUI[3].Position.Y = end.Y;
            _vertexBufferUI[3].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[3].TextureCoordinate = new Vector3(1, 1, 0);
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }
    }
}