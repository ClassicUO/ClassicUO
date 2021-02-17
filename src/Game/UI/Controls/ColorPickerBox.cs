﻿#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Runtime.InteropServices;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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
        private ColorBox[,] _colorBoxes;
        private readonly int _columns;
        private readonly ushort[] _customPallete;
        private int _graduation, _selectedIndex;
        private ushort[] _hues;
        private bool _needToFileeBoxes = true;
        private Texture2D _pointer;
        private readonly int _rows;

        private bool _selected;

        public ColorPickerBox
        (
            int x,
            int y,
            int rows = 10,
            int columns = 20,
            int cellW = 8,
            int cellH = 8,
            ushort[] customPallete = null
        ) : base(0, 0)
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

        public ushort SelectedHue =>
            SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort) 0 : _hues[SelectedIndex];

        public EventHandler ColorSelectedIndex;

        public void SetHue(ushort hue)
        {
            _selected = true;
            Graduation = hue - 1;
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_needToFileeBoxes)
            {
                CreateTexture();
            }

            base.Update(totalTime, frameTime);

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    _colorBoxes?[y, x].Update(totalTime, frameTime);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_pointer == null)
            {
                _pointer = new Texture2D(batcher.GraphicsDevice, 1, 1);

                _pointer.SetData
                (
                    new Color[1]
                    {
                        Color.White
                    }
                );

                if (SelectedIndex != 0)
                {
                    SelectedIndex = 0;
                }
            }

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++)
                {
                    _colorBoxes?[i, j].Draw(batcher, x + j * _cellWidth, y + i * _cellHeight);
                }
            }

            if (_hues.Length > 1)
            {
                ResetHueVector();

                batcher.Draw2D
                (
                    _pointer,
                    (int) (x + Width / _columns * (SelectedIndex % _columns + .5f) - 1),
                    (int) (y + Height / _rows * (SelectedIndex / _columns + .5f) - 1),
                    2,
                    2,
                    ref HueVector
                );
            }

            return base.Draw(batcher, x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                int row = x / (Width / _columns);
                int column = y / (Height / _rows);
                SelectedIndex = row + column * _columns;
            }
        }


        private unsafe void CreateTexture()
        {
            if (!_needToFileeBoxes || IsDisposed)
            {
                return;
            }

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
            IntPtr ptr = Marshal.AllocHGlobal(size * HuesLoader.Instance.HuesRange.Length);

            for (int i = 0; i < HuesLoader.Instance.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(HuesLoader.Instance.HuesRange[i], ptr + i * size, false);
            }

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
            IntPtr ptr = Marshal.AllocHGlobal(size * HuesLoader.Instance.HuesRange.Length);

            for (int i = 0; i < HuesLoader.Instance.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(HuesLoader.Instance.HuesRange[i], ptr + i * size, false);
            }

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
                    {
                        _colorBoxes[y, x]?.Dispose();
                    }
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