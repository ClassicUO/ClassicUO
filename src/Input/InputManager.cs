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

using ClassicUO.Network;
using ClassicUO.Utility;

using static SDL2.SDL;

namespace ClassicUO.Input
{
    internal sealed class InputManager : IDisposable
    {
        //private readonly SDL_EventFilter _hookDel;
        private bool _dragStarted;

        private bool _ignoreNextTextInput;

        public bool IsDisposed { get; private set; }


        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            //SDL_DelEventWatch(_hookDel, IntPtr.Zero);
        }


        public event EventHandler<MouseDoubleClickEventArgs> LeftMouseDoubleClick, MidMouseDoubleClick, RightMouseDoubleClick;

        public event EventHandler LeftMouseButtonDown, LeftMouseButtonUp, MidMouseButtonDown, MidMouseButtonUp, RightMouseButtonDown, RightMouseButtonUp, X1MouseButtonDown, X1MouseButtonUp, X2MouseButtonDown, X2MouseButtonUp;

        public event EventHandler<bool> MouseWheel;

        public event EventHandler MouseDragging, DragBegin, DragEnd;

        public event EventHandler<SDL_KeyboardEvent> KeyDown, KeyUp;

        public event EventHandler<string> TextInput;

        //private unsafe int HookFunc(IntPtr userdata, IntPtr ev)
        public unsafe void EventHandler(ref SDL_Event e)
        {
            // SDL_Event* e = (SDL_Event*) ev;

            switch (e.type)
            {
                case SDL_EventType.SDL_AUDIODEVICEADDED:
                    Console.WriteLine("AUDIO ADDED: {0}", e.adevice.which);

                    break;

                case SDL_EventType.SDL_AUDIODEVICEREMOVED:
                    Console.WriteLine("AUDIO REMOVED: {0}", e.adevice.which);

                    break;


                case SDL_EventType.SDL_WINDOWEVENT:

                    switch (e.window.windowEvent)
                    {
                        case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                            Mouse.MouseInWindow = true;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                            Mouse.MouseInWindow = false;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                            Plugin.OnFocusGained();

                            // SDL_CaptureMouse(SDL_bool.SDL_TRUE);
                            //Log.Message(LogTypes.Debug, "FOCUS");
                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            Plugin.OnFocusLost();
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

                    if (Plugin.ProcessHotkeys((int) e.key.keysym.sym, (int) e.key.keysym.mod, true))
                    {
                        _ignoreNextTextInput = false;
                        Engine.SceneManager.CurrentScene.OnKeyDown(e.key);

                        KeyDown?.Raise(e.key);
                    }
                    else
                        _ignoreNextTextInput = true;

                    break;

                case SDL_EventType.SDL_KEYUP:

                    Engine.SceneManager.CurrentScene.OnKeyUp(e.key);
                    KeyUp.Raise(e.key);

                    break;

                case SDL_EventType.SDL_TEXTINPUT:

                    if (_ignoreNextTextInput)
                        break;

                    fixed (SDL_Event* ev = &e)
                    {
                        string s = StringHelper.ReadUTF8(ev->text.text);

                        if (!string.IsNullOrEmpty(s))
                        {
                            Engine.SceneManager.CurrentScene.OnTextInput(s);
                            TextInput.Raise(s);
                        }

                    }

                    break;

                case SDL_EventType.SDL_MOUSEMOTION:
                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        Engine.SceneManager.CurrentScene.OnMouseDragging();
                        MouseDragging.Raise();
                    }

                    if (Mouse.IsDragging && !_dragStarted)
                    {
                        DragBegin.Raise();
                        _dragStarted = true;
                    }

                    break;

                case SDL_EventType.SDL_MOUSEWHEEL:
                    Mouse.Update();
                    bool isup = e.wheel.y > 0;

                    Plugin.ProcessMouse(0, e.wheel.y);
                    Engine.SceneManager.CurrentScene.OnMouseWheel(isup);
                    MouseWheel.Raise(isup);

                    break;

                case SDL_EventType.SDL_MOUSEBUTTONUP:
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    Mouse.Update();
                    bool isDown = e.type == SDL_EventType.SDL_MOUSEBUTTONDOWN;
                    bool resetTime = false;

                    if (_dragStarted && !isDown)
                    {
                        DragEnd.Raise();
                        _dragStarted = false;
                        resetTime = true;
                    }

                    SDL_MouseButtonEvent mouse = e.button;

                    switch ((uint) mouse.button)
                    {
                        case SDL_BUTTON_LEFT:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.LButtonPressed = true;
                                Mouse.LDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastLeftButtonClickTime = 0;

                                    var res = Engine.SceneManager.CurrentScene.OnLeftMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Left);

                                    LeftMouseDoubleClick.Raise(arg);

                                    if (!arg.Result && !res)
                                    {
                                        Engine.SceneManager.CurrentScene.OnLeftMouseDown();
                                        LeftMouseButtonDown.Raise();
                                    }
                                    else
                                        Mouse.LastLeftButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                Engine.SceneManager.CurrentScene.OnLeftMouseDown();
                                LeftMouseButtonDown.Raise();
                                Mouse.LastLeftButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastLeftButtonClickTime = 0;

                                if (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF)
                                {
                                    Engine.SceneManager.CurrentScene.OnLeftMouseUp();
                                    LeftMouseButtonUp.Raise();
                                }
                                Mouse.LButtonPressed = false;
                                Mouse.End();

                                Mouse.LastClickPosition = Mouse.Position;
                            }

                            break;

                        case SDL_BUTTON_MIDDLE:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.MButtonPressed = true;
                                Mouse.MDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastMidButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastMidButtonClickTime = 0;
                                    var res = Engine.SceneManager.CurrentScene.OnMiddleMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Middle);

