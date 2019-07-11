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

using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using static SDL2.SDL;

namespace ClassicUO.Input
{
    internal sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, MouseButton button = MouseButton.None, ButtonState state = ButtonState.Released)
        {
            Location = new Point(x, y);
            Button = button;
            ButtonState = state;
        }

        public Point Location { get; }

        public int X => Location.X;

        public int Y => Location.Y;

        public MouseButton Button { get; }

        public ButtonState ButtonState { get; }
    }

    internal sealed class MouseDoubleClickEventArgs : EventArgs
    {
        public MouseDoubleClickEventArgs(int x, int y, MouseButton button)
        {
            Location = new Point(x, y);
            Button = button;
        }

        public Point Location { get; }

        public int X => Location.X;

        public int Y => Location.Y;

        public MouseButton Button { get; }

        public bool Result { get; set; }
    }

    internal sealed class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(MouseEvent direction)
        {
            if (direction != MouseEvent.WheelScroll && direction != MouseEvent.WheelScrollDown && direction != MouseEvent.WheelScrollUp)
                throw new Exception("Wrong scroll direction: " + direction);

            Direction = direction;
        }

        public MouseEvent Direction { get; }
    }

    internal sealed class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(SDL_Keycode key, SDL_Keymod mod, KeyboardEvent state)
        {
            Key = key;
            Mod = mod;
            KeyboardEvent = state;
        }

        public SDL_Keycode Key { get; }

        public SDL_Keymod Mod { get; }

        public KeyboardEvent KeyboardEvent { get; }
    }
}