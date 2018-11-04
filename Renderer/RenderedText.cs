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

using System;
using System.Collections.Generic;

using ClassicUO.Game;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    [Flags]
    public enum FontStyle
    {
        None = 0x00,
        Solid = 0x01,
        Italic = 0x02,
        Indention = 0x04,
        BlackBorder = 0x08,
        Underline = 0x10,
        Fixed = 0x20,
        Cropped = 0x40,
        BQ = 0x80
    }

    public class RenderedText
    {
        private string _text;

        public RenderedText()
        {
            Hue = 0xFFFF;
            Cell = 30;
        }

        public bool IsUnicode { get; set; }

        public byte Font { get; set; }

        public TEXT_ALIGN_TYPE Align { get; set; }

        public int MaxWidth { get; set; }

        public FontStyle FontStyle { get; set; }

        public byte Cell { get; set; }

        public bool IsHTML { get; set; }

        public List<WebLinkRect> Links { get; set; } = new List<WebLinkRect>();

        public Hue Hue { get; set; }

        public uint HTMLColor { get; set; } = 0xFFFFFFFF;

        public bool HasBackgroundColor { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;

                    if (string.IsNullOrEmpty(value))
                    {
                        Width = 0;
                        Height = 0;
                        IsPartialHue = false;

                        if (IsHTML)
                            Fonts.SetUseHTML(false);
                        Links.Clear();

                        Texture?.Dispose();
                        Texture = null;
                    }
                    else
                        CreateTexture();
                }
            }
        }

        public int LinesCount => Texture == null || Texture.IsDisposed ? 0 : Texture.LinesCount;

        public bool IsPartialHue { get; set; }

        public bool IsDisposed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public FontTexture Texture { get; private set; }

        public bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            return Draw(spriteBatch, new Rectangle((int) position.X, (int) position.Y, Width, Height), 0, 0, hue);
        }

        public bool Draw(SpriteBatchUI spriteBatch, Rectangle dst, int offsetX, int offsetY, Vector3? hue = null)
        {
            if (string.IsNullOrEmpty(Text))
                return false;
            Rectangle src = new Rectangle();

            if (offsetX > Width || offsetX < -MaxWidth || offsetY > Height || offsetY < -Height)
                return false;
            src.X = offsetX;
            src.Y = offsetY;
            int maxX = src.X + dst.Width;

            if (maxX <= Width)
                src.Width = dst.Width;
            else
            {
                src.Width = Width - src.X;
                dst.Width = src.Width;
            }

            int maxY = src.Y + dst.Height;

            if (maxY <= Height)
                src.Height = dst.Height;
            else
            {
                src.Height = Height - src.Y;
                dst.Height = src.Height;
            }

            return spriteBatch.Draw2D(Texture, dst, src, hue ?? Vector3.Zero);
        }

        public void CreateTexture()
        {
            if (Texture != null && !Texture.IsDisposed)
            {
                Texture.Dispose();
                Texture = null;
            }

            if (IsHTML)
                Fonts.SetUseHTML(true, HTMLColor, HasBackgroundColor);

            bool ispartial = false;
            Fonts.FontTextureInfo? info;

            if (IsUnicode)
                info = Fonts.GenerateUnicode(Font, Text, Hue, Cell, MaxWidth, Align, (ushort)FontStyle);
            else
                info = Fonts.GenerateASCII(Font, Text, Hue, MaxWidth, Align, (ushort)FontStyle, out ispartial);

            IsPartialHue = ispartial;

            if (info.HasValue)
            {
                Texture = new FontTexture(info.Value.Width, info.Value.Height, info.Value.LinesCount, info.Value.Links);
                Texture.SetData(info.Value.Pixels);
          

                Width = Texture.Width;
                Height = Texture.Height;
                Links = Texture.Links;
            }

            if (IsHTML)
                Fonts.SetUseHTML(false);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
        }
    }
}