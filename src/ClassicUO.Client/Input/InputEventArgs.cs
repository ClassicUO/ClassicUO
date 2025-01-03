#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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