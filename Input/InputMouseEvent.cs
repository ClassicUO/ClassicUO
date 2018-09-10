using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Input
{
    public class InputMouseEvent : InputEvent
    {
        private readonly MouseButtons _button;
        private readonly int _clicks;
        private readonly int _data;
        
        public InputMouseEvent(MouseEvent type, MouseButtons button, int clicks, int x, int y, int data, SDL2.SDL.SDL_Keymod mod) : base(mod)
        {
            EventType = type;
            _button = button;
            _clicks = clicks;
            X = x;
            Y = y;
            _data = data;
        }

        public InputMouseEvent(MouseEvent type, InputMouseEvent parent) : base(parent)
        {
            EventType = type;
            _button = parent._button;
            _clicks = parent._clicks;
            X = parent.X;
            Y = parent.Y;
            _data = parent._data;
        }


        public int X { get; }
        public int Y { get; }
        public MouseEvent EventType { get; }
        public Point Position => new Point(X, Y);
        public MouseButtons Button => _button;
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
