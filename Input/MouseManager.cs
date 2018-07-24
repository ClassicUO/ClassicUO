using System;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Input
{
    public static class MouseManager
    {
        private static MouseState _prevMouseState = Mouse.GetState();


        public static Point ScreenPosition { get; private set; }


        public static event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MousePressed;
        public static event EventHandler<MouseWheelEventArgs> MouseWheel;

        public static void Update()
        {
            MouseState current = Mouse.GetState();

            if (IsMouseButtonDown(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Left, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Right, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Middle, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton1, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton2, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }


            if (IsMouseButtonUp(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Left, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Right, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Middle, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton1, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton2, ButtonState.Released);
                MouseUp.Raise(arg);
            }


            if (IsMouseButtonPressed(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Left, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Right, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.Middle, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton1, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }
            else if (IsMouseButtonPressed(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    MouseButton.XButton2, ButtonState.Pressed);
                MousePressed.Raise(arg);
            }


            if (current.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                MouseWheelEventArgs arg = new MouseWheelEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y,
                    current.ScrollWheelValue == 0 ? WheelDirection.None :
                    current.ScrollWheelValue > 0 ? WheelDirection.Up : WheelDirection.Down);
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


        private static bool IsMouseButtonDown(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Released;
        }

        private static bool IsMouseButtonUp(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Released && prev == ButtonState.Pressed;
        }

        private static bool IsMouseButtonPressed(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Pressed;
        }
    }
}