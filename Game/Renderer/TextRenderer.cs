using ClassicUO.AssetsLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer
{
    public sealed class TextRenderer
    {
        private bool _isPartialHue;

        public TextRenderer(in string text = "")
        {
            Text = text;
        }

        public string Text { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TextTexture Texture { get; private set; }
        public bool IsUnicode { get; set; }
        public byte Font { get; set; }
        public ushort Color { get; set; }

        public void GenerateTexture(in int maxWidth, in ushort flags, in TEXT_ALIGN_TYPE aling, in byte cell)
        {
            uint[] data;
            int linesCount;
            List<WebLinkRect> links;

            if (IsUnicode)
                (data, Width, Height, linesCount, links) = Fonts.GenerateUnicode(Font, Text, Color, cell, maxWidth, aling, flags);
            else
                (data, Width, Height, linesCount, _isPartialHue) = Fonts.GenerateASCII(Font, Text, Color, maxWidth, aling, flags);


            if (Texture != null)
                Texture.Dispose();

            if (data == null || data.Length <= 0)
                return;

            Texture = new TextTexture(TextureManager.Device, Width, Height, false, SurfaceFormat.Color);
            Texture.SetData(data);
            Texture.LinesCount = linesCount;
        }

        public void Draw(in SpriteBatchUI spriteBatch, in Point position)
        {
            spriteBatch.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height), RenderExtentions.GetHueVector(0, _isPartialHue, false, false)  );
            // Draw(spriteBatch, new Rectangle(position.X, position.Y, Width, Height), 0, 0);
        }


        public void Draw(in SpriteBatchUI spriteBatch, Rectangle destRect, in int scrollX, in int scrollY)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            Rectangle sourceRect;

            sourceRect.X = scrollX;
            sourceRect.Y = scrollY;

            int maxX = sourceRect.X + destRect.Width;
            if (maxX <= Width)
                sourceRect.Width = destRect.Width;
            else
                destRect.Width = sourceRect.Width = Width - sourceRect.X;

            int maxY = sourceRect.Y + destRect.Height;
            if (maxY <= Height)
                sourceRect.Height = destRect.Height;
            else
                destRect.Height = sourceRect.Height = Height - sourceRect.Y;



            spriteBatch.Draw2D(Texture, destRect, sourceRect, new Vector3(0, 0, 0) /*new Vector3(Color, _isPartialHue ? -2 : -1, 0)*/ /*RenderExtentions.GetHueVector(0, _isPartialHue, false ,false)*/ );
        }
    }
}
