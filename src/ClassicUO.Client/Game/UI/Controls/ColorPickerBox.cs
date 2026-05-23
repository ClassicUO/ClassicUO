// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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

        public ushort SelectedHue
        {
            get => SelectedIndex < 0 || SelectedIndex >= _hues.Length ? (ushort)0 : _hues[SelectedIndex];
            set => SelectHue(value);
        }

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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float colorLayerDepth = layerDepthRef += 0.001f;
            float selectorLayerDepth = layerDepthRef += 0.001f;

            Texture2D texture = SolidColorTextureCache.GetTexture(Color.White);

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hues[i * _columns + j]);

                    Rectangle rect = new(x + j * _cellWidth, y + i * _cellHeight, _cellWidth, _cellHeight);

                    renderLists.AddGumpNoAtlas(
                        batcher =>
                        {
                            batcher.Draw
                            (
                                texture,
                                rect,
                                hueVector,
                                colorLayerDepth
                            );
                            return true;
                        }
                    );
                }
            }

                      

            if (_hues.Length > 1)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                Rectangle rect = new(
                                    (int)(x + Width / _columns * (SelectedIndex % _columns + .5f) - 1),
                                    (int)(y + Height / _rows * (SelectedIndex / _columns + .5f) - 1),
                                    rect.Width = 2,
                                    rect.Height = 2
                                    );

                renderLists.AddGumpNoAtlas(
                    batcher =>
                    {
                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(Color.White),
                            rect,
                            hueVector,
                            selectorLayerDepth
                        );
                        return true;
                    }
                );
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
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

        private void SelectHue(ushort desiredHue)
        {
            // revert setting the Graduation later if it does not contain the desired color
            int previousGraduation = Graduation;

            // When calculating the color from the graduation, it's incremented twice by one in CreateTexture()
            // To revert this we could subtract two but to avoid negative values, adding 3 does the same thing because of the modulo
            Graduation = (desiredHue + 3) % 5;

            for (int i = 0; i < Hues.Length; i++)
            {
                if (Hues[i] == desiredHue)
                {
                    SelectedIndex = i;
                    return;
                }
            }

            Graduation = previousGraduation;
        }
    }
}