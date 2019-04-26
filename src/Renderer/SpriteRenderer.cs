using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal static class SpriteRenderer
    {
        /* The main target of this class it to reduce the amount of properties around the objects.
           Than it will be useful in future when i'll implement the ECS system*/
        
        public static void DrawGump(Graphic graphic, Hue hue, int x, int y, bool ispartial)
        {
            Texture2D texture = FileManager.Gumps.GetTexture(graphic);
            if (texture == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue, ispartial, 0);

            Engine.Batcher.Draw2D(texture, x, y, huev);
        }

        public static void DrawGump(Graphic graphic, Hue hue, int x, int y, int width, int height, bool ispartial)
        {
            Texture2D texture = FileManager.Gumps.GetTexture(graphic);
            if (texture == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue, ispartial, 0);

            Engine.Batcher.Draw2D(texture, x, y, width, height, huev);
        }


        private static readonly Texture2D[] _textureCache = new Texture2D[9];

        public static void DrawGumpTiled(Graphic graphic, int x, int y, int width, int height)
        {
            Array.Clear(_textureCache, 0, 9);

            for (int i = 0; i < 9; i++)
            {
                Texture2D pth = FileManager.Gumps.GetTexture((uint) (graphic + i));

                if (pth == null)
                    return;

                if (i == 4)
                    _textureCache[8] = pth;
                else if (i > 4)
                    _textureCache[i - 1] = pth;
                else
                {
                    _textureCache[i] = pth;
                }
            }

            int offsetTop = Math.Max(_textureCache[0].Height, _textureCache[2].Height) - _textureCache[1].Height;
            int offsetBottom = Math.Max(_textureCache[5].Height, _textureCache[7].Height) - _textureCache[6].Height;
            int offsetLeft = Math.Max(_textureCache[0].Width, _textureCache[5].Width) - _textureCache[3].Width;
            int offsetRight = Math.Max(_textureCache[2].Width, _textureCache[7].Width) - _textureCache[4].Width;

            for (int i = 0; i < 9; i++)
            {
                int drawWidth = _textureCache[i].Width;
                int drawHeight = _textureCache[i].Height;
                float drawCountX = 1.0f;
                float drawCountY = 1.0f;
                int drawX = x;
                int drawY = y;

                switch (i)
                {
                    case 1:

                    {
                        drawX += _textureCache[0].Width;

                        drawWidth = width - _textureCache[0].Width - _textureCache[2].Width;

                        drawCountX = drawWidth / (float) _textureCache[i].Width;

                        break;
                    }
                    case 2:

                    {
                        drawX += width - drawWidth;
                        drawY += offsetTop;

                        break;
                    }
                    case 3:

                    {
                        drawY += _textureCache[0].Height;
                        drawX += offsetLeft;

                        drawHeight = height - _textureCache[0].Height - _textureCache[5].Height;

                        drawCountY = drawHeight / (float) _textureCache[i].Height;

                        break;
                    }
                    case 4:

                    {
                        drawX += width - drawWidth - offsetRight;
                        drawY += _textureCache[2].Height;

                        drawHeight = height - _textureCache[2].Height - _textureCache[7].Height;

                        drawCountY = drawHeight / (float) _textureCache[i].Height;

                        break;
                    }
                    case 5:

                    {
                        drawY += height - drawHeight;

                        break;
                    }
                    case 6:

                    {
                        drawX += _textureCache[5].Width;
                        drawY += height - drawHeight - offsetBottom;

                        drawWidth = width - _textureCache[5].Width - _textureCache[7].Width;

                        drawCountX = drawWidth / (float) _textureCache[i].Width;

                        break;
                    }
                    case 7:

                    {
                        drawX += width - drawWidth;
                        drawY += height - drawHeight;

                        break;
                    }
                    case 8:

                    {
                        drawX += _textureCache[0].Width;
                        drawY += _textureCache[0].Height;

                        drawWidth = width - _textureCache[0].Width - _textureCache[2].Width;

                        drawHeight = height - _textureCache[2].Height - _textureCache[7].Height;

                        drawCountX = drawWidth / (float) _textureCache[i].Width;
                        drawCountY = drawHeight / (float) _textureCache[i].Height;

                        break;
                    }
                }


                for (int xx = 0; xx < drawCountX; xx++)
                    for (int yy = 0; yy < drawCountY; yy++)
                        Engine.Batcher.Draw2D(_textureCache[i], drawX + xx, drawY + yy, drawWidth, drawHeight, Vector3.Zero);
            }
        }

        public static void DrawLand(Land land, Hue hue, int x, int y, SpriteVertex[] vertices)
        {
            Texture2D th = FileManager.Textmaps.GetTexture(land.TileData.TexID);

            if (th == null)
            {
                DrawLandArt(land.Graphic, hue, x, y);
            }
            else
            {
                Vector3 huev = Vector3.Zero;

                if (hue != 0)
                    ShaderHuesTraslator.GetHueVector(ref huev, hue, ShaderHuesTraslator.SHADER_LAND_HUED);
                else
                    ShaderHuesTraslator.GetHueVector(ref huev, hue, ShaderHuesTraslator.SHADER_LAND);

                for (int i = 0; i < 4; i++)
                    vertices[i].Hue = huev;

                Engine.Batcher.DrawSprite(th, vertices);
            }
        }

        public static void DrawLandArt(Graphic graphic, Hue hue, int x, int y)
        {
            Texture2D th = FileManager.Art.GetLandTexture(graphic);

            if (th == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue, ShaderHuesTraslator.SHADER_HUED);


            SpriteVertex[] vertices = SpriteVertex.PolyBuffer;

            vertices[0].Position.X = x;
            vertices[0].Position.Y = y;
            vertices[0].TextureCoordinate.Y = 0;
            vertices[1].Position = vertices[0].Position;
            vertices[1].Position.X += 44;
            vertices[1].TextureCoordinate.Y = 0;
            vertices[2].Position = vertices[0].Position;
            vertices[2].Position.Y += 44;
            vertices[3].Position = vertices[1].Position;
            vertices[3].Position.Y += 44;

            for (int i = 0; i < 4; i++)
                vertices[i].Hue = huev;

            Engine.Batcher.DrawSprite(th, vertices);
        }

        public static void DrawStaticArt(Graphic graphic, Hue hue, int x, int y)
        {
            Texture2D th = FileManager.Art.GetTexture(graphic);

            if (th == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue);

            SpriteVertex[] vertices = SpriteVertex.PolyBuffer;

            vertices[0].Position.X = x;
            vertices[0].Position.Y = y;
            vertices[0].Position.X -= (th.Width >> 1) - 22;
            vertices[0].Position.Y -= th.Height - 44;
            vertices[0].TextureCoordinate.Y = 0;
            vertices[1].Position = vertices[0].Position;
            vertices[1].Position.X += th.Width;
            vertices[1].TextureCoordinate.Y = 0;
            vertices[2].Position = vertices[0].Position;
            vertices[2].Position.Y += th.Height;
            vertices[3].Position = vertices[1].Position;
            vertices[3].Position.Y += th.Height;

            for (int i = 0; i < 4; i++)
                vertices[i].Hue = huev;

            Engine.Batcher.DrawSprite(th, vertices);
        }

        public static void DrawStaticArtAnimated(Graphic graphic, Hue hue, int x, int y, byte offset)
        {
            DrawStaticArt( (Graphic)(graphic + offset), hue, x, y);
        }

        public static void DrawStaticArtRotated(Graphic graphic, Hue hue, int x, int y, float angle)
        {
            Texture2D th = FileManager.Art.GetTexture(graphic);

            if (th == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue);


            float w = th.Width / 2f;
            float h = th.Height / 2f;
            Vector3 center = Vector3.Zero;
            center.X = x;
            center.Y = y;
            center.X -= ((th.Width >> 1) - 22) - 44 + w;
            center.Y -= (th.Height - 44) + h;
            float sinx = (float)Math.Sin(angle) * w;
            float cosx = (float)Math.Cos(angle) * w;
            float siny = (float)Math.Sin(angle) * h;
            float cosy = (float)Math.Cos(angle) * h;

            SpriteVertex[] vertices = SpriteVertex.PolyBufferFlipped;
            vertices[0].Position = center;
            vertices[0].Position.X += cosx - -siny;
            vertices[0].Position.Y -= sinx + -cosy;
            vertices[1].Position = center;
            vertices[1].Position.X += cosx - siny;
            vertices[1].Position.Y += -sinx + -cosy;
            vertices[2].Position = center;
            vertices[2].Position.X += -cosx - -siny;
            vertices[2].Position.Y += sinx + cosy;
            vertices[3].Position = center;
            vertices[3].Position.X += -cosx - siny;
            vertices[3].Position.Y += sinx + -cosy;

            for (int i = 0; i < 4; i++)
                vertices[i].Hue = huev;

            Engine.Batcher.DrawSprite(th, vertices);
        }

        public static void DrawStaticArtAnimatedRotated(Graphic graphic, Hue hue, int x, int y, float angle, byte offset)
        {
            DrawStaticArtRotated((Graphic) (graphic + offset), hue, x, y, angle);
        }

        public static void DrawStaticArtTransparent(Graphic graphic, Hue hue, int x, int y, bool selection)
        {
            Texture2D th = FileManager.Art.GetTexture(graphic);

            if (th == null)
                return;

            Vector3 huev = Vector3.Zero;

            if (hue != 0)
                ShaderHuesTraslator.GetHueVector(ref huev, hue, false, 0.5f);

            SpriteVertex[] vertices = SpriteVertex.PolyBuffer;

            vertices[0].Position.X = x;
            vertices[0].Position.Y = y;
            vertices[0].Position.X -= (th.Width >> 1) - 22;
            vertices[0].Position.Y -= th.Height - 44;
            vertices[0].TextureCoordinate.Y = 0;
            vertices[1].Position = vertices[0].Position;
            vertices[1].Position.X += th.Width;
            vertices[1].TextureCoordinate.Y = 0;
            vertices[2].Position = vertices[0].Position;
            vertices[2].Position.Y += th.Height;
            vertices[3].Position = vertices[1].Position;
            vertices[3].Position.Y += th.Height;

            for (int i = 0; i < 4; i++)
                vertices[i].Hue = huev;

            Engine.Batcher.DrawSprite(th, vertices);
        }

        public static void DrawStaticArtAnimatedTransparent(Graphic graphic, Hue hue, int x, int y, bool selection, byte offset)
        {
            DrawStaticArtTransparent((Graphic) (graphic + offset), hue, x, y, selection);
        }

        public static void DrawLight()
        {

        }
    }
}
