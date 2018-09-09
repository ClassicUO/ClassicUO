#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
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

    public class RenderedText : IDrawableUI
    {
        private Rectangle _bounds;
        private string _text;
        private SpriteTexture _texture;

        public RenderedText(string text = "")
        {
            _text = text;
            Hue = 0xFFFF;
        }

        public bool IsUnicode { get; set; }
        public byte Font { get; set; }
        public TEXT_ALIGN_TYPE Align { get; set; }
        public int MaxWidth { get; set; }
        public FontStyle FontStyle { get; set; }
        public byte Cell { get; set; } = 30;
        public MessageType MessageType { get; set; }
        //public long Timeout { get; set; }
        //public bool IsPersistent { get; set; }
        public bool IsHTML { get; set; }
        public List<WebLinkRect> Links { get; set; } = new List<WebLinkRect>();
        public Hue Hue { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (!string.IsNullOrEmpty(value) && _text != value)
                {
                    _text = value;
                    Texture = CreateTexture();
                }
            }
        }


        public bool IsPartialHue { get; set; }

        public bool IsDisposed { get; private set; }

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

        public SpriteTexture Texture
        {
            get
            {
                if (!string.IsNullOrEmpty(_text) && (_texture == null || _texture.IsDisposed))
                    _texture = CreateTexture();
                return _texture;
            }
            set
            {
                if (_texture != null && !_texture.IsDisposed)
                    _texture.Dispose();
                _texture = value;
            }
        }
        public bool AllowedToDraw { get; set; } = true;
        public Vector3 HueVector { get; set; }

        public bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            if (string.IsNullOrEmpty(Text))
                return false;

            Rectangle src = new Rectangle();
            Rectangle dst = new Rectangle((int)position.X, (int)position.Y, Width, Height);

            if (dst.Width <= Width)
                src.Width = dst.Width;
            else
            {
                src.Width = Width - src.X;
                dst.Width = src.Width;
            }

            if (dst.Height <= Height)
                src.Height = dst.Height;
            else
            {
                src.Height = Height - src.Y;
                dst.Height = src.Height;
            }

            return spriteBatch.Draw2D(Texture, dst, src, HueVector);
        }

        private Fonts.FontTexture CreateTexture()
        {
            if (IsHTML)
                Fonts.SetUseHTML(true);

            Fonts.FontTexture ftexture;

            if (IsUnicode)
            {
                Fonts.GenerateUnicode(out ftexture, Font, Text, Hue, Cell, MaxWidth, Align, (ushort)FontStyle);
            }
            else
            {
                //(data, gt.Width, gt.Height, linesCount, gt.IsPartialHue) = Fonts.GenerateASCII(gt.Font, gt.Text, gt.Hue, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
                IsPartialHue = Fonts.GenerateASCII(out ftexture, Font, Text, Hue, MaxWidth, Align, (ushort)FontStyle);
            }

            Width = ftexture.Width;
            Height = ftexture.Height;
            Links = ftexture.Links;

            //var texture = new SpriteTexture(gt.Width, gt.Height);
            //texture.SetData(data);


            if (IsHTML)
                Fonts.SetUseHTML(false);

            return ftexture;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            if (_texture != null && !_texture.IsDisposed)
                _texture.Dispose();
        }
    }
}