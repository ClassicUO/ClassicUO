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

using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Panel : Control
    {
        private readonly SpriteTexture[] _frame = new SpriteTexture[9];

        public Panel(ushort background)
        {
            for (int i = 0; i < _frame.Length; i++)
                _frame[i] = FileManager.Gumps.GetTexture((ushort) (background + i));
        }

        public override void Update(double totalMS, double frameMS)
        {
            foreach (SpriteTexture t in _frame)
            {
                if (t != null)
                    t.Ticks = (long) totalMS;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            int centerWidth = Width - _frame[0].Width - _frame[2].Width;
            int centerHeight = Height - _frame[0].Height - _frame[6].Height;
            int line2Y = y + _frame[0].Height;
            int line3Y = y + Height - _frame[6].Height;
            // top row
            batcher.Draw2D(_frame[0], x, y, Vector3.Zero);
            batcher.Draw2DTiled(_frame[1], x + _frame[0].Width, y, centerWidth, _frame[0].Height, Vector3.Zero);
            batcher.Draw2D(_frame[2], x + Width - _frame[2].Width, y, Vector3.Zero);
            // middle
            batcher.Draw2DTiled(_frame[3], x, line2Y, _frame[3].Width, centerHeight, Vector3.Zero);
            batcher.Draw2DTiled(_frame[4], x + _frame[3].Width, line2Y, centerWidth, centerHeight, Vector3.Zero);
            batcher.Draw2DTiled(_frame[5], x + Width - _frame[5].Width, line2Y, _frame[5].Width, centerHeight, Vector3.Zero);
            // bottom
            batcher.Draw2D(_frame[6], x, line3Y, Vector3.Zero);
            batcher.Draw2DTiled(_frame[7], x + _frame[6].Width, line3Y, centerWidth, _frame[6].Height, Vector3.Zero);
            batcher.Draw2D(_frame[8], x + Width - _frame[8].Width, line3Y, Vector3.Zero);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}