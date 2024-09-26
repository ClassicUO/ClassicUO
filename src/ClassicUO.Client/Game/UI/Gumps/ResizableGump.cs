#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    public abstract class ResizableGump : AnchorableGump
    {
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize, _savedSize;
        private readonly int _minH;
        private readonly int _minW;
        protected bool _isLocked = false;
        protected bool? _prevCanMove = null, _prevCloseWithRightClick = null, _prevBorder = null;

        public BorderControl BorderControl { get { return _borderControl; } }

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
            GroupMatrixHeight = Height;
            GroupMatrixWidth = Width;
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
                GroupMatrixHeight = Height;
                GroupMatrixWidth = Width;
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
            GroupMatrixHeight = Height;
            GroupMatrixWidth = Width;
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            SetLockStatus(IsLocked);
        }

        protected virtual void SetLockStatus(bool locked)
        {
            _prevCanMove ??= CanMove;
            _prevCloseWithRightClick ??= CanCloseWithRightClick;
            _prevBorder ??= ShowBorder;

            _isLocked = locked;
            IsLocked = locked;
            if (_isLocked)
            {
                CanMove = false;
                CanCloseWithRightClick = false;
                ShowBorder = false;
            }
            else
            {
                CanMove = _prevCanMove ?? true;
                CanCloseWithRightClick = _prevCloseWithRightClick ?? true;
                ShowBorder = _prevBorder ?? true;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);
                if (texture.Texture != null)
                {
                    if (x >= 0 && x <= texture.UV.Width && y >= 0 && y <= texture.UV.Height)
                    {
                        SetLockStatus(!_isLocked);
                    }
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);

                if (texture.Texture != null)
                {
                    if (_isLocked)
                    {
                        hueVector.X = 34;
                        hueVector.Y = 1;
                    }
                    batcher.Draw
                    (
                        texture.Texture,
                        new Vector2(x, y),
                        texture.UV,
                        hueVector
                    );
                }
            }

            return true;
        }
    }
}