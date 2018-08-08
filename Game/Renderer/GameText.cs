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
    public enum FontStyle : int
    {
        None = 0x00,

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
            AllowedToDraw = true;
        }

        public SpriteTexture Texture { get; set; }
        public bool IsUnicode { get; set; }
        public Hue Hue { get; set; }
        public bool IsPartialHue { get; set; }
        public byte Font { get; set; }
        public TEXT_ALIGN_TYPE Align { get; set; }
        public byte MaxWidth { get; set; } 
        public FontStyle FontStyle { get; set; }
        public byte Cell { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    Texture = TextureManager.GetOrCreateStringTextTexture(this);
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
            Texture.Ticks = World.Ticks;

            HueVector = RenderExtentions.GetHueVector(Hue, IsPartialHue, false, false);
            return spriteBatch.Draw2D(Texture, new Rectangle(X + (int)position.X, Y + (int)position.Y, Width, Height), HueVector);
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode() + base.GetHashCode();
        }
    }
}