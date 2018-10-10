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
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class ColorPickerBox : GumpControl
    {
        private int _cellHeight;

        private int _cellWidth;
        private SpriteTexture _colorTable;
        private readonly int _columns;
        private int _graduation, _selectedIndex;
        private ushort[] _hues;
        private Texture2D _pointer;

        private readonly int _rows;

        public EventHandler ColorSelectedIndex;

        public ColorPickerBox(int x, int y, int rows = 10, int columns = 20, int cellW = 8, int cellH = 8,
            int graduation = 0)
        {
            X = x;
            Y = y;

            Width = columns * cellW;
            Height = rows * cellH;

            _rows = rows;
            _columns = columns;
            _cellWidth = cellW;
            _cellHeight = cellH;

            Graduation = 1;

            AcceptMouseInput = true;
        }

        public int Graduation
        {
            get => _graduation;
            set
            {
                if (_graduation != value)
                {
                    _graduation = value;

                    if (_colorTable != null && !_colorTable.IsDisposed)
                        _colorTable.Dispose();
                    CreateTexture();

                    ColorSelectedIndex.Raise();
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            private set
            {
                _selectedIndex = value;
                ColorSelectedIndex.Raise();
            }
        }

        public ushort SelectedHue =>
            SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort) 0 : _hues[SelectedIndex];

        public void SetHue(ushort hue)
        {
            if (_colorTable != null && !_colorTable.IsDisposed)
                _colorTable.Dispose();

            _colorTable = new SpriteTexture(1, 1);
            uint[] color = new uint[1] {Hues.RgbaToArgb((Hues.GetPolygoneColor(12, hue) << 8) | 0xFF)};
            _colorTable.SetData(color);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_colorTable == null || _colorTable.IsDisposed)
            {
                CreateTexture();
            }

            _colorTable.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (_pointer == null)
            {
                _pointer = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pointer.SetData(new Color[1] {Color.White});
                SelectedIndex = 0;
            }

            spriteBatch.Draw2D(_colorTable, new Rectangle((int) position.X, (int) position.Y, Width, Height),
                Vector3.Zero);

            if (_hues.Length > 1)
                spriteBatch.Draw2D(_pointer, new Rectangle((int) (
                        position.X + Width / _columns * (SelectedIndex % _columns + .5f) - 1),
                    (int) (position.Y + Height / _rows * (SelectedIndex / _columns + .5f) - 1), 2, 2), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            int row = x / (Width / _columns);

            int column = y / (Height / _rows);

            SelectedIndex = row + column * _columns;
        }


        private unsafe void CreateTexture()
        {
            if (_colorTable != null && !_colorTable.IsDisposed)
                return;

            int offset = Marshal.SizeOf<HuesGroup>() - 4;

            ushort startColor = (ushort) (Graduation + 1);

            _hues = new ushort[_rows * _columns];
            var pixels = new uint[_rows * _columns];

            int size = Marshal.SizeOf<HuesGroup>();

            IntPtr ptr = Marshal.AllocHGlobal(size * Hues.HuesRange.Length);

            for (int i = 0; i < Hues.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(Hues.HuesRange[i], ptr + i * size, false);
            }


            byte* huesData = (byte*) (ptr + (32 + 4));

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    int colorIndex = (startColor + ((startColor + (startColor << 2)) << 1)) << 3;
                    colorIndex += (colorIndex / offset) << 2;

                    ushort color = *(ushort*) ((IntPtr) huesData + colorIndex);
                    uint cc = Hues.RgbaToArgb((Hues.Color16To32(color) << 8) | 0xFF);

                    pixels[y * _columns + x] = cc;
                    _hues[y * _columns + x] = startColor;
                    startColor += 5;
                }
            }

            Marshal.FreeHGlobal(ptr);

            _colorTable = new SpriteTexture(_columns, _rows);
            _colorTable.SetData(pixels);
        }
    }
}