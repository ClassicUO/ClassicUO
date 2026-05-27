// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class ResizableGump : Gump
    {
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize, _savedSize;
        private readonly int _minH;
        private readonly int _minW;


        protected ResizableGump
        (
            World world,int width,
            int height,
            int minW,
            int minH,
            uint local,
            uint server,
            ushort borderHue = 0
        ) : base(world, local, server)
        {
            _borderControl = new BorderControl
            (
                0,
                0,
                Width,
                Height,
                4
            )
            {
                Hue = borderHue
            };

            Add(_borderControl);
            _button = new Button(0, 0x837, 0x838, 0x838);
            Add(_button);

            _button.MouseDown += (sender, e) => { _clicked = true; };

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
            {
                newSize.X = _minW;
            }

            if (newSize.Y < _minH)
            {
                newSize.Y = _minH;
            }

            //Resize();
            _savedSize = newSize;

            return newSize;
        }


        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            Point offset = Mouse.LDragOffset;

            _lastSize = _savedSize;

            if (_clicked && offset != Point.Zero)
            {
                int w = _lastSize.X + offset.X;
                int h = _lastSize.Y + offset.Y;

                if (w < _minW)
                {
                    w = _minW;
                }

                if (h < _minH)
                {
                    h = _minH;
                }

                _lastSize.X = w;
                _lastSize.Y = h;
            }

            if (Width != _lastSize.X || Height != _lastSize.Y)
            {
                Width = _lastSize.X;
                Height = _lastSize.Y;
                OnResize();
            }

            base.Update();
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