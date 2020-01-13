#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Collections.Generic;
using ClassicUO.Data;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class CroppedText : Control
    {
        private readonly RenderedText _gameText;

        public CroppedText(string text, ushort hue, int maxWidth = 0)
        {
            _gameText = RenderedText.Create(text, hue, (byte)(Client.Version >= ClientVersion.CV_305D ? 1 : 0), true, maxWidth > 0 ? FontStyle.BlackBorder | FontStyle.Cropped : FontStyle.BlackBorder,
                                            maxWidth: maxWidth);
            AcceptMouseInput = false;
        }

        public CroppedText(List<string> parts, string[] lines) : this(int.TryParse(parts[6], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty, (ushort) (UInt16Converter.Parse(parts[5]) + 1), int.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            _gameText.Draw(batcher, x, y);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gameText?.Destroy();
        }
    }
}