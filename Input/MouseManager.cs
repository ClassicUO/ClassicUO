#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClassicUO.Input
{       
    public static class InputManager
    {
        private static SDL2.SDL.SDL_EventFilter _hookDel;
        public static void Initialize()
        {
            _hookDel = new SDL2.SDL.SDL_EventFilter(HookFunc);

            SDL2.SDL.SDL_AddEventWatch(_hookDel, IntPtr.Zero);
        }

        private static unsafe int HookFunc(IntPtr userdata, IntPtr ev)
        {
            SDL2.SDL.SDL_Event* e = (SDL2.SDL.SDL_Event*)ev;

            switch(e->type)
            {
                case SDL2.SDL.SDL_EventType.SDL_KEYDOWN:
                    break;
                case SDL2.SDL.SDL_EventType.SDL_KEYUP:
                    break;


                case SDL2.SDL.SDL_EventType.SDL_TEXTINPUT:
                    Console.WriteLine(Marshal.PtrToStringAnsi((IntPtr)e->text.text));
                    break;
            }


            return 1;
        }
    }

    public class MouseManager
    {
        private MouseState _prevMouseState;


        public Point ScreenPosition { get; private set; }


        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MousePressed;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;

        public MouseManager()
        {
            _prevMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
        }

        
        public void Update()
        {
            MouseState current = Microsoft.Xna.Framework.Input.Mouse.GetState();

            if (IsMouseButtonDown(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Left, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Right, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Middle, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton1, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton2, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }


            if (IsMouseButtonUp(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Left, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Right, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Middle, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton1, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton2, ButtonState.Released);
                MouseUp.Raise(arg);
            }


            if (IsMouseButtonPressed(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Left, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Right, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Middle, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton1, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton2, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }


            if (current.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                MouseWheelEventArgs arg = new MouseWheelEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, current.ScrollWheelValue == 0 ? WheelDirection.None : current.ScrollWheelValue > 0 ? WheelDirection.Up : WheelDirection.Down);
                MouseWheel.Raise(arg);
            }

            if (current.X != _prevMouseState.X || current.Y != _prevMouseState.Y)
            {
                ScreenPosition = new Point(current.X, current.Y);
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y);
                MouseMove.Raise(arg);
            }

            _prevMouseState = current;
        }


        private bool IsMouseButtonDown(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Released;
        }

        private bool IsMouseButtonUp(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Released && prev == ButtonState.Pressed;
        }

        private bool IsMouseButtonPressed(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Pressed;
        }
    }
}