using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Input
{
    public class InputMouseEvent : InputEvent
    {
        private readonly int _clicks;
        private readonly int _data;

        public InputMouseEvent(MouseEvent type, MouseButton button, int clicks, int x, int y, int data,
            SDL.SDL_Keymod mod) : base(mod)
        {
            EventType = type;
            Button = button;
            _clicks = clicks;
            X = x;
            Y = y;
            _data = data;
        }

        public InputMouseEvent(MouseEvent type, InputMouseEvent parent) : base(parent)
        {
            EventType = type;
            Button = parent.Button;
            _clicks = parent._clicks;
            X = parent.X;
            Y = parent.Y;
            _data = parent._data;
        }


        public int X { get; }
        public int Y { get; }
        public MouseEvent EventType { get; }
        public Point Position => new Point(X, Y);
        public MouseButton Button { get; }

        //{
        //    get
        //    {
        //        if ((_button & MouseButtons.Left) == MouseButtons.Left)
        //            return MouseButtons.Left;
        //        if ((_button & MouseButtons.Right) == MouseButtons.Right)
        //            return MouseButtons.Right;
        //        if ((_button & MouseButtons.Middle) == MouseButtons.Middle)
        //            return MouseButtons.Middle;
        //        if ((_button & MouseButtons.XButton1) == MouseButtons.XButton1)
        //            return MouseButtons.XButton1;
        //        if ((_button & MouseButtons.XButton2) == MouseButtons.XButton2)
        //            return MouseButtons.XButton2;
        //        return MouseButtons.None;
        //    }
        //}
    }
}