#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
        private readonly int _columns;
        private readonly int _rows;
        private int _cellHeight;
        private int _cellWidth;
        private int _graduation, _selectedIndex;
        private ushort[] _hues;
        private readonly ushort[] _customPallete;
        private Texture2D _pointer;
        public EventHandler ColorSelectedIndex;
        private ColorBox[,] _colorBoxes;
        private bool _needToFileeBoxes = true;

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

                    //if (_colorTable != null && !_colorTable.IsDisposed)
                    //    _colorTable.Dispose();
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
                    throw new IndexOutOfRangeException();

                _selectedIndex = value;
                ColorSelectedIndex.Raise();
            }
        }

        public ushort SelectedHue
        {
            get => SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort) 0 : _hues[SelectedIndex];
        }

        public void SetHue(ushort hue)
        {
            uint aa = FileManager.Hues.GetPolygoneColor(12, hue);
            Console.WriteLine("{0:X8}", aa);
            uint c = HuesHelper.RgbaToArgb((aa << 8) | 0xFF);
            Console.WriteLine("{0:X8}", c);

            _hues[SelectedIndex] = hue;
            //HuesHelper.GetBGRA()
            //SetHue(c);


            Console.WriteLine("{0:X4}", hue);
            Console.WriteLine("{0:X4} - {1:X4}", (ushort)(HuesHelper.Color32To16(c)), (ushort)(HuesHelper.Color32To16(aa)));

            if (_hues[SelectedIndex] == hue)
            {

            }
            else
            {

            }

            //if (_colorTable != null && !_colorTable.IsDisposed)
            //    _colorTable.Dispose();
            //_colorTable = new SpriteTexture(1, 1);

            //uint[] color = new uint[1]
            //{
            //    c
            //};
            //_colorTable.SetData(color);
        }

        public void SetHue(uint hue)
        {

            //_hues[SelectedIndex] = (ushort)(HuesHelper.Color32To16(   hue ));


            //if (_colorTable != null && !_colorTable.IsDisposed)
            //    _colorTable.Dispose();
            //_colorTable = new SpriteTexture(1, 1);

            //uint[] color = new uint[1]
            //{
            //    hue
            //};
            //_colorTable.SetData(color);
        }

        public override void Update(double totalMS, double frameMS)
        {
            //if (_colorTable == null || _colorTable.IsDisposed)
            //    CreateTexture();
            //_colorTable.Ticks = (long) totalMS;

            if (_needToFileeBoxes)
                CreateTexture();

            base.Update(totalMS, frameMS);

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    _colorBoxes[y, x].Update(totalMS, frameMS);
                }
            }
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
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

            //batcher.Draw2D(_colorTable, new Rectangle(position.X, position.Y, Width, Height), Vector3.Zero);

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    _colorBoxes[y, x].Draw(batcher, new Point(position.X + (x * _cellWidth), position.Y + (y * _cellHeight)), hue);
                }
            }

            if (_hues.Length > 1)
                batcher.Draw2D(_pointer, new Rectangle((int) (position.X + Width / _columns * (SelectedIndex % _columns + .5f) - 1), (int) (position.Y + Height / _rows * (SelectedIndex / _columns + .5f) - 1), 2, 2), Vector3.Zero);

            return base.Draw(batcher, position, hue);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            int row = x / (Width / _columns);
            int column = y / (Height / _rows);
            SelectedIndex = row + column * _columns;
        }

        private unsafe void CreateTexture()
        {
            //if (_colorTable != null && !_colorTable.IsDisposed)
            //    return;

            if (!_needToFileeBoxes)
                return;

            _needToFileeBoxes = false;

            int offset = Marshal.SizeOf<HuesGroup>() - 4;
            ushort startColor = (ushort) (Graduation + 1);
            _hues = new ushort[_rows * _columns];
            //uint[] pixels = new uint[_rows * _columns];
            int size = Marshal.SizeOf<HuesGroup>();
            IntPtr ptr = Marshal.AllocHGlobal(size * FileManager.Hues.HuesRange.Length);
            for (int i = 0; i < FileManager.Hues.HuesRange.Length; i++)
                Marshal.StructureToPtr(FileManager.Hues.HuesRange[i], ptr + i * size, false);
            byte* huesData = (byte*) (ptr + (32 + 4));

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    if (_customPallete != null)
                        startColor = _customPallete[y * _columns + x];
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

            //Marshal.FreeHGlobal(ptr);
            //_colorTable = new SpriteTexture(_columns, _rows);
            //_colorTable.SetData(pixels);
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