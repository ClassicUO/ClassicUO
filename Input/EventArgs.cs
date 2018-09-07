#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClassicUO.Input
{
    public enum MouseButton 
    {
        None,
        Left,
        Middle,
        Right,
        XButton1,
        XButton2
    }

    public enum WheelDirection
    {
        None,
        Up,
        Down
    }

    public sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, int offx, int offy, MouseButton button = MouseButton.None, ButtonState state = ButtonState.Released)
        {
            Location = new Point(x, y);
            Button = button;
            ButtonState = state;
            Offset = new Point(x - offx, y - offy);
        }

        public Point Location { get; }
        public Point Offset { get; }
        public MouseButton Button { get; }
        public ButtonState ButtonState { get; }
    }

    public sealed class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(int x, int y, int offx, int offy, WheelDirection direction)
        {
            Location = new Point(x, y);
            Direction = direction;
            Offset = new Point(x - offx, y - offy);
        }

        public Point Location { get; }
        public Point Offset { get; }
        public WheelDirection Direction { get; }
    }

    public sealed class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(Keys key, KeyState state)
        {
            Key = key;
            KeyState = state;
        }

        public Keys Key { get; }
        public KeyState KeyState { get; }
    }

    public sealed class ButtonClickEventArgs : EventArgs
    {
    }
}