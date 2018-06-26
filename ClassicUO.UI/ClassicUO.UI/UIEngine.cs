using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.UI
{
    //http://www.sdltutorials.com/sdl-events

    public sealed class UIEngine
    {

        private MouseState _prevMouseState;
        private KeyboardState _prevKeyboardState;
        private Control _lastFocused;
        private Game _game;

        private bool _mouseLeftDown;

        public UIEngine(Game game)
        {
            _game = game;
            Controls = new List<Control>();
            _prevMouseState = Mouse.GetState();
            _prevKeyboardState = Keyboard.GetState();
        }

        public List<Control> Controls { get; }


        public void Initialize()
        {

        }


        public void Draw(GameTime time, SpriteBatch spriteBatch)
        {
            Controls.ForEach(s => s.Draw(time, spriteBatch));
        }

        public void Update(GameTime time)
        {
            /*while (SDL2.SDL.SDL_PollEvent(out var e) > 0)
            {
                CheckMouseEvents(e);
                CheckKeyboardEvents(e);
            }*/

            if (_game.IsActive)
            {
                CheckMouseEvents();
                CheckKeyboardEvents();
            }

            Controls.ForEach(s => s.Update(time));
        }

        private void CheckMouseEvents()
        {
            MouseState current = Mouse.GetState();


            if (IsMouseButtonDown(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Left, ButtonState.Pressed);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonDown(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Right, ButtonState.Pressed);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonDown(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Middle, ButtonState.Pressed);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonDown(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.XButton1, ButtonState.Pressed);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonDown(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.XButton2, ButtonState.Pressed);
                DoMouseButtonEvents(arg);
            }


            if (IsMouseButtonUp(current.LeftButton, _prevMouseState.LeftButton))
            {
                _mouseLeftDown = false;
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Left, ButtonState.Released);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonUp(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Right, ButtonState.Released);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonUp(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.Middle, ButtonState.Released);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonUp(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.XButton1, ButtonState.Released);
                DoMouseButtonEvents(arg);
            }
            else if (IsMouseButtonUp(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, MouseButton.XButton2, ButtonState.Released);
                DoMouseButtonEvents(arg);
            }


            if (current.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                MouseWheelEventArgs arg = new MouseWheelEventArgs(current.X, current.Y, current.ScrollWheelValue == 0 ? WheelDirection.None : current.ScrollWheelValue > 0 ? WheelDirection.Up : WheelDirection.Down);
                DoMouseWheelEvents(arg);
            }

            if (current.X != _prevMouseState.X || current.Y != _prevMouseState.Y)
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y);
                DoMouseMoveEvents(arg);
                Console.WriteLine(current.X + "," + current.Y);
            }

            _prevMouseState = current;
        }

        private void CheckKeyboardEvents()
        {
            KeyboardState current = Keyboard.GetState();

            Keys[] oldkeys = _prevKeyboardState.GetPressedKeys();
            Keys[] newkeys = current.GetPressedKeys();

            foreach (Keys k in newkeys)
            {
                if (current.IsKeyDown(k))
                {
                    Keys old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyDown(old))
                    {
                        // pressed 1st time: FIRE!
                        DoKeyboardEvents(new KeyboardEventArgs(k, KeyState.Down));
                        Console.WriteLine("KEY DOWN");
                    }                
                }
            }

            foreach (Keys k in oldkeys)
            {
                if (current.IsKeyUp(k))
                {
                    Keys old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyUp(old))
                    {
                        // released 1st time: FIRE!
                        DoKeyboardEvents(new KeyboardEventArgs(k, KeyState.Up));
                        Console.WriteLine("KEY UP");
                    }
                }
            }

            _prevKeyboardState = current;
        }

        private bool IsMouseButtonDown(ButtonState current, ButtonState prev) => current == ButtonState.Pressed && prev == ButtonState.Released;
        private bool IsMouseButtonUp(ButtonState current, ButtonState prev) => current == ButtonState.Released && prev == ButtonState.Pressed;


      /*private void CheckMouseEvents(SDL2.SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL2.SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    switch ((uint)e.button.button)
                    {
                        case SDL2.SDL.SDL_BUTTON_LEFT:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, new MouseState()., MouseButtonState.Down));
                            break;
                        case SDL2.SDL.SDL_BUTTON_RIGHT:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.Right, MouseButtonState.Down));
                            break;
                        case SDL2.SDL.SDL_BUTTON_MIDDLE:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.Middle, MouseButtonState.Down));
                            break;
                        case SDL2.SDL.SDL_BUTTON_X1:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.XButton1, MouseButtonState.Down));
                            break;
                        case SDL2.SDL.SDL_BUTTON_X2:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.XButton2, MouseButtonState.Down));
                            break;
                    }
                    break;
                case SDL2.SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    switch ((uint)e.button.button)
                    {
                        case SDL2.SDL.SDL_BUTTON_LEFT:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.Left));
                            break;
                        case SDL2.SDL.SDL_BUTTON_RIGHT:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.Right));
                            break;
                        case SDL2.SDL.SDL_BUTTON_MIDDLE:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.Middle));
                            break;
                        case SDL2.SDL.SDL_BUTTON_X1:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.XButton1));
                            break;
                        case SDL2.SDL.SDL_BUTTON_X2:
                            DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y, MouseButton.XButton2));
                            break;
                    }
                    break;
                case SDL2.SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    DoMouseWheelEvents(new MouseWheelEventArgs(e.wheel.y > 0 ? MouseButtonState.Up : MouseButtonState.Down));
                    break;
                case SDL2.SDL.SDL_EventType.SDL_MOUSEMOTION:
                    DoMouseEvents(new MouseEventArgs(e.motion.x, e.motion.y));
                    break;
            }
        }

        private void CheckKeyboardEvents(SDL2.SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL2.SDL.SDL_EventType.SDL_KEYDOWN:
                    break;
                case SDL2.SDL.SDL_EventType.SDL_KEYUP:
                    break;
            }
        }
        */

        private void DoMouseButtonEvents(MouseEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                if (arg.Button == MouseButton.Left && arg.ButtonState == ButtonState.Released && s.CanDragNow)
                    s.CanDragNow = false;

                if (s.IsEnabled && s.IsVisible && s.Rectangle.Contains(arg.Location.X, arg.Location.Y))
                {
                    if (s.IsMovable && !s.CanDragNow && arg.Button == MouseButton.Left
                        && arg.ButtonState == ButtonState.Pressed)
                        s.CanDragNow = true;
                    return true;
                }

                return false;
            }
            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            if (control != null)
            {
                if (arg.ButtonState == ButtonState.Pressed)
                {
                    if (_lastFocused != null && control != _lastFocused)
                        _lastFocused.RemoveFocus();

                    control.SetFocused();

                    _lastFocused = control;
                }

                control?.OnMouseButton(arg);
            }

        }

        private void DoMouseMoveEvents(MouseEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                if (s.IsEnabled && s.IsVisible)
                {
                    if (s.Rectangle.Contains(arg.Location.X, arg.Location.Y) || s.CanDragNow)
                    {
                        if (!s.MouseIsOver)
                            s.OnMouseEnter(arg);
                        return true;
                    }
                    else if (s.MouseIsOver)
                        s.OnMouseLeft(arg);
                }
                return false;
            }

            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            if (control != null)
            {
                
                
                if (control.IsMovable && control.CanDragNow)
                {
                    control.MoveTo(

                         arg.Location.X - _prevMouseState.X, 
                         arg.Location.Y - _prevMouseState.Y

                        );
                }

                control.OnMouseMove(arg);
            }
        }

        private void DoMouseWheelEvents(MouseWheelEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                return s.IsEnabled && s.IsVisible && s.Rectangle.Contains(arg.Location.X, arg.Location.Y);
            }

            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            control?.OnMouseWheel(arg);
        }


        private void DoKeyboardEvents(KeyboardEventArgs arg)
        {
            _lastFocused?.OnKeyboard(arg);
        }

        private void GetControl(Control parent, Func<Control, bool> condition, ref Control founded)
        {
            //for (int i = parent.Children.Count - 1; i >= 0; i--)
            for (int i = 0; i < parent.Children.Count; i++)
            {
                Control c = parent.Children[i];
                if (condition(c))
                {
                    founded = c;
                    GetControl(c, condition, ref founded);
                }
            }

            if (founded == null && condition(parent))
                founded = parent;
        }


    }
}
