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

using ClassicUO.Input;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class AbstractTextBox : Control
    {
        public abstract AbstractEntry EntryValue { get; }

        public int MaxCharCount { get; set; }
        public bool Unicode { get; protected set; }
        public byte Font { get; protected set; }
        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                SetKeyboardFocus();
                EntryValue?.OnMouseClick(x, y);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left) EntryValue?.OnSelectionEnd(x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            EntryValue?.OnDraw(batcher, ScreenCoordinateX, ScreenCoordinateY);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            IsEditable = false;
            EntryValue?.Destroy();
        }
    }
}