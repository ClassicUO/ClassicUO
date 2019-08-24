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

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class HoveredLabel : Label
    {
        private readonly ushort _overHue, _normalHue;

        public HoveredLabel(string text, bool isunicode, ushort hue, ushort overHue, int maxwidth = 0, byte font = 255, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) : base($" {text}", isunicode, hue, maxwidth, font, style, align)
        {
            _overHue = overHue;
            _normalHue = hue;
            AcceptMouseInput = true;
        }
        public bool DrawBackgroundCurrentIndex { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (MouseIsOver)
            {
                if (Hue != _overHue)
                    Hue = _overHue;
            }
            else if (Hue != _normalHue)
                    Hue = _normalHue;
            

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DrawBackgroundCurrentIndex && MouseIsOver && !string.IsNullOrWhiteSpace(Text))
            {
                ResetHueVector();
                batcher.Draw2D(Textures.GetTexture(Color.Gray), x, y + 2, Width - 4, Height - 4, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }
    }
}