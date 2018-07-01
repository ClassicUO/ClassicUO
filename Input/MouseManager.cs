using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClassicUO.Input
{
    public class MouseManager : GameComponent
    {
        private MouseState _prevMouseState = Mouse.GetState();

        public MouseManager(Game game) : base(game)
        {
           
        }

        public override void Update(GameTime gameTime)
        {
            MouseState current = Mouse.GetState();

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


            if (current.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                MouseWheelEventArgs arg = new MouseWheelEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, current.ScrollWheelValue == 0 ? WheelDirection.None : current.ScrollWheelValue > 0 ? WheelDirection.Up : WheelDirection.Down);
                MouseWheel.Raise(arg);
            }

            if (current.X != _prevMouseState.X || current.Y != _prevMouseState.Y)
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y);
                MouseMove.Raise(arg);
                Log.Message(LogTypes.Trace, arg.Location.ToString());
            }

            _prevMouseState = current;

            base.Update(gameTime);
        }

        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;

        private bool IsMouseButtonDown(ButtonState current, ButtonState prev) => current == ButtonState.Pressed && prev == ButtonState.Released;
        private bool IsMouseButtonUp(ButtonState current, ButtonState prev) => current == ButtonState.Released && prev == ButtonState.Pressed;
    }
}
