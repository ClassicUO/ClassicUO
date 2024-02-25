#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.UI.Controls
{
    public class TextBox : Control
    {
        private RichTextLayout _rtl;
        private string _font;
        private float _size;
        private Color _color;
        private TextHorizontalAlignment _align;
        private bool _dirty = false;

        private int getStrokeSize
        {
            get
            {
                if (ProfileManager.CurrentProfile != null)
                    return ProfileManager.CurrentProfile.TextBorderSize;
                return 1;
            }
        }

        public TextBox
        (
            string text,
            string font,
            float size,
            int? width,
            int hue = 2996,
            TextHorizontalAlignment align = TextHorizontalAlignment.Left,
            bool strokeEffect = true,
            bool supportsCommands = true,
            bool ignoreColorCommands = false,
            bool calculateGlyphs = false,
            bool converthtmlcolors = true
        ) : this(text, font, size, width, ConvertHueToColor(hue), align, strokeEffect, supportsCommands, ignoreColorCommands, calculateGlyphs, converthtmlcolors) { }

        public TextBox
            (
                string text,
                string font,
                float size,
                int? width,
                Color color,
                TextHorizontalAlignment align = TextHorizontalAlignment.Left,
                bool strokeEffect = true,
                bool supportsCommands = true,
                bool ignoreColorCommands = false,
                bool calculateGlyphs = false,
                bool converthtmlcolors = true
            )
        {
            if (strokeEffect)
            {
                text = $"/es[{getStrokeSize}]" + text;
            }

            if (converthtmlcolors)
            {
                text = ConvertHTMLColorsToFSS(text);
            }

            _rtl = new RichTextLayout
            {
                Font = TrueTypeLoader.Instance.GetFont(font, size),
                Text = text,
                IgnoreColorCommand = ignoreColorCommands,
                SupportsCommands = supportsCommands,
                CalculateGlyphs = calculateGlyphs,
            };
            if (width != null)
                _rtl.Width = width;

            _font = font;
            _size = size;
            _color = color;

            _align = align;
            ConvertHtmlColors = converthtmlcolors;
            AcceptMouseInput = true;
            Width = _rtl.Width == null ? _rtl.Size.X : (int)_rtl.Width;
            base.Height = _rtl.Size.Y;
        }

        public static Color ConvertHueToColor(int hue)
        {
            if (hue == 0xFFFF || hue == ushort.MaxValue)
            {
                return Color.White;
            }
            return new Color() { PackedValue = HuesLoader.Instance.GetHueColorRgba8888(31, (ushort)hue) };
        }

        public bool PixelCheck(int x, int y)
        {
            if (!AcceptMouseInput || string.IsNullOrWhiteSpace(Text))
            {
                return false;
            }

            if (x < 0 || x >= Width)
            {
                return false;
            }

            if (y < 0 || y >= Height)
            {
                return false;
            }

            return true;
        }

        public new int Height
        {
            get
            {
                if (_rtl == null)
                    return 0;

                return _rtl.Size.Y;
            }
        }

        public Point MeasuredSize
        {
            get
            {
                if (_rtl == null)
                    return Point.Zero;
                return _rtl.Size;
            }
        }

        public string Text
        {
            get => _rtl.Text;
            set
            {
                if (_rtl.Text != value)
                {
                    if (ConvertHtmlColors)
                    {
                        _rtl.Text = ConvertHTMLColorsToFSS(value);
                    }
                    else
                    {
                        _rtl.Text = value;
                    }

                    _dirty = true;
                }
            }
        }

        public int Hue
        {
            get => (int)_color.PackedValue;
            set
            {
                var newVal = HuesLoader.Instance.GetHueColorRgba8888(31, (ushort)value);
                if (_color.PackedValue != newVal)
                {
                    _color.PackedValue = newVal;
                    _dirty = true;
                }
            }
        }

        public Color Fontcolor
        {
            get => _color;
            set
            {
                _color = value;
                _dirty = true;
            }
        }

        public RichTextLayout RTL => _rtl;

        public string Font
        {
            get => _font;
            set
            {
                _font = value;
                _dirty = true;
            }

        }

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                _dirty = true;
            }
        }

        public bool ConvertHtmlColors { get; set; }

        /// <summary>
        /// Update the text of the TextBox
        /// </summary>
        /// <param name="text">New string</param>
        /// <param name="width">Set to null to ignore width, taking as much width as needed.</param>
        public void UpdateText(string text, int? width = null, bool converthtmlcolors = true)
        {
            if (converthtmlcolors)
            {
                text = ConvertHTMLColorsToFSS(text);
            }

            if (width != null && width > 0)
            {
                _rtl = new RichTextLayout
                {
                    Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                    Text = text,
                    Width = width
                };
            }
            else
            {
                _rtl = new RichTextLayout
                {
                    Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                    Text = text,
                };
                Width = _rtl.Size.X;
            }
        }

        public static string ConvertHTMLColorsToFSS(string text)
        {
            string finalString;

            if (string.IsNullOrEmpty(text))
                return "";

            finalString = Regex.Replace(text, "<basefont color=\"?'?(?<color>.*?)\"?'?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = Regex.Replace(finalString, "<Bodytextcolor\"?'?(?<color>.*?)\"?'?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = finalString.Replace("</basefont>", "/cd").Replace("</BASEFONT>", "/cd").Replace("\n", "\n/cd");

            return finalString;
        }

        public static string ConvertHtmlToFontStashSharpCommand(string text)
        {
            string finalString;

            if (string.IsNullOrEmpty(text))
                return "";

            finalString = ConvertHTMLColorsToFSS(text);

            finalString = finalString.Replace("<br>", "\n").Replace("<BR>", "\n");
            finalString = finalString.Replace("<left>", "").Replace("</left>", "");
            finalString = finalString.Replace("<b>", "").Replace("</b>", "");
            finalString = finalString.Replace("</font>", "").Replace("<h2>", "");
            finalString = finalString.Replace("<BODY>", "").Replace("<body>", "");
            finalString = finalString.Replace("</BODY>", "").Replace("</body>", "");
            finalString = finalString.Replace("</p>", "").Replace("<p>", "");
            return finalString;
        }

        public override void Update()
        {
            if (Width != _rtl.Width || _dirty || WantUpdateSize)
            {
                var text = _rtl.Text;

                if (WantUpdateSize)
                {
                    _rtl = new RichTextLayout
                    {
                        Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                        Text = text,
                    };
                }
                else
                {
                    _rtl = new RichTextLayout
                    {
                        Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                        Text = text,
                        Width = Width,
                    };
                }

                WantUpdateSize = false;
                _dirty = false;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            return Draw(batcher, x, y, _color);
        }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, Color color)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (_align == TextHorizontalAlignment.Center)
            {
                x += Width / 2;
            }
            else if (_align == TextHorizontalAlignment.Right)
            {
                x += Width;
            }

            _rtl.Draw(batcher, new Vector2(x, y), color * Alpha, horizontalAlignment: _align);

            return true;
        }
    }
}