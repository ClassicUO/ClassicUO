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

using static SDL2.SDL;

namespace ClassicUO.Input
{
    internal class InputKeyboardEvent : InputEvent
    {
        private readonly int _keyDataExtra;

        public InputKeyboardEvent(KeyboardEvent eventtype, SDL_Keycode keycode, int key, SDL_Keymod modifiers) : base(modifiers)
        {
            EventType = eventtype;
            KeyCode = keycode;
            _keyDataExtra = key;
        }

        public InputKeyboardEvent(KeyboardEvent eventtype, InputKeyboardEvent parent) : base(parent)
        {
            EventType = eventtype;
            KeyCode = parent.KeyCode;
            _keyDataExtra = parent._keyDataExtra;
        }

        public KeyboardEvent EventType { get; }

        public SDL_Keycode KeyCode { get; }

        public bool IsChar => KeyChar.Length > 0 && KeyChar[0] != '\0';

        public string KeyChar { get; set; }
    }
}