using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static SDL2.SDL;

namespace ClassicUO.Input
{

    public class InputManager : IDisposable
    {
        const int MOUSE_DRAG_BEGIN_DISTANCE = 2;
        const int MOUSE_CLICK_MAX_DELTA = 2;
        const int MOUSE_DOUBLE_CLICK_TIME = 200;


        private SDL_EventFilter _hookDel;
        private Queue<InputEvent> _events = new Queue<InputEvent>();
        private Queue<InputEvent> _nextEvents = new Queue<InputEvent>();
        private SDL_Keycode _lastKey;
        private bool _mouseIsDragging;
        private InputMouseEvent _lastMouseDown, _lastMouseClick;
        private float _time = -1f;
        private float _lastMouseDownTime, _lastMouseClickTime;


        public Point MousePosition { get; private set; }

        public InputManager()
        {
            _hookDel = new SDL_EventFilter(HookFunc);
            SDL_AddEventWatch(_hookDel, IntPtr.Zero);
        }


        public void Dispose()
        {
            SDL_DelEventWatch(_hookDel, IntPtr.Zero);
        }


        public IEnumerable<InputKeyboardEvent> GetKeyboardEvents()
        {
            return _events.Where(s => s is InputKeyboardEvent e && !e.IsHandled).Cast<InputKeyboardEvent>();
        }

        public IEnumerable<InputMouseEvent> GetMouseEvents()
        {
            return _events.Where(s => s is InputMouseEvent e && !e.IsHandled).Cast<InputMouseEvent>();
        }

        public void Update(float totaltime)
        {
            _time = totaltime;

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
            {
                AddEvent(new InputKeyboardEvent(KeyboardEvent.Press, e));
            }
            else
            {
                _lastKey = e.KeyCode;
                AddEvent(e);
            }
        }

        private void OnKeyUp(InputKeyboardEvent e)
        {
            _lastKey = SDL_Keycode.SDLK_UNKNOWN;
            AddEvent(e);
        }

        private void OnTextInput(InputKeyboardEvent e)
        {
            AddEvent(e);
        }

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

                        if (_time - _lastMouseClickTime <= MOUSE_DOUBLE_CLICK_TIME && _lastMouseClick != null && !DistanceBetweenPoints(_lastMouseClick.Position, e.Position, MOUSE_CLICK_MAX_DELTA))
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
            SDL_Event* e = (SDL_Event*)ev;


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
                    string s = Marshal.PtrToStringUTF8((IntPtr)e->text.text);
                    if (!string.IsNullOrEmpty(s))
                        OnTextInput(new InputKeyboardEvent(KeyboardEvent.TextInput, SDL_Keycode.SDLK_UNKNOWN, 0, SDL_Keymod.KMOD_NONE) { KeyChar = s[0] });
                    break;


                // MOUSE
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    OnMouseDown(new InputMouseEvent(MouseEvent.Down, CovertMouseButton(e->button.button), e->button.clicks, e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    OnMouseUp(new InputMouseEvent(MouseEvent.Up, CovertMouseButton(e->button.button), e->button.clicks, e->button.x, e->button.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEMOTION:
                    OnMouseMove(new InputMouseEvent(MouseEvent.Move, MouseButtons.None, 0, e->motion.x, e->motion.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
                case SDL_EventType.SDL_MOUSEWHEEL:
                    OnMouseWheel(new InputMouseEvent(MouseEvent.WheelScroll, MouseButtons.Middle, 0, e->wheel.x, e->wheel.y, 0, SDL_Keymod.KMOD_NONE));
                    break;
            }

            return 1;
        }

        private MouseButtons CovertMouseButton(byte button)
        {
            switch (button)
            {
                case 1: return MouseButtons.Left;
                case 2: return MouseButtons.Middle;
                case 3: return MouseButtons.Right;
                case 4: return MouseButtons.XButton1;
                case 5: return MouseButtons.XButton2;
                default: return MouseButtons.None;
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
