#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    [Flags]
    public enum FontStyle : ushort
    {
        None = 0x0000,
        Solid = 0x0001,
        Italic = 0x0002,
        Indention = 0x0004,
        BlackBorder = 0x0008,
        Underline = 0x0010,
        Fixed = 0x0020,
        Cropped = 0x0040,
        BQ = 0x0080,
        ExtraHeight = 0x0100
    }

    internal sealed class RenderedText
    {
        private byte _font;
        private string _text;

        public RenderedText()
        {
            Hue = 0xFFFF;
            Cell = 30;
        }

        public bool IsUnicode { get; set; }

        public byte Font
        {
            get => _font;
            set
            {
                if (value == 0xFF)
                    value = (byte) (FileManager.ClientVersion >= ClientVersions.CV_305D ? 1 : 0);
                _font = value;
            }
        }

        public TEXT_ALIGN_TYPE Align { get; set; }

        public int MaxWidth { get; set; }

        public FontStyle FontStyle { get; set; }

        public byte Cell { get; set; }

        public bool IsHTML { get; set; }

        public bool RecalculateWidthByInfo { get; set; }

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
                            FileManager.Fonts.SetUseHTML(false);
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

        public bool SaveHitMap { get; set; }

        public bool IsDestroyed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public FontTexture Texture { get; private set; }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, float alpha = 0, ushort hue = 0)
        {
            return Draw(batcher, x, y, Width, Height, 0, 0, alpha, hue);
        }

        public bool Draw(UltimaBatcher2D batcher, int dx, int dy, int dwidth, int dheight, int offsetX, int offsetY, float alpha = 0, ushort hue = 0)
        {
            if (string.IsNullOrEmpty(Text))
                return false;

            Rectangle src = Rectangle.Empty;

            if (offsetX > Width || offsetX < -MaxWidth || offsetY > Height || offsetY < -Height)
                return false;

            src.X = offsetX;
            src.Y = offsetY;
            int maxX = src.X + dwidth;

            if (maxX <= Width)
                src.Width = dwidth;
            else
            {
                src.Width = Width - src.X;
                dwidth = src.Width;
            }

            int maxY = src.Y + dheight;

            if (maxY <= Height)
                src.Height = dheight;
            else
            {
                src.Height = Height - src.Y;
                dheight = src.Height;
            }

            if (Texture == null)
                return false;

            Vector3 huev = Vector3.Zero;
            huev.X = hue;

            if (hue != 0)
                huev.Y = 1;
            huev.Z = alpha;

            return batcher.Draw2D(Texture, dx, dy, dwidth, dheight, src.X, src.Y, src.Width, src.Height, ref huev);
        }

        public void CreateTexture()
        {
            if (Texture != null && !Texture.IsDisposed)
            {
                Texture.Dispose();
                Texture = null;
            }

            if (IsHTML)
                FileManager.Fonts.SetUseHTML(true, HTMLColor, HasBackgroundColor);

            FileManager.Fonts.RecalculateWidthByInfo = RecalculateWidthByInfo;

            bool ispartial = false;

            if (IsUnicode)
                Texture = FileManager.Fonts.GenerateUnicode(Font, Text, Hue, Cell, MaxWidth, Align, (ushort) FontStyle, SaveHitMap);
            else
                Texture = FileManager.Fonts.GenerateASCII(Font, Text, Hue, MaxWidth, Align, (ushort) FontStyle, out ispartial, SaveHitMap);
            IsPartialHue = ispartial;

            if (Texture != null)
            {
                Width = Texture.Width;
                Height = Texture.Height;
                Links = Texture.Links;
            }

            if (IsHTML)
                FileManager.Fonts.SetUseHTML(false);
            FileManager.Fonts.RecalculateWidthByInfo = false;
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
        }
    }
}