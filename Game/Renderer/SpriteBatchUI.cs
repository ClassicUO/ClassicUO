using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer
{
    public class SpriteBatchUI : SpriteBatch3D
    {
        private readonly SpriteVertex[] _vertexBuffer = new SpriteVertex[4];

        public SpriteBatchUI(in Microsoft.Xna.Framework.Game game) : base(game)
        {

        }


        public bool Draw2D(in Texture2D texture, in Vector3 position, in Vector3 hue)
        {
            _vertexBuffer[0].Position.X = position.X;
            _vertexBuffer[0].Position.Y = position.Y;
            _vertexBuffer[0].Position.Z = 0;
            _vertexBuffer[0].Normal.X = 0;
            _vertexBuffer[0].Normal.Y = 0;
            _vertexBuffer[0].Normal.Z = 1;
            _vertexBuffer[0].TextureCoordinate = Vector3.Zero;

            _vertexBuffer[1].Position.X = position.X + texture.Width;
            _vertexBuffer[1].Position.Y = position.Y;
            _vertexBuffer[1].Position.Z = 0;
            _vertexBuffer[1].Normal.X = 0;
            _vertexBuffer[1].Normal.Y = 0;
            _vertexBuffer[1].Normal.Z = 1;
            _vertexBuffer[1].TextureCoordinate.X = 1;
            _vertexBuffer[1].TextureCoordinate.Y = 0;
            _vertexBuffer[1].TextureCoordinate.Z = 0;

            _vertexBuffer[2].Position.X = position.X;
            _vertexBuffer[2].Position.Y = position.Y + texture.Height;
            _vertexBuffer[2].Position.Z = 0;
            _vertexBuffer[2].Normal.X = 0;
            _vertexBuffer[2].Normal.Y = 0;
            _vertexBuffer[2].Normal.Z = 1;
            _vertexBuffer[2].TextureCoordinate.X = 0;
            _vertexBuffer[2].TextureCoordinate.Y = 1;
            _vertexBuffer[2].TextureCoordinate.Z = 0;

            _vertexBuffer[3].Position.X = position.X + texture.Width;
            _vertexBuffer[3].Position.Y = position.Y + texture.Height;
            _vertexBuffer[3].Position.Z = 0;
            _vertexBuffer[3].Normal.X = 0;
            _vertexBuffer[3].Normal.Y = 0;
            _vertexBuffer[3].Normal.Z = 1;
            _vertexBuffer[3].TextureCoordinate.X = 1;
            _vertexBuffer[3].TextureCoordinate.Y = 1;
            _vertexBuffer[3].TextureCoordinate.Z = 0;

            _vertexBuffer[0].Hue = _vertexBuffer[1].Hue = _vertexBuffer[2].Hue = _vertexBuffer[3].Hue = hue;
            return DrawSprite(texture, _vertexBuffer);
        }

        public bool Draw2D(in Texture2D texture, in Vector3 position, in Rectangle sourceRect, in Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width;
            float maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height;
            float maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBuffer[0].Position.X = position.X;
            _vertexBuffer[0].Position.Y = position.Y;
            _vertexBuffer[0].Position.Z = 0;
            _vertexBuffer[0].Normal.X = 0;
            _vertexBuffer[0].Normal.Y = 0;
            _vertexBuffer[0].Normal.Z = 1;
            _vertexBuffer[0].TextureCoordinate.X = minX;
            _vertexBuffer[0].TextureCoordinate.Y = minY;
            _vertexBuffer[0].TextureCoordinate.Z = 0;

            _vertexBuffer[1].Position.X = position.X + sourceRect.Width;
            _vertexBuffer[1].Position.Y = position.Y;
            _vertexBuffer[1].Position.Z = 0;
            _vertexBuffer[1].Normal.X = 0;
            _vertexBuffer[1].Normal.Y = 0;
            _vertexBuffer[1].Normal.Z = 1;
            _vertexBuffer[1].TextureCoordinate.X = maxX;
            _vertexBuffer[1].TextureCoordinate.Y = minY;
            _vertexBuffer[1].TextureCoordinate.Z = 0;

            _vertexBuffer[2].Position.X = position.X;
            _vertexBuffer[2].Position.Y = position.Y + sourceRect.Height;
            _vertexBuffer[2].Position.Z = 0;
            _vertexBuffer[2].Normal.X = 0;
            _vertexBuffer[2].Normal.Y = 0;
            _vertexBuffer[2].Normal.Z = 1;
            _vertexBuffer[2].TextureCoordinate.X = minX;
            _vertexBuffer[2].TextureCoordinate.Y = maxY;
            _vertexBuffer[2].TextureCoordinate.Z = 0;

            _vertexBuffer[3].Position.X = position.X + sourceRect.Width;
            _vertexBuffer[3].Position.Y = position.Y + sourceRect.Height;
            _vertexBuffer[3].Position.Z = 0;
            _vertexBuffer[3].Normal.X = 0;
            _vertexBuffer[3].Normal.Y = 0;
            _vertexBuffer[3].Normal.Z = 1;
            _vertexBuffer[3].TextureCoordinate.X = maxX;
            _vertexBuffer[3].TextureCoordinate.Y = maxY;
            _vertexBuffer[3].TextureCoordinate.Z = 0;

            _vertexBuffer[0].Hue = _vertexBuffer[1].Hue = _vertexBuffer[2].Hue = _vertexBuffer[3].Hue = hue;

            return DrawSprite(texture, _vertexBuffer);
        }

        public bool Draw2D(in Texture2D texture, in Rectangle destRect, in Rectangle sourceRect, in Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width, maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height, maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBuffer[0].Position.X = destRect.X;
            _vertexBuffer[0].Position.Y = destRect.Y;
            _vertexBuffer[0].Position.Z = 0;
            _vertexBuffer[0].Normal.X = 0;
            _vertexBuffer[0].Normal.Y = 0;
            _vertexBuffer[0].Normal.Z = 1;
            _vertexBuffer[0].TextureCoordinate.X = minX;
            _vertexBuffer[0].TextureCoordinate.Y = minY;
            _vertexBuffer[0].TextureCoordinate.Z = 0;

            _vertexBuffer[1].Position.X = destRect.X + destRect.Width;
            _vertexBuffer[1].Position.Y = destRect.Y;
            _vertexBuffer[1].Position.Z = 0;
            _vertexBuffer[1].Normal.X = 0;
            _vertexBuffer[1].Normal.Y = 0;
            _vertexBuffer[1].Normal.Z = 1;
            _vertexBuffer[1].TextureCoordinate.X = maxX;
            _vertexBuffer[1].TextureCoordinate.Y = minY;
            _vertexBuffer[1].TextureCoordinate.Z = 0;

            _vertexBuffer[2].Position.X = destRect.X;
            _vertexBuffer[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBuffer[2].Position.Z = 0;
            _vertexBuffer[2].Normal.X = 0;
            _vertexBuffer[2].Normal.Y = 0;
            _vertexBuffer[2].Normal.Z = 1;
            _vertexBuffer[2].TextureCoordinate.X = minX;
            _vertexBuffer[2].TextureCoordinate.Y = maxY;
            _vertexBuffer[2].TextureCoordinate.Z = 0;

            _vertexBuffer[3].Position.X = destRect.X + destRect.Width;
            _vertexBuffer[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBuffer[3].Position.Z = 0;
            _vertexBuffer[3].Normal.X = 0;
            _vertexBuffer[3].Normal.Y = 0;
            _vertexBuffer[3].Normal.Z = 1;
            _vertexBuffer[3].TextureCoordinate.X = maxX;
            _vertexBuffer[3].TextureCoordinate.Y = maxY;
            _vertexBuffer[3].TextureCoordinate.Z = 0;

            _vertexBuffer[0].Hue = _vertexBuffer[1].Hue = _vertexBuffer[2].Hue = _vertexBuffer[3].Hue = hue;

            return DrawSprite(texture, _vertexBuffer);
        }

        public bool Draw2D(in Texture2D texture, in Rectangle destRect, in Vector3 hue)
        {
            _vertexBuffer[0].Position.X = destRect.X;
            _vertexBuffer[0].Position.Y = destRect.Y;
            _vertexBuffer[0].Position.Z = 0;
            _vertexBuffer[0].Normal.X = 0;
            _vertexBuffer[0].Normal.Y = 0;
            _vertexBuffer[0].Normal.Z = 1;
            _vertexBuffer[0].TextureCoordinate = Vector3.Zero;

            _vertexBuffer[1].Position.X = destRect.X + destRect.Width;
            _vertexBuffer[1].Position.Y = destRect.Y;
            _vertexBuffer[1].Position.Z = 0;
            _vertexBuffer[1].Normal.X = 0;
            _vertexBuffer[1].Normal.Y = 0;
            _vertexBuffer[1].Normal.Z = 1;
            _vertexBuffer[1].TextureCoordinate.X = 1;
            _vertexBuffer[1].TextureCoordinate.Y = 0;
            _vertexBuffer[1].TextureCoordinate.Z = 0;

            _vertexBuffer[2].Position.X = destRect.X;
            _vertexBuffer[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBuffer[2].Position.Z = 0;
            _vertexBuffer[2].Normal.X = 0;
            _vertexBuffer[2].Normal.Y = 0;
            _vertexBuffer[2].Normal.Z = 1;
            _vertexBuffer[2].TextureCoordinate.X = 0;
            _vertexBuffer[2].TextureCoordinate.Y = 1;
            _vertexBuffer[2].TextureCoordinate.Z = 0;

            _vertexBuffer[3].Position.X = destRect.X + destRect.Width;
            _vertexBuffer[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBuffer[3].Position.Z = 0;
            _vertexBuffer[3].Normal.X = 0;
            _vertexBuffer[3].Normal.Y = 0;
            _vertexBuffer[3].Normal.Z = 1;
            _vertexBuffer[3].TextureCoordinate.X = 1;
            _vertexBuffer[3].TextureCoordinate.Y = 1;
            _vertexBuffer[3].TextureCoordinate.Z = 0;

            _vertexBuffer[0].Hue = _vertexBuffer[1].Hue = _vertexBuffer[2].Hue = _vertexBuffer[3].Hue = hue;
            return DrawSprite(texture, _vertexBuffer);
        }

        public bool Draw2DTiled(in Texture2D texture, in Rectangle destRect, in Vector3 hue)
        {
            int y = destRect.Y;
            int h = destRect.Height;
            Rectangle sRect;

            while (h > 0)
            {
                int x = destRect.X;
                int w = destRect.Width;
                if (h < texture.Height)
                {
                    sRect = new Rectangle(0, 0, texture.Width, h);
                }
                else
                {
                    sRect = new Rectangle(0, 0, texture.Width, texture.Height);
                }
                while (w > 0)
                {
                    if (w < texture.Width)
                    {
                        sRect.Width = w;
                    }
                    Draw2D(texture, new Vector3(x, y, 0), sRect, hue);
                    w -= texture.Width;
                    x += texture.Width;
                }
                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }
    }
}
