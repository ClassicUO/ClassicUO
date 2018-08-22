using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClassicUO.Input
{
    public enum MouseButton
    {
        None,
        Left,
        Middle,
        Right,
        XButton1,
        XButton2
    }

    public enum WheelDirection
    {
        None,
        Up,
        Down
    }

    public sealed class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(int x, int y, int offx, int offy, MouseButton button = MouseButton.None, ButtonState state = ButtonState.Released)
        {
            Location = new Point(x, y);
            Button = button;
            ButtonState = state;
            Offset = new Point(x - offx, y - offy);
        }

        public Point Location { get; }
        public Point Offset { get; }
        public MouseButton Button { get; }
        public ButtonState ButtonState { get; }
    }

    public sealed class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(int x, int y, int offx, int offy, WheelDirection direction)
        {
            Location = new Point(x, y);
            Direction = direction;
            Offset = new Point(x - offx, y - offy);
        }

        public Point Location { get; }
        public Point Offset { get; }
        public WheelDirection Direction { get; }
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
    }
}