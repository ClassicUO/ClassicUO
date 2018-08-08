using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = ClassicUO.Game.GameObjects.Interfaces.IDrawable;
using IUpdateable = ClassicUO.Game.GameObjects.Interfaces.IUpdateable;

namespace ClassicUO.Game.Renderer
{
    public sealed class TextRenderer
    {
        private bool _isPartialHue;
        private string _text;
        private bool _textChanged;

        public TextRenderer(in string text = "")
        {
            _text = text;
            _textChanged = true;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    _textChanged = true;
                }
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public TextTexture Texture { get; private set; }
        public bool IsUnicode { get; set; }
        public byte Font { get; set; }
        public ushort Color { get; set; }

        public void GenerateTexture(in int maxWidth, in ushort flags, in TEXT_ALIGN_TYPE aling, in byte cell)
        {
            if (!_textChanged)
                return;

            _textChanged = false;

            uint[] data;
            int linesCount;
            List<WebLinkRect> links;

            if (IsUnicode)
                (data, Width, Height, linesCount, links) = Fonts.GenerateUnicode(Font, Text, Color, cell, maxWidth, aling, flags);
            else
                (data, Width, Height, linesCount, _isPartialHue) = Fonts.GenerateASCII(Font, Text, Color, maxWidth, aling, flags);


            Texture?.Dispose();

            if (data == null || data.Length <= 0)
                return;

            Texture = new TextTexture(TextureManager.Device, Width, Height, false, SurfaceFormat.Color);
            Texture.SetData(data);
            Texture.LinesCount = linesCount;
        }

        public void Draw(in SpriteBatch3D spriteBatch, in Point position)
        {
            //spriteBatch.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height),
            //    RenderExtentions.GetHueVector(0, _isPartialHue, false, false));
            Draw(spriteBatch, new Rectangle(position.X, position.Y, Width, Height), 0, 0);
        }


        public void Draw(in SpriteBatch3D spriteBatch, Rectangle destRect, in int scrollX, in int scrollY)
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


            spriteBatch.Draw2D(Texture, destRect, sourceRect, RenderExtentions.GetHueVector(0, _isPartialHue, false, false));
        }
    }


    public enum FontStyle : int
    {
        Solid = 0x01,
        Italic = 0x02,
        Indention = 0x04,
        BlackBorder = 0x08,
        Underline = 0x10,
        Cropped = 0x40,
        BQ = 0x80
    }

    public class GameText : IDrawable, IUpdateable, IDisposable
    {
        private string _text;
        private Rectangle _bounds;

        public GameText(in string text = "")
        {
            _text = text;
        }

        public SpriteTexture Texture { get; set; }
        public bool IsUnicode { get; set; }
        public Hue Hue { get; set; }
        public bool IsPartialHue { get; set; }
        public byte Font { get; set; }
        public TEXT_ALIGN_TYPE Align { get; set; }
        public byte MaxWidth { get; set; } 
        public FontStyle FontStyle { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;

                }
            }
        }

        public Rectangle Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public int X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public int Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public int Width
        {
            get => _bounds.Width;
            set => _bounds.Width = value;
        }

        public int Height
        {
            get => _bounds.Height;
            set => _bounds.Height = value;
        }

        public bool AllowedToDraw { get; set; }

        public Vector3 HueVector { get; set; }

        public bool IsDisposed { get; set; }




        public void Update(in double frameMS)
        {

        }

        public bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            HueVector = RenderExtentions.GetHueVector(Hue, IsPartialHue, false, false);
            return spriteBatch.Draw2D(Texture, Bounds, HueVector);
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
        }
    }
}