                                    MidMouseDoubleClick.Raise(arg);

                                    if (!arg.Result && !res)
                                    {
                                        Engine.SceneManager.CurrentScene.OnMiddleMouseDown();

                                        MidMouseButtonDown.Raise();
                                    }

                                    break;
                                }

                                Plugin.ProcessMouse(e.button.button, 0);

                                Engine.SceneManager.CurrentScene.OnMiddleMouseDown();
                                MidMouseButtonDown.Raise();
                                Mouse.LastMidButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                MidMouseButtonUp.Raise();
                                Mouse.MButtonPressed = false;
                                Mouse.End();
                            }

                            break;

                        case SDL_BUTTON_RIGHT:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.RButtonPressed = true;
                                Mouse.RDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastRightButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastRightButtonClickTime = 0;

                                    var res = Engine.SceneManager.CurrentScene.OnRightMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Right);

                                    RightMouseDoubleClick.Raise(arg);

                                    if (!arg.Result && !res)
                                    {
                                        Engine.SceneManager.CurrentScene.OnRightMouseDown();
                                        RightMouseButtonDown.Raise();
                                    }
                                    else
                                        Mouse.LastRightButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                Engine.SceneManager.CurrentScene.OnRightMouseDown();
                                RightMouseButtonDown.Raise();
                                Mouse.LastRightButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastRightButtonClickTime = 0;

                                if (Mouse.LastRightButtonClickTime != 0xFFFF_FFFF)
                                {
                                    Engine.SceneManager.CurrentScene.OnRightMouseUp();
                                    RightMouseButtonUp.Raise();
                                }
                                Mouse.RButtonPressed = false;
                                Mouse.End();
                            }

                            break;

                        case SDL_BUTTON_X1:

                            if (isDown)
                                Plugin.ProcessMouse(e.button.button, 0);

                            break;

                        case SDL_BUTTON_X2:

                            if (isDown)
                                Plugin.ProcessMouse(e.button.button, 0);

                            break;
                    }

                    break;
            }
        }
    }
}