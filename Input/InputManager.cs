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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using static SDL2.SDL;
using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Input
{
    public class InputManager : IDisposable, IUpdateable
    {
        private const int MOUSE_DRAG_BEGIN_DISTANCE = 2;
        private const int MOUSE_CLICK_MAX_DELTA = 2;
        private const int MOUSE_DOUBLE_CLICK_TIME = 200;


        private readonly SDL_EventFilter _hookDel;
        private readonly Queue<InputEvent> _events = new Queue<InputEvent>();
        private readonly Queue<InputEvent> _nextEvents = new Queue<InputEvent>();
        private SDL_Keycode _lastKey;
        private bool _mouseIsDragging;
        private InputMouseEvent _lastMouseDown, _lastMouseClick;
        private float _time = -1f;
        private float _lastMouseDownTime, _lastMouseClickTime;


        public Point MousePosition { get; private set; }

        public InputManager()
        {
            _hookDel = HookFunc;
            SDL_AddEventWatch(_hookDel, IntPtr.Zero);
        }


        public void Dispose() => SDL_DelEventWatch(_hookDel, IntPtr.Zero);

        public IEnumerable<InputKeyboardEvent> GetKeyboardEvents() =>
            _events.Where(s => s is InputKeyboardEvent e && !e.IsHandled).Cast<InputKeyboardEvent>();

        public IEnumerable<InputMouseEvent> GetMouseEvents() =>
            _events.Where(s => s is InputMouseEvent e && !e.IsHandled).Cast<InputMouseEvent>();

        public bool HandleMouseEvent(MouseEvent type, MouseButton button)
        {
            foreach (InputEvent e in _events)
            {
                if (!e.IsHandled && e is InputMouseEvent me && me.EventType == type && me.Button == button)
                {
                    e.IsHandled = true;
                    return true;
                }
            }

            return false;
        }

        public bool HandleKeybaordEvent(KeyboardEvent type, SDL_Keycode key, bool shift, bool alt, bool ctrl)
        {
            foreach (InputEvent e in _events)
            {
                if (!e.IsHandled && e is InputKeyboardEvent ke && ke.EventType == type && ke.KeyCode == key &&
                    ke.Shift == shift && ke.Alt == alt && ke.Control == ctrl)
                {
                    e.IsHandled = true;
                    return true;
                }
            }

            return false;
        }

        public void Update(double totalMS, double frameMS)
        {
            _time = (float) totalMS;

            lock (_nextEvents)
            {
                _events.Clear();

                while (_nextEvents.Count > 0)
                    _events.Enqueue(_nextEvents.Dequeue());
            }
        }

        private void OnKeyDown(InputKeyboardEvent e)
        {
            if (_lastKey == e.KeyCode && _lastKey != SDL_Keycode.SDLK_UNKNOWN)
                AddEvent(new InputKeyboardEvent(KeyboardEvent.Press, e));
            else
            {
                if (_lastKey == SDL_Keycode.SDLK_LCTRL && e.KeyCode == SDL_Keycode.SDLK_v)
                {
                    OnTextInput(
                        new InputKeyboardEvent(KeyboardEvent.TextInput, SDL_Keycode.SDLK_UNKNOWN, 0,
                            SDL_Keymod.KMOD_NONE) {KeyChar = SDL_GetClipboardText()});
                }
                else
                {
                    _lastKey = e.KeyCode;
                    AddEvent(e);
                }
            }
        }

        private void OnKeyUp(InputKeyboardEvent e)
        {
            _lastKey = SDL_Keycode.SDLK_UNKNOWN;
            AddEvent(e);
        }

        private void OnTextInput(InputKeyboardEvent e) => AddEvent(e);

        private void OnMouseDown(InputMouseEvent e)
        {
            _lastMouseDown = e;
            _lastMouseDownTime = _time;
            AddEvent(e);
        }

        private void OnMouseUp(InputMouseEvent e)
        {
            if (_mouseIsDragging)
            {
                AddEvent(new InputMouseEvent(MouseEvent.DragEnd, e));
                _mouseIsDragging = false;
            }
            else
            {
                if (_lastMouseDown != null)
                {
                    if (!DistanceBetweenPoints(_lastMouseDown.Position, e.Position, MOUSE_CLICK_MAX_DELTA))
                    {
                        AddEvent(new InputMouseEvent(MouseEvent.Click, e));

                        if (_time - _lastMouseClickTime <= MOUSE_DOUBLE_CLICK_TIME && _lastMouseClick != null &&
                            !DistanceBetweenPoints(_lastMouseClick.Position, e.Position, MOUSE_CLICK_MAX_DELTA))
                        {
                            _lastMouseClickTime = 0f;
                            AddEvent(new InputMouseEvent(MouseEvent.DoubleClick, e));
                        }
                        else
                        {
                            _lastMouseClickTime = _time;
                            _lastMouseClick = e;
                        }
                    }
                }
            }

            AddEvent(new InputMouseEvent(MouseEvent.Up, e));
            _lastMouseDown = null;
        }

        private void OnMouseMove(InputMouseEvent e)
        {
            MousePosition = e.Position;

            AddEvent(new InputMouseEvent(MouseEvent.Move, e));

            if (!_mouseIsDragging && _lastMouseDown != null)
            {
                if (DistanceBetweenPoints(_lastMouseDown.Position, e.Position, MOUSE_DRAG_BEGIN_DISTANCE))
                {
                    AddEvent(new InputMouseEvent(MouseEvent.DragBegin, e));
                    _mouseIsDragging = true;
                }
            }
        }

        private void OnMouseWheel(InputMouseEvent e)
        {
            AddEvent(e);
            AddEvent(new InputMouseEvent(ConvertWheelDirection(e.X, e.Y), e));
        }


        private void AddEvent(InputEvent e) => _nextEvents.Enqueue(e);

        private bool DistanceBetweenPoints(Point initial, Point final, int distance)
            => Math.Abs(final.X - initial.X) + Math.Abs(final.Y - initial.Y) > distance;

        private unsafe int HookFunc(IntPtr userdata, IntPtr ev)
        {
            SDL_Event* e = (SDL_Event*) ev;


            switch (e->type)
            {
                // KEYBOARD
                case SDL_EventType.SDL_KEYDOWN:
                    OnKeyDown(new InputKeyboardEvent(KeyboardEvent.Down, e->key.keysym.sym, 0, e->key.keysym.mod));
                    break;
                case SDL_EventType.SDL_KEYUP:
                    OnKeyUp(new InputKeyboardEvent(KeyboardEvent.Up, e->key.keysym.sym, 0, e->key.keysym.mod));
                    break;
                case SDL_EventType.SDL_TEXTINPUT:
                    string s = Marshal.PtrToStringUTF8((IntPtr) e->text.text);
                    if (!string.IsNullOrEmpty(s))
                    {
                        OnTextInput(new InputKeyboardEvent(KeyboardEvent.TextInput, SDL_Keycode.SDLK_UNKNOWN, 0,
                            SDL_Keymod.KMOD_NONE) {KeyChar = s});
                    }

                    break;


                // MOUSE
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    OnMouseDown(new InputMouseEvent(MouseEvent.Down, CovertMouseButton(e->button.button),
                        e->button.clicks, e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    OnMouseUp(new InputMouseEvent(MouseEvent.Up, CovertMouseButton(e->button.button), e->button.clicks,
                        e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEMOTION:
                    OnMouseMove(new InputMouseEvent(MouseEvent.Move, CovertMouseButton(e->button.button), 0,
                        e->motion.x, e->motion.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEWHEEL:
                    OnMouseWheel(new InputMouseEvent(MouseEvent.WheelScroll, MouseButton.Middle, 0, e->wheel.x,
                        e->wheel.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
            }

            return 1;
        }

        private MouseButton CovertMouseButton(byte button)
        {
            switch (button)
            {
                case 1:
                    return MouseButton.Left;
                case 2:
                    return MouseButton.Middle;
                case 3:
                    return MouseButton.Right;
                case 4:
                    return MouseButton.XButton1;
                case 5:
                    return MouseButton.XButton2;
                default:
                    return MouseButton.None;
            }
        }

        private MouseEvent ConvertWheelDirection(int x, int y)
        {
            MouseEvent dir = MouseEvent.WheelScroll;

            if (y > 0)
                dir = MouseEvent.WheelScrollUp;
            else if (y < 0)
                dir = MouseEvent.WheelScrollDown;

            if (x > 0)
                dir = MouseEvent.Right;
            else if (x < 0)
                dir = MouseEvent.Left;

            return dir;
        }
    }
}