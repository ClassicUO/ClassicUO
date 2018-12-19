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
using System;

using ClassicUO.Utility;

using static SDL2.SDL;

namespace ClassicUO.Input
{
    public sealed class InputManager : IDisposable
    {
        private bool _dragStarted;
        private readonly SDL_EventFilter _hookDel;

        public InputManager()
        {
            _hookDel = HookFunc;
            SDL_AddEventWatch(_hookDel, IntPtr.Zero);
        }

        public bool IsDisposed { get; private set; }


        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            SDL_DelEventWatch(_hookDel, IntPtr.Zero);
        }


        public event EventHandler<MouseDoubleClickEventArgs> LeftMouseDoubleClick, MidMouseDoubleClick, RightMouseDoubleClick;

        public event EventHandler LeftMouseButtonDown, LeftMouseButtonUp, MidMouseButtonDown, MidMouseButtonUp, RightMouseButtonDown, RightMouseButtonUp, X1MouseButtonDown, X1MouseButtonUp, X2MouseButtonDown, X2MouseButtonUp;

        public event EventHandler<bool> MouseWheel;

        public event EventHandler MouseMoving, MouseDragging, DragBegin, DragEnd;

        public event EventHandler<SDL_KeyboardEvent> KeyDown, KeyUp;

        public event EventHandler<string> TextInput;

        private unsafe int HookFunc(IntPtr userdata, IntPtr ev)
        {
            SDL_Event* e = (SDL_Event*) ev;

            switch (e->type)
            {
                case SDL_EventType.SDL_WINDOWEVENT:

                    switch (e->window.windowEvent)
                    {
                        case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                            Mouse.MouseInWindow = true;

                            break;
                        case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                            Mouse.MouseInWindow = false;

                            break;
                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:

                            // SDL_CaptureMouse(SDL_bool.SDL_TRUE);
                            //Log.Message(LogTypes.Debug, "FOCUS");
                            break;
                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            //Log.Message(LogTypes.Debug, "NO FOCUS");
                            //SDL_CaptureMouse(SDL_bool.SDL_FALSE);

                            break;
                        case SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS:

                            //Log.Message(LogTypes.Debug, "TAKE FOCUS");
                            break;
                        case SDL_WindowEventID.SDL_WINDOWEVENT_HIT_TEST:

                            break;
                    }

                    break;
                case SDL_EventType.SDL_SYSWMEVENT:

                    break;
                case SDL_EventType.SDL_KEYDOWN:
                    KeyDown?.Raise(e->key);

                    break;
                case SDL_EventType.SDL_KEYUP:
                    KeyUp.Raise(e->key);

                    break;
                case SDL_EventType.SDL_TEXTINPUT:
                    string s = StringHelper.ReadUTF8(e->text.text);

                    if (!string.IsNullOrEmpty(s))
                        TextInput.Raise(s);

                    break;
                case SDL_EventType.SDL_MOUSEMOTION:
                    Mouse.Update();
                    MouseMoving.Raise();
                    if (Mouse.IsDragging) MouseDragging.Raise();

                    if (Mouse.IsDragging && !_dragStarted)
                    {
                        DragBegin.Raise();
                        _dragStarted = true;
                    }

                    break;
                case SDL_EventType.SDL_MOUSEWHEEL:
                    Mouse.Update();
                    bool isup = e->wheel.y > 0;
                    MouseWheel.Raise(isup);

                    break;
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    Mouse.Update();
                    bool isDown = e->type == SDL_EventType.SDL_MOUSEBUTTONDOWN;

                    if (_dragStarted && !isDown)
                    {
                        DragEnd.Raise();
                        _dragStarted = false;
                    }

                    SDL_MouseButtonEvent mouse = e->button;

                    switch ((uint) mouse.button)
                    {
                        case SDL_BUTTON_LEFT:
                            Mouse.LButtonPressed = isDown;

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.LDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastLeftButtonClickTime = 0;

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Left);

                                    LeftMouseDoubleClick.Raise(arg);

                                    if (!arg.Result)
                                        LeftMouseButtonDown.Raise();
                                    else
                                        Mouse.LastLeftButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                LeftMouseButtonDown.Raise();
                                Mouse.LastLeftButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF)
                                    LeftMouseButtonUp.Raise();
                                Mouse.End();
                            }

                            break;
                        case SDL_BUTTON_MIDDLE:
                            Mouse.MButtonPressed = isDown;

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.MDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastMidButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {                                  
                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Middle);

                                    MidMouseDoubleClick.Raise(arg);

                                    if (!arg.Result)
                                        MidMouseButtonDown.Raise();
                                    Mouse.LastMidButtonClickTime = 0;

                                    break;
                                }

                                MidMouseButtonDown.Raise();
                                Mouse.LastMidButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                MidMouseButtonUp.Raise();
                                Mouse.End();
                            }

                            break;
                        case SDL_BUTTON_RIGHT:
                            Mouse.RButtonPressed = isDown;

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.RDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastRightButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastRightButtonClickTime = 0;

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Middle);

                                    RightMouseDoubleClick.Raise(arg);

                                    if (!arg.Result)
                                        RightMouseButtonDown.Raise();
                                    else
                                        Mouse.LastRightButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                RightMouseButtonDown.Raise();
                                Mouse.LastRightButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (Mouse.LastRightButtonClickTime != 0xFFFF_FFFF)
                                    RightMouseButtonUp.Raise();
                                Mouse.End();
                            }

                            break;
                        case SDL_BUTTON_X1:

                            break;
                        case SDL_BUTTON_X2:

                            break;
                    }

                    break;
            }      
            return 1;
        }
    }
}