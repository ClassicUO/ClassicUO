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
using ClassicUO.Interfaces;
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
        ExtraHeight = 0x0100,
        CropTexture = 0x0200
    }

    internal sealed class RenderedText
    {
        private byte _font;
        private string _text;
        private FontTexture _texture;

        private static readonly Queue<RenderedText> _pool = new Queue<RenderedText>();

        private RenderedText()
        {

        }

        public static RenderedText Create(string text, ushort hue = 0xFFFF, byte font = 0xFF, bool isunicode = true, FontStyle style = 0, TEXT_ALIGN_TYPE align = 0, 
                                          int maxWidth = 0, byte cell = 30, bool isHTML = false, 
                                          bool recalculateWidthByInfo = false, bool saveHitmap = false)
        {
            RenderedText r;
            if (_pool.Count != 0)
            {
                r = _pool.Dequeue();
                r.IsDestroyed = false;
                r.Links.Clear();
            }
            else
            {
                r = new RenderedText();
            }

            r.Hue = hue;
            r.Font = font;
            r.IsUnicode = isunicode;
            r.FontStyle = style;
            r.Cell = cell;
            r.Align = align;
            r.MaxWidth = maxWidth;
            r.IsHTML = isHTML;
            r.RecalculateWidthByInfo = recalculateWidthByInfo;
            r.Width = 0;
            r.Height = 0;
            r.SaveHitMap = saveHitmap;
            r.HTMLColor = 0xFFFF_FFFF;
            r.HasBackgroundColor = false;
            r.IsPartialHue = false;

            if (r.Text != text)
                r.Text = text; // here makes the texture
            else 
                r.CreateTexture();
            return r;
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

        public int MaxHeight { get; set; } = 0;

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
                        _texture?.Dispose();
                        _texture = null;
                    }
                    else
                        CreateTexture();
                }
            }
        }

        public int LinesCount => Texture == null || Texture.IsDisposed ? 0 : Texture.LinesCount;

        public bool IsPartialHue { get; private set; }

        public bool SaveHitMap { get; private set; }

        public bool IsDestroyed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public FontTexture Texture => _texture;

        public bool Draw(UltimaBatcher2D batcher, int x, int y, float alpha = 0, ushort hue = 0)
        {
            return Draw(batcher, x, y, Width, Height, 0, 0, alpha, hue);
        }

        private static Vector3 _hueVector = Vector3.Zero;

        public bool Draw(UltimaBatcher2D batcher, int dx, int dy, int dwidth, int dheight, int offsetX, int offsetY, float alpha = 0, ushort hue = 0)
        {
            if (string.IsNullOrEmpty(Text) || Texture == null)
                return false;


            if (offsetX > Width || offsetX < -MaxWidth || offsetY > Height || offsetY < -Height)
                return false;

            int srcX = offsetX;
            int srcY = offsetY;
            int maxX = srcX + dwidth;

            int srcWidth;
            int srcHeight;

            if (maxX <= Width)
                srcWidth = dwidth;
            else
            {
                srcWidth = Width - srcX;
                dwidth = srcWidth;
            }

            int maxY = srcY + dheight;

            if (maxY <= Height)
                srcHeight = dheight;
            else
            {
                srcHeight = Height - srcY;
                dheight = srcHeight;
            }

            _hueVector.X = hue;

            if (hue != 0)
            {
                if (IsUnicode)
                    _hueVector.Y = ShaderHuesTraslator.SHADER_TEXT_HUE_NO_BLACK;
                else if (Font == 3)
                    _hueVector.Y = 5;
                else if (Font != 5 && Font != 8)
                    _hueVector.Y = ShaderHuesTraslator.SHADER_PARTIAL_HUED;
                else
                    _hueVector.Y = ShaderHuesTraslator.SHADER_HUED;
            }
            else
                _hueVector.Y = 0;

            _hueVector.Z = alpha;

            return batcher.Draw2D(Texture, dx, dy, dwidth, dheight, srcX, srcY, srcWidth, srcHeight, ref _hueVector);
        }

        public void CreateTexture()
        {
            if (_texture != null && !_texture.IsDisposed)
            {
                _texture.Dispose();
                _texture = null;
            }

            if (IsHTML)
                FileManager.Fonts.SetUseHTML(true, HTMLColor, HasBackgroundColor);

            FileManager.Fonts.RecalculateWidthByInfo = RecalculateWidthByInfo;

            bool ispartial = false;

            if (IsUnicode)
                FileManager.Fonts.GenerateUnicode(ref _texture, Font, Text, Hue, Cell, MaxWidth, Align, (ushort)FontStyle, SaveHitMap, MaxHeight);
            else
                FileManager.Fonts.GenerateASCII(ref _texture, Font, Text, Hue, MaxWidth, Align, (ushort)FontStyle, out ispartial, SaveHitMap, MaxHeight);

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

            _pool.Enqueue(this);
        }
    }
}