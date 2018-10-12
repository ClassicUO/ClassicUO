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

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class DevConsole : Gump
    {
        private const ushort BLACK = 0x243A;
        private const ushort GRAY = 0x248A;

        private const int MAX_LINES = 15;

        private readonly TextBox _textbox;

        public DevConsole() : base(0, 0)
        {
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            CanMove = true;

            X = 150;
            Y = 50;

            AddChildren(new GumpPicTiled(BLACK)
            {
                Width = 400,
                Height = 400
            });

            AddChildren(_textbox = new TextBox(2, -1, 350, style: FontStyle.BlackBorder)
            {
                Width = 400,
                Height = 400,
                CanMove = true
            });
        }

        public void Append(string line) => _textbox.SetText(line, true);

        public void AppendLine(string line)
        {
            if (_textbox.LinesCount + 1 > MAX_LINES) _textbox.RemoveLineAt(0);
            _textbox.SetText(_textbox.Text + line + "\n");
        }

        public void RemoveLine()
        {
            if (_textbox.LinesCount > 0)
            {
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
            base.Draw(spriteBatch, position, hue);
    }
}