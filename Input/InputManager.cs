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
    public class InputManager : IDisposable
    {
        //public void Update(double totalMS, double frameMS)
        //{
        //    _time = (float) totalMS;

        //    lock (_nextEvents)
        //    {
        //        _events.Clear();

        //        while (_nextEvents.Count > 0)
        //            _events.Enqueue(_nextEvents.Dequeue());
        //    }
        //}

        //public IEnumerable<InputKeyboardEvent> GetKeyboardEvents()
        //{
        //    return _events.Where(s => s is InputKeyboardEvent e && !e.IsHandled).Cast<InputKeyboardEvent>();
        //}

        //public IEnumerable<InputMouseEvent> GetMouseEvents()
        //{
        //    return _events.Where(s => s is InputMouseEvent e && !e.IsHandled).Cast<InputMouseEvent>();
        //}

        //public bool HandleMouseEvent(MouseEvent type, MouseButton button)
        //{
        //    foreach (InputEvent e in _events)
        //        if (!e.IsHandled && e is InputMouseEvent me && me.EventType == type && me.Button == button)
        //        {
        //            e.IsHandled = true;

        //            return true;
        //        }

        //    return false;
        //}

        //public bool HandleKeybaordEvent(KeyboardEvent type, SDL_Keycode key, bool shift, bool alt, bool ctrl)
        //{
        //    foreach (InputEvent e in _events)
        //        if (!e.IsHandled && e is InputKeyboardEvent ke && ke.EventType == type && ke.KeyCode == key && ke.Shift == shift && ke.Alt == alt && ke.Control == ctrl)
        //        {
        //            e.IsHandled = true;

        //            return true;
        //        }

        //    return false;
        //}

        //private void OnKeyDown(InputKeyboardEvent e)
        //{
        //    if (_lastKey == SDL_Keycode.SDLK_LCTRL && e.KeyCode == SDL_Keycode.SDLK_v)
        //    {
        //        OnTextInput(new InputKeyboardEvent(KeyboardEvent.TextInput, SDL_Keycode.SDLK_UNKNOWN, 0, SDL_Keymod.KMOD_NONE) {KeyChar = SDL_GetClipboardText()});
        //    }
        //    else
        //    {
        //        if (_lastKey == e.KeyCode && _lastKey != SDL_Keycode.SDLK_UNKNOWN || e.EventType == KeyboardEvent.Down) AddEvent(new InputKeyboardEvent(KeyboardEvent.Press, e));
        //        _lastKey = e.KeyCode;
        //        AddEvent(e);
        //    }
        //}

        //private void OnKeyUp(InputKeyboardEvent e)
        //{
        //    _lastKey = SDL_Keycode.SDLK_UNKNOWN;
        //    AddEvent(e);
        //}

        //private void OnTextInput(InputKeyboardEvent e)
        //{
        //    AddEvent(e);
        //}

        //private void OnMouseDown(InputMouseEvent e)
        //{
        //    if (e.Button == MouseButton.Left)
        //    {
        //        _leftButtonPressed = true;
        //        //LeftDragPosition = MousePosition;
        //    }

        //    _lastMouseDown = e;
        //    _lastMouseDownTime = _time;
        //    AddEvent(e);
        //}

        //private void OnMouseUp(InputMouseEvent e)
        //{
        //    if (_mouseIsDragging)
        //    {
        //        AddEvent(new InputMouseEvent(MouseEvent.DragEnd, e));
        //        _mouseIsDragging = false;
        //    }
        //    else
        //    {
        //        if (_lastMouseDown != null)
        //            if (!DistanceBetweenPoints(_lastMouseDown.Position, e.Position, MOUSE_CLICK_MAX_DELTA))
        //            {
        //                AddEvent(new InputMouseEvent(MouseEvent.Click, e));

        //                if (_time - _lastMouseClickTime <= MOUSE_DOUBLE_CLICK_TIME && _lastMouseClick != null && !DistanceBetweenPoints(_lastMouseClick.Position, e.Position, MOUSE_CLICK_MAX_DELTA))
        //                {
        //                    _lastMouseClickTime = 0f;
        //                    AddEvent(new InputMouseEvent(MouseEvent.DoubleClick, e));
        //                }
        //                else
        //                {
        //                    _lastMouseClickTime = _time;
        //                    _lastMouseClick = e;
        //                }
        //            }
        //    }

        //    if (e.Button == MouseButton.Left)
        //        _leftButtonPressed = false;
        //    AddEvent(new InputMouseEvent(MouseEvent.Up, e));
        //    _lastMouseDown = null;
        //}

        //private void OnMouseMove(InputMouseEvent e)
        //{
        //    //MousePosition = e.Position;
        //    AddEvent(new InputMouseEvent(MouseEvent.Move, e));

        //    if (!_mouseIsDragging && _lastMouseDown != null)
        //        if (DistanceBetweenPoints(_lastMouseDown.Position, e.Position, MOUSE_DRAG_BEGIN_DISTANCE))
        //        {
        //            AddEvent(new InputMouseEvent(MouseEvent.DragBegin, e));
        //            _mouseIsDragging = true;
        //        }
        //}

        //private void OnMouseWheel(InputMouseEvent e)
        //{
        //    AddEvent(e);
        //    AddEvent(new InputMouseEvent(ConvertWheelDirection(e.X, e.Y), e));
        //}

        //private void AddEvent(InputEvent e)
        //{
        //    _nextEvents.Enqueue(e);
        //}

        //private static bool DistanceBetweenPoints(Point initial, Point final, int distance)
        //{
        //    return Math.Abs(final.X - initial.X) + Math.Abs(final.Y - initial.Y) > distance;
        //}

        public delegate bool DoubleClickDelegate();

        private static bool _dragStarted;
        //private const int MOUSE_DRAG_BEGIN_DISTANCE = 2;
        //private const int MOUSE_CLICK_MAX_DELTA = 2;
        //public const int MOUSE_DOUBLE_CLICK_TIME = 350;

        //private readonly Queue<InputEvent> _events = new Queue<InputEvent>();
        private readonly SDL_EventFilter _hookDel;
        //private readonly Queue<InputEvent> _nextEvents = new Queue<InputEvent>();
        //private SDL_Keycode _lastKey;
        //private InputMouseEvent _lastMouseDown, _lastMouseClick;
        //private float _lastMouseDownTime, _lastMouseClickTime;
        //private bool _leftButtonPressed;
        //private bool _mouseIsDragging;
        //private float _time = -1f;

        public InputManager()
        {
            _hookDel = HookFunc;
            SDL_AddEventWatch(_hookDel, IntPtr.Zero);
        }

        //public Point MousePosition { get; private set; }

        //public Point LeftDragPosition { get; private set; }

        //public Point Offset => _leftButtonPressed ? MousePosition - LeftDragPosition : Point.Zero;

        public void Dispose()
        {
            SDL_DelEventWatch(_hookDel, IntPtr.Zero);
        }

        public static event DoubleClickDelegate LeftMouseDoubleClick, MidMouseDoubleClick, RightMouseDoubleClick;

        public static event EventHandler LeftMouseButtonDown, LeftMouseButtonUp, MidMouseButtonDown, MidMouseButtonUp, RightMouseButtonDown, RightMouseButtonUp, X1MouseButtonDown, X1MouseButtonUp, X2MouseButtonDown, X2MouseButtonUp;

        public static event EventHandler<bool> MouseWheel;

        public static event EventHandler MouseDragging, DragBegin, DragEnd;

        public static event EventHandler<SDL_KeyboardEvent> KeyDown, KeyUp;

        public static event EventHandler<string> TextInput;

        private unsafe int HookFunc(IntPtr userdata, IntPtr ev)
        {
            SDL_Event* e = (SDL_Event*) ev;

            switch (e->type)
            {
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

                                    if (LeftMouseDoubleClick != null && !LeftMouseDoubleClick.Invoke())
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
                                    if (MidMouseDoubleClick != null && !MidMouseDoubleClick.Invoke())
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

                                    if (RightMouseDoubleClick != null && !RightMouseDoubleClick.Invoke())
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

            //switch (e->type)
            //{
            //    // KEYBOARD
            //    case SDL_EventType.SDL_KEYDOWN:
            //        OnKeyDown(new InputKeyboardEvent(KeyboardEvent.Down, e->key.keysym.sym, 0, e->key.keysym.mod));

            //        break;
            //    case SDL_EventType.SDL_KEYUP:
            //        OnKeyUp(new InputKeyboardEvent(KeyboardEvent.Up, e->key.keysym.sym, 0, e->key.keysym.mod));

            //        break;
            //    case SDL_EventType.SDL_TEXTINPUT:
            //        string s = StringHelper.ReadUTF8(e->text.text);

            //        if (!string.IsNullOrEmpty(s))
            //            OnTextInput(new InputKeyboardEvent(KeyboardEvent.TextInput, SDL_Keycode.SDLK_UNKNOWN, 0, SDL_Keymod.KMOD_NONE) {KeyChar = s});

            //        break;

            //    // MOUSE
            //    case SDL_EventType.SDL_MOUSEBUTTONDOWN:
            //        MouseDown.Raise();
            //        OnMouseDown(new InputMouseEvent(MouseEvent.Down, CovertMouseButton(e->button.button), e->button.clicks, e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));

            //        break;
            //    case SDL_EventType.SDL_MOUSEBUTTONUP:
            //        MouseUp.Raise();
            //        OnMouseUp(new InputMouseEvent(MouseEvent.Up, CovertMouseButton(e->button.button), e->button.clicks, e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));

            //        switch (e->button.clicks)
            //        {
            //            case 1:
            //                MouseClick.Raise();
            //                break;
            //            case 2:
            //                MouseDoubleClick.Raise();
            //                break;
            //        }

            //        break;
            //    case SDL_EventType.SDL_MOUSEMOTION:
            //        MouseMove.Raise();
            //        OnMouseMove(new InputMouseEvent(MouseEvent.Move, CovertMouseButton(e->button.button), 0, e->motion.x, e->motion.y, 0, SDL_Keymod.KMOD_NONE));

            //        break;
            //    case SDL_EventType.SDL_MOUSEWHEEL:
            //        OnMouseWheel(new InputMouseEvent(MouseEvent.WheelScroll, MouseButton.Middle, 0, e->wheel.x, e->wheel.y, 0, SDL_Keymod.KMOD_NONE));

            //        break;
            //}

            return 0;
        }

        //private static MouseButton CovertMouseButton(byte button)
        //{
        //    switch (button)
        //    {
        //        case 1:

        //            return MouseButton.Left;
        //        case 2:

        //            return MouseButton.Middle;
        //        case 3:

        //            return MouseButton.Right;
        //        case 4:

        //            return MouseButton.XButton1;
        //        case 5:

        //            return MouseButton.XButton2;
        //        default:

        //            return MouseButton.None;
        //    }
        //}

        //private static MouseEvent ConvertWheelDirection(int x, int y)
        //{
        //    MouseEvent dir = MouseEvent.WheelScroll;

        //    if (y > 0)
        //        dir = MouseEvent.WheelScrollUp;
        //    else if (y < 0)
        //        dir = MouseEvent.WheelScrollDown;

        //    if (x > 0)
        //        dir = MouseEvent.Right;
        //    else if (x < 0)
        //        dir = MouseEvent.Left;

        //    return dir;
        //}
    }
}