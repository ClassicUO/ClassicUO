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

using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Label : Control
    {
        private float _alpha;
        private readonly RenderedText _gText;

        public Label(string text, bool isunicode, ushort hue, int maxwidth = 0, byte font = 0xFF, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT)
        {
            if (font == 0xFF) font = (byte) (FileManager.ClientVersion >= ClientVersions.CV_305D ? 1 : 0);

            _gText = new RenderedText
            {
                IsUnicode = isunicode,
                Font = font,
                FontStyle = style,
                Hue = hue,
                Align = align,
                MaxWidth = maxwidth,
                Text = text
            };
            AcceptMouseInput = false;
            Width = _gText.Width;
            Height = _gText.Height;
        }

        public Label(string[] parts, string[] lines) : this(lines[int.Parse(parts[4])], true, (Hue) (Hue.Parse(parts[3]) + 1), 0, style: FontStyle.BlackBorder)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }

        public string Text
        {
            get => _gText.Text;
            set
            {
                _gText.Text = value;
                Width = _gText.Width;
                Height = _gText.Height;
            }
        }

        public Hue Hue
        {
            get => _gText.Hue;
            set
            {
                if (_gText.Hue != value)
                {
                    _gText.Hue = value;
                    _gText.CreateTexture();
                }
            }
        }

        public bool FadeOut { get; set; }

        private static Hue TransformHue(Hue hue)
        {
            if (hue > 1)
                hue -= 2;

            if (hue < 2)
                hue = 1;

            return hue;
        }


        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (IsDisposed) return false;

            _gText.Draw(batcher, position, hue);

            return base.Draw(batcher, position, hue);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gText.Dispose();
        }
    }
}