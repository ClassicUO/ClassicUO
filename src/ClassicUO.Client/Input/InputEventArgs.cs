// SPDX-License-Identifier: BSD-2-Clause

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static SDL2.SDL;

namespace ClassicUO.Input
{
    internal sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, MouseButtonType button = MouseButtonType.None, ButtonState state = ButtonState.Released)
        {
            Location = new Point(x, y);
            Button = button;
            ButtonState = state;
        }

        public Point Location { get; }

        public int X => Location.X;

        public int Y => Location.Y;

        public MouseButtonType Button { get; }

        public ButtonState ButtonState { get; }
    }

    internal sealed class MouseDoubleClickEventArgs : EventArgs
    {
        public MouseDoubleClickEventArgs(int x, int y, MouseButtonType button)
        {
            Location = new Point(x, y);
            Button = button;
        }

        public Point Location { get; }

        public int X => Location.X;

        public int Y => Location.Y;

        public MouseButtonType Button { get; }

        public bool Result { get; set; }
    }

    internal sealed class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(MouseEventType direction)
        {
            if (direction != MouseEventType.WheelScroll && direction != MouseEventType.WheelScrollDown && direction != MouseEventType.WheelScrollUp)
            {
                throw new Exception("Wrong scroll direction: " + direction);
            }

            Direction = direction;
        }

        public MouseEventType Direction { get; }
    }

    internal sealed class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(SDL_Keycode key, SDL_Keymod mod, KeyboardEventType state)
        {
            Key = key;
            Mod = mod;
            KeyboardEvent = state;
        }

        public SDL_Keycode Key { get; }

        public SDL_Keymod Mod { get; }

        public KeyboardEventType KeyboardEvent { get; }
    }
}