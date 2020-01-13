#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    abstract class ResizableGump : Gump
    {
        private readonly Button _button;
        private readonly BorderControl _borderControl;
        private Point _lastSize, _savedSize;
        private bool _clicked;
        private int _minW, _minH;



        protected ResizableGump(int width, int height, int minW, int minH, uint local, uint server, ushort borderHue = 0) : base(local, server)
        {
            _borderControl = new BorderControl(0, 0, Width, Height, 4)
            {
                Hue = borderHue
            };
            Add(_borderControl);
            _button = new Button(0, 0x837, 0x838, 0x838);
            Add(_button);

            _button.MouseDown += (sender, e) => { _clicked = true;};
            _button.MouseUp += (sender, e) =>
            {
                ResizeWindow(_lastSize);
                _clicked = false;
            };

            WantUpdateSize = false;

            Width = _lastSize.X = width;
            Height = _lastSize.Y = height;
            _savedSize = _lastSize;

            _minW = minW;
            _minH = minH;

            OnResize();
        }

        public bool ShowBorder
        {
            get => _borderControl.IsVisible;
            set => _borderControl.IsVisible = _button.IsVisible = value;
        }


       


        public Point ResizeWindow(Point newSize)
        {
            if (newSize.X < _minW)
                newSize.X = _minW;

            if (newSize.Y < _minH)
                newSize.Y = _minH;

            //Resize();
            _savedSize = newSize;

            return newSize;
        }



        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            Point offset = Mouse.LDroppedOffset;

            _lastSize = _savedSize;

            if (_clicked && offset != Point.Zero)
            {
                int w = _lastSize.X + offset.X;
                int h = _lastSize.Y + offset.Y;

                if (w < _minW)
                    w = _minW;

                if (h < _minH)
                    h = _minH;

                _lastSize.X = w;
                _lastSize.Y = h;
            }

            if (Width != _lastSize.X || Height != _lastSize.Y)
            {
                Width = _lastSize.X;
                Height = _lastSize.Y;
                OnResize();
            }

            base.Update(totalMS, frameMS);
        }


        public virtual void OnResize()
        {
            _borderControl.Width = Width;
            _borderControl.Height = Height;
            _button.X = Width - (_button.Width >> 0) + 2;
            _button.Y = Height - (_button.Height >> 0) + 2;
        }
        
    }
}
