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

using System;
using System.Runtime.InteropServices;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorPickerBox : Control
    {
        private readonly int _cellHeight;
        private readonly int _cellWidth;
        private readonly int _columns;
        private readonly ushort[] _customPallete;
        private readonly int _rows;
        private ColorBox[,] _colorBoxes;
        private int _graduation, _selectedIndex;
        private ushort[] _hues;
        private bool _needToFileeBoxes = true;
        private Texture2D _pointer;

        private bool _selected;
        public EventHandler ColorSelectedIndex;

        public ColorPickerBox(int x, int y, int rows = 10, int columns = 20, int cellW = 8, int cellH = 8, ushort[] customPallete = null)
        {
            X = x;
            Y = y;
            Width = columns * cellW;
            Height = rows * cellH;
            _rows = rows;
            _columns = columns;
            _cellWidth = cellW;
            _cellHeight = cellH;

            _colorBoxes = new ColorBox[rows, columns];

            _customPallete = customPallete;
            AcceptMouseInput = true;


            Graduation = 1;
        }

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
                    ClearColorBoxes();

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
                    return;

                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    ColorSelectedIndex.Raise();
                }
            }
        }

        public ushort SelectedHue => SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort) 0 : _hues[SelectedIndex];

        public void SetHue(ushort hue)
        {
            _selected = true;
            Graduation = hue - 1;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_needToFileeBoxes)
                CreateTexture();

            base.Update(totalMS, frameMS);

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++) _colorBoxes[y, x].Update(totalMS, frameMS);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_pointer == null)
            {
                _pointer = new Texture2D(batcher.GraphicsDevice, 1, 1);

                _pointer.SetData(new Color[1]
                {
                    Color.White
                });

                if (SelectedIndex != 0)
                    SelectedIndex = 0;
            }

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++) _colorBoxes[i, j].Draw(batcher, x + j * _cellWidth, y + i * _cellHeight);
            }

            if (_hues.Length > 1)
            {
                ResetHueVector();

                batcher.Draw2D(_pointer, (int) (x + Width / _columns * (SelectedIndex % _columns + .5f) - 1), (int) (y + Height / _rows * (SelectedIndex / _columns + .5f) - 1), 2, 2, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                int row = x / (Width / _columns);
                int column = y / (Height / _rows);
                SelectedIndex = row + column * _columns;
            }
        }


        private unsafe void CreateTexture()
        {
            if (!_needToFileeBoxes || IsDisposed)
                return;

            _needToFileeBoxes = false;


            if (_customPallete != null && !_selected)
            {
                CreateTextureFromCustomPallet();
                _selected = false;

                return;
            }

            int offset = Marshal.SizeOf<HuesGroup>() - 4;
            ushort startColor = (ushort) (Graduation + 1);
            _hues = new ushort[_rows * _columns];
            int size = Marshal.SizeOf<HuesGroup>();
            IntPtr ptr = Marshal.AllocHGlobal(size * FileManager.Hues.HuesRange.Length);

            for (int i = 0; i < FileManager.Hues.HuesRange.Length; i++)
                Marshal.StructureToPtr(FileManager.Hues.HuesRange[i], ptr + i * size, false);
            byte* huesData = (byte*) (ptr + (32 + 4));

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    int colorIndex = (startColor + ((startColor + (startColor << 2)) << 1)) << 3;
                    colorIndex += (colorIndex / offset) << 2;
                    ushort color = *(ushort*) ((IntPtr) huesData + colorIndex);
                    //uint cc = HuesHelper.RgbaToArgb((HuesHelper.Color16To32(color) << 8) | 0xFF);
                    //pixels[y * _columns + x] = cc;
                    _hues[y * _columns + x] = startColor;

                    ColorBox box = new ColorBox(_cellWidth, _cellHeight, startColor, HuesHelper.Color16To32(color));
                    _colorBoxes[y, x] = box;
                    startColor += 5;
                }
            }

            _selected = false;
            Marshal.FreeHGlobal(ptr);
        }

        private unsafe void CreateTextureFromCustomPallet()
        {
            int size = Marshal.SizeOf<HuesGroup>();
            IntPtr ptr = Marshal.AllocHGlobal(size * FileManager.Hues.HuesRange.Length);

            for (int i = 0; i < FileManager.Hues.HuesRange.Length; i++)
                Marshal.StructureToPtr(FileManager.Hues.HuesRange[i], ptr + i * size, false);
            byte* huesData = (byte*) (ptr + (32 + 4));

            _hues = new ushort[_rows * _columns];
            int offset = Marshal.SizeOf<HuesGroup>() - 4;

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    ushort startColor = _customPallete[y * _columns + x];
                    int colorIndex = (startColor + ((startColor + (startColor << 2)) << 1)) << 3;
                    colorIndex += (colorIndex / offset) << 2;
                    ushort color = *(ushort*) ((IntPtr) huesData + colorIndex);
                    _hues[y * _columns + x] = startColor;

                    ColorBox box = new ColorBox(_cellWidth, _cellHeight, startColor, HuesHelper.Color16To32(color));
                    _colorBoxes[y, x] = box;
                }
            }

            Marshal.FreeHGlobal(ptr);
        }

        private void ClearColorBoxes()
        {
            if (_colorBoxes != null)
            {
                for (int y = 0; y < _rows; y++)
                {
                    for (int x = 0; x < _columns; x++)
                        _colorBoxes[y, x]?.Dispose();
                }
            }

            _needToFileeBoxes = true;
        }


        public override void Dispose()
        {
            ClearColorBoxes();
            _colorBoxes = null;
            //_colorTable?.Dispose();
            //_colorTable = null;
            _pointer?.Dispose();
            _pointer = null;
            base.Dispose();
        }
    }
}