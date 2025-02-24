// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorPickerBox : Gump
    {
        private readonly int _cellHeight;
        private readonly int _cellWidth;
        private readonly int _columns;
        private readonly ushort[] _customPallete;
        private int _graduation, _selectedIndex;
        private ushort[] _hues;
        private bool _needToFileeBoxes = true;
        private readonly int _rows;


        public ColorPickerBox
        (
            World world,
            int x,
            int y,
            int rows = 10,
            int columns = 20,
            int cellW = 8,
            int cellH = 8,
            ushort[] customPallete = null
        ) : base(world, 0, 0)
        {
            X = x;
            Y = y;
            Width = columns * cellW;
            Height = rows * cellH;
            _rows = rows;
            _columns = columns;
            _cellWidth = cellW;
            _cellHeight = cellH;

            _customPallete = customPallete;
            AcceptMouseInput = true;


            Graduation = 1;
            SelectedIndex = 0;
        }


        public event EventHandler ColorSelectedIndex;

        public bool ShowLivePreview { get; set; }

        public ushort[] Hues
        {
            get
            {
                CreateTexture();

                return _hues;
            }
        }

        public int Graduation
        {
            get => _graduation;
            set
            {
                if (_graduation != value)
                {
                    _graduation = value;

                    _needToFileeBoxes = true;

                    CreateTexture();
                    ColorSelectedIndex.Raise();
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= _hues.Length)
                {
                    return;
                }

                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    ColorSelectedIndex.Raise();
                }
            }
        }

        public ushort SelectedHue => SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort) 0 : _hues[SelectedIndex];


        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_needToFileeBoxes)
            {
                CreateTexture();
            }

            if (ShowLivePreview)
            {
                int xx = Mouse.Position.X - X - ParentX;
                int yy = Mouse.Position.Y - Y - ParentY;

                if (Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
                {
                    SetSelectedIndex(xx, yy);
                }
            }

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Texture2D texture = SolidColorTextureCache.GetTexture(Color.White);

            Rectangle rect = new Rectangle(0, 0, _cellWidth, _cellHeight);

            Vector3 hueVector;

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++)
                {
                    hueVector = ShaderHueTranslator.GetHueVector(_hues[i * _columns + j]);

                    rect.X = x + j * _cellWidth;
                    rect.Y = y + i * _cellHeight;

                    batcher.Draw
                    (
                        texture,
                        rect,
                        hueVector
                    );
                }
            }

            hueVector = ShaderHueTranslator.GetHueVector(0);

            if (_hues.Length > 1)
            {
                rect.X = (int) (x + Width / _columns * (SelectedIndex % _columns + .5f) - 1);
                rect.Y = (int)(y + Height / _rows * (SelectedIndex / _columns + .5f) - 1);
                rect.Width = 2;
                rect.Height = 2;

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    rect,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                SetSelectedIndex(x, y);
            }
        }

        private void SetSelectedIndex(int x, int y)
        {
            int row = x / (Width / _columns);
            int column = y / (Height / _rows);
            SelectedIndex = row + column * _columns;
        }

        private void CreateTexture()
        {
            if (!_needToFileeBoxes || IsDisposed)
            {
                return;
            }

            _needToFileeBoxes = false;


            int size = _rows * _columns;
            ushort startColor = (ushort)(Graduation + 1);

            if (_hues == null || size != _hues.Length)
            {
                _hues = new ushort[size];
            }

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    ushort hue = (ushort) ((_customPallete?[y * _columns + x] ?? startColor) + 1);

                    _hues[y * _columns + x] = hue;

                    startColor += 5;
                }
            }
        }
    }
}