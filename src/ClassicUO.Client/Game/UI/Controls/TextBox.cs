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

using ClassicUO.Renderer;
using ClassicUO.Assets;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class TextBox : Control
    {
        private RichTextLayout _rtl;
        private string _font;
        private float _size;
        private Color _color;
        private TextHorizontalAlignment _align;
        private bool _dropShadow;
        private bool _dirty = false;

        public TextBox
        (
            string text,
            string font,
            float size,
            int width,
            int hue = 2996,
            TextHorizontalAlignment align = TextHorizontalAlignment.Left,
            bool dropShadow = false
        )
        {
            _rtl = new RichTextLayout
            {
                Font = TrueTypeLoader.Instance.GetFont(font, size),
                Text = text,
                Width = width,
            };

            _font = font;
            _color.PackedValue = (uint)hue;

            if (hue == 0xFFFF || hue == ushort.MaxValue)
                _color.PackedValue = (uint)191;


            _align = align;
            _dropShadow = dropShadow;

            AcceptMouseInput = false;
            Width = width;
            Height = _rtl.Size.Y;
        }

        public string Text
        {
            get => _rtl.Text;
            set
            {
                _rtl.Text = value;
                _dirty = true;
            }
        }

        public int Hue
        {
            get => (int)_color.PackedValue;
            set
            {
                _color.PackedValue = (uint)value;
                _dirty = true;
            }
        }

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

        public static string ConvertHtmlToFontStashSharpCommand(string text)
        {
            string finalString = "";

            string[] lines = text.Split(new string[] { "<br>", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string tempLine = "/cd";
                int bfInd = line.IndexOf("<basefont color=\"");
                if (bfInd != -1)
                {
                    //<basefont color="yellow">  <-Example
                    string color = line.Substring(bfInd + 17); //Should be something like red"> or #444444"> blah blah blah
                    int endInd = color.IndexOf("\">");
                    if (endInd > -1)
                    {
                        tempLine = $"/c[{color.Substring(0, endInd)}]" + color.Substring(endInd + 2);
                    }
                    else
                    {
                        tempLine += line;
                    }
                } else
                {
                    tempLine += line;
                }
                finalString += tempLine + "\n";
            }
            GameActions.Print(finalString);
            return finalString;
        }

        public override void Update()
        {
            if (Width != _rtl.Width || _dirty)
            {
                var text = _rtl.Text;
                _rtl = new RichTextLayout
                {
                    Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                    Text = text,
                    Width = Width,
                };

                _dirty = false;
            }

            Height = _rtl.Size.Y;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
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

            if (_dropShadow)
            {
                _rtl.Draw(batcher, new Vector2(x + 1, y + 1), new Color(0, 0, 0, 0), horizontalAlignment: _align);
            }

            _rtl.Draw(batcher, new Vector2(x, y), _color, horizontalAlignment: _align);

            return true;
        }
    }
}