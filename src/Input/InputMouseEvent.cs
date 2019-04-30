#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Input
{
    internal class InputMouseEvent : InputEvent
    {
        private readonly int _clicks;
        private readonly int _data;

        public InputMouseEvent(MouseEvent type, MouseButton button, int clicks, int x, int y, int data, SDL.SDL_Keymod mod) : base(mod)
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