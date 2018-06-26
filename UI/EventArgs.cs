using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.UI
{

    public sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, MouseButton button = MouseButton.None, ButtonState state = ButtonState.Released)
        {
            Location = new Point(x, y);
            Button = button;
            ButtonState = state;

            
        }

        public Point Location { get; set; }
        public MouseButton Button { get; set; }
        public ButtonState ButtonState { get; set; }
    }

    public sealed class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(int x, int y, WheelDirection direction)
        {
            Location = new Point(x, y);
            Direction = direction;
        }

        public Point Location { get; set; }
        public WheelDirection Direction { get; set; }
    }

    public sealed class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(Keys key, KeyState state)
        {
            Key = key;
            KeyState = state;
        }

        public Keys Key { get; }
        public KeyState KeyState { get; }
    }

    public sealed class ButtonClickEventArgs : EventArgs
    {
        public ButtonClickEventArgs()
        {

        }
    }
}
