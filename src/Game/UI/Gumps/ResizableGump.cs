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
        private readonly GameBorder _border;
        private Point _lastSize, _savedSize;
        private bool _clicked;
        private int _minW, _minH;



        protected ResizableGump(int width, int height, int minW, int minH, Serial local, Serial server) : base(local, server)
        {
            _border = new GameBorder(0, 0, Width, Height, 4);
            _border.Hue = 0x01EC;
            Add(_border);
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
            get => _border.IsVisible;
            set => _border.IsVisible = _button.IsVisible = value;
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
            _border.Width = Width;
            _border.Height = Height;
            _button.X = Width - (_button.Width >> 0) + 2;
            _button.Y = Height - (_button.Height >> 0) + 2;
        }
        
    }
}
