using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps
{
    class ColorPickerBox : GumpControl
    {
        private const int ROWS = 10;
        private const int COLUMNS = 20;

        private const int CELL_WIDTH = 8;
        private const int CELL_HEIGHT = 8;

        private const int WIDTH = COLUMNS * CELL_WIDTH;
        private const int HEIGHT = ROWS * CELL_HEIGHT;


        private int _graduation, _selectedIndex;
        private SpriteTexture _colorTable;
        private Texture2D _pointer;
        private ushort[] _hues;


        public ColorPickerBox(int x, int y) : base()
        {
            X = x;
            Y = y;

            Width = WIDTH;
            Height = HEIGHT;

            AcceptMouseInput = true;
        }

        public EventHandler ColorSelectedIndex;

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

        public ushort SelectedHue => SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort)0 : _hues[SelectedIndex];


        public override void Update(double totalMS, double frameMS)
        {
            if (_colorTable == null || _colorTable.IsDisposed)
            {
                CreateTexture();
            }

            _colorTable.Ticks = (long)totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (_pointer == null)
            {
                _pointer = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pointer.SetData(new Color[1] { Color.White });
                SelectedIndex = 0;
            }

            spriteBatch.Draw2D(_colorTable, new Rectangle((int)position.X, (int)position.Y, Width, Height), Vector3.Zero);

            spriteBatch.Draw2D(_pointer, new Rectangle((int)(
                position.X + (WIDTH / COLUMNS) * ((SelectedIndex % COLUMNS) + .5f) - 1),
                (int)(position.Y + (HEIGHT / ROWS) * ((SelectedIndex / COLUMNS) + .5f) - 1), 2, 2), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);          
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            int row = x / (WIDTH / COLUMNS);

            int column = y / (HEIGHT / ROWS);

            SelectedIndex = row + column * COLUMNS;
        }



        private unsafe void CreateTexture()
        {
            if (_colorTable != null && !_colorTable.IsDisposed)
                return;

            int offset = Marshal.SizeOf<HuesGroup>() - 4;

            ushort startColor = (ushort)(Graduation + 1);

            _hues = new ushort[20 * 10];
            var pixels = new Color[20 * 10];

            int size = Marshal.SizeOf<HuesGroup>();

            IntPtr ptr = Marshal.AllocHGlobal(size * Hues.HuesRange.Length);

            for (int i = 0; i < Hues.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(Hues.HuesRange[i], ptr + i * size, false);
            }


            byte* huesData = (byte*)(ptr + (32 + 4));

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    int colorIndex = (startColor + ((startColor + (startColor << 2)) << 1)) << 3;
                    colorIndex += (colorIndex / offset) << 2;

                    ushort color = *(ushort*)((IntPtr)huesData + colorIndex);
                    (byte b, byte g, byte r, byte _) = Hues.GetBGRA(Hues.Color16To32(color));

                    Color cc = new Color(b, g, r);
                    pixels[y * 20 + x] = cc;
                    _hues[y * 20 + x] = startColor;
                    startColor += 5;
                }
            }

            Marshal.FreeHGlobal(ptr);

            _colorTable = new SpriteTexture(20, 10);
            _colorTable.SetData(pixels);
        }

    }
}
