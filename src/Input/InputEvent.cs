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
    internal class InputEvent
    {
        public InputEvent(SDL_Keymod modifiers)
        {
            Mod = modifiers;
        }

        protected InputEvent(InputEvent parent)
        {
            Mod = parent.Mod;
        }

        public SDL_Keymod Mod { get; }

        public bool IsHandled { get; set; }

        public bool Alt => (Mod & SDL_Keymod.KMOD_ALT) != SDL_Keymod.KMOD_NONE;

        public bool Control => (Mod & SDL_Keymod.KMOD_CTRL) != SDL_Keymod.KMOD_NONE;

        public bool Shift => (Mod & SDL_Keymod.KMOD_SHIFT) != SDL_Keymod.KMOD_NONE;
    }
}