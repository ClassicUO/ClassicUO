#region license

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

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.IO.Audio;
using static ClassicUO.Renderer.UltimaBatcher2D;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MacroButtonEditorGump : Gump
    {
        private Texture2D backgroundTexture;
        private Label label;
        private const int WIDTH = 400;
        private const int HEIGHT = 400;
        private Area _previewArea;
        private Area _optionsArea;
        private ScrollArea _area;

        public MacroButtonEditorGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            _macro = Macro.CreateEmptyMacro("No Action");
        }
        public MacroButtonEditorGump(Macro macro, int x, int y) : this()
        {
            X = x;
            Y = y;
            _macro = macro;
            BuildGump();
        }        

        public override GumpType GumpType => GumpType.MacroButtonEditor;
        public Macro _macro;
        private ushort _hue;
        private bool _useScale;
        private int _scaleValue;
        private bool _useGraphic;

        private void BuildGump()
        {
            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = 0,
                    Y = 0,
                    Width = WIDTH,
                    Height = HEIGHT,
                    Hue = 999
                }
            );
            Label _header;
            Add(_header = new Label
                (
                    $"Editor for {_macro.Name}",
                    true,
                    0xFFFF,
                    WIDTH,
                    0xFF,
                    FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_CENTER
            ){ Y = 5});


            _optionsArea = BuildOptionsArea(5, _header.Height + 5);
            Add(_optionsArea);


            _previewArea = new Area();
            _previewArea.X = 5;
            _previewArea.Y = _optionsArea.Y + _optionsArea.Height + 15;
            _previewArea.Width = WIDTH - 10;
            _previewArea.Height = HEIGHT - _previewArea.Y - 75;
            Add(_previewArea);

            AddPreview();

            Add
           (
               new Button(2, 0x00F3, 0x00F1, 0x00F2)
               {
                   X = 50,
                   Y = HEIGHT - 50,
                   ButtonAction = ButtonAction.Default
               }
           );
            Add
           (
               new Button(1, 0x00EF, 0x00F0, 0x00EE)
               {
                   X = 150,
                   Y = HEIGHT - 50,
                   ButtonAction = ButtonAction.Activate
               }
           );
            Add
          (
              new Button(0, 0x00F9, 0x00F8, 0x00F7)
              {
                  X = 250,
                  Y = HEIGHT - 50,
                  ButtonAction = ButtonAction.Default
              }
          );

        }
        private Area BuildOptionsArea(int? x, int? y)
        {
            var area = new Area();
            area.Width = WIDTH - 10;
            area.Height = 80;
            area.X = x ?? 5;
            area.Y = y ?? 5;


            Checkbox _hideLabelCheckbox = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Hide Label",
                0xFF,
                0xFFFF
            )
            {
                X = 10,
                Y = 10
            };

            _hideLabelCheckbox.ValueChanged += (sender, e) =>
            {
                _macro.HideLabel = _hideLabelCheckbox.IsChecked;
                AddPreview();
            };
            area.Add(_hideLabelCheckbox);


            Checkbox _useScale = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Scale",
                0xFF,
                0xFFFF
            )
            {
                X = _hideLabelCheckbox.X + _hideLabelCheckbox.Width + 15,
                Y = _hideLabelCheckbox.Y
            };
            area.Add(_useScale);

            HSliderBar _scale = new HSliderBar
            (
                _useScale.X + _useScale.Width + 15,
                _useScale.Y + 2,
                200,
                1,
                10,
                5,
                HSliderBarStyle.MetalWidgetRecessedBar,
                true,
                0xFF,
                0xFFFF
            );
            area.Add(_scale);

            area.Add(new ModernColorPicker.HueDisplay((ushort)_macro.Hue, OnColorChange, true)
            {
                X = 10,
                Y = _scale.Y + _scale.Height + 15
            });

            Label _ColorLabel;
            area.Add(_ColorLabel = new Label
                (
                    $"Color",
                    true,
                    0xFFFF,
                    50,
                    0xFF,
                    FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_LEFT
            )
            {
                X = 30,
                Y = _scale.Y + _scale.Height + 15
            });

            Label _graphicLabel;
            area.Add(_graphicLabel = new Label
                (
                    $"Graphic",
                    true,
                    0xFFFF,
                    50,
                    0xFF,
                    FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_RIGHT
            )
            {
                X = _useScale.X,
                Y = _ColorLabel.Y
            });

            StbTextBox _searchBox = new StbTextBox(0xFF, 20, 50, true, FontStyle.None, 0x0481)
            {
                X = _scale.X,
                Y = _ColorLabel.Y,
                Multiline = false,
                Width = 50,
                Height = 20
            };

            _searchBox.TextChanged += (sender, e) => {
                if (ushort.TryParse(_searchBox.Text, out var id)){
                    OnGraphicChange(id);
                }
            };
            _searchBox.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            });

            //Combobox _graphicId = new Combobox(_scale.X, _ColorLabel.Y, 100, _graphics.Select(g => $"0x{g}").ToArray())
            //{
            //    SelectedIndex = 0
            //};
            //_graphicId.OnOptionSelected += (senderr, ee) =>
            //{
            //    ushort graphicId = _graphics.ElementAt(ee);
            //    OnGraphicChange(graphicId);
            //};
            area.Add(_searchBox);


            area.WantUpdateSize = false;
            return area;
        }
        private void OnColorChange(ushort hue)
        {
            _macro.Hue = hue;
            AddPreview();
        }
        private void OnGraphicChange(ushort graphic)
        {
            _macro.Graphic = graphic;
            AddPreview();
        }
        private void AddPreview()
        {
            _previewArea.Clear();
            _previewArea.Children.Clear();

            var _preview = new MacroButtonGump(_macro, 0, 0) { AcceptMouseInput = false };

            _preview.X = ((WIDTH - 10) >> 1) - (_preview.Width >> 1);
            _preview.Y = ((_previewArea.Height - 10) >> 1) - (_preview.Height >> 1);
            _previewArea.Add(_preview);

            _previewArea.WantUpdateSize = false;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    Client.Game.GetScene<GameScene>().Macros.Save();
                    break;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }

    internal class MacroButtonPreview: Control
    {
        private Texture2D backgroundTexture;
        private Vector3 hueVector;
        private AlphaBlendControl _background;
        private readonly RenderedText _renderedText;
        private Macro _macro;
        private ushort _graphic;
        private Label _label;

        public bool IsPartialHue { get; set; }
        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;
                if (_graphic == 0)
                {
                    return;
                }
                backgroundTexture = GumpsLoader.Instance.GetGumpTexture(Graphic, out Rectangle bounds);

                if (backgroundTexture == null)
                {
                    Dispose();

                    return;
                }

                Width = bounds.Width;
                Height = bounds.Height;

                IsPartialHue = TileDataLoader.Instance.StaticData[value].IsPartialHue;
            }
        }
        public MacroButtonPreview(Macro macro)
        {
            _macro = macro;
            Width = 88;
            Height = 44;
            Graphic = macro.Graphic;
            _label = new Label
               (
                   _macro.Name,
                   true,
                   0x03b2,
                   Width,
                   255,
                   FontStyle.BlackBorder,
                   TEXT_ALIGN_TYPE.TS_CENTER
               )
            {
                X = 0,
                Width = Width - 10
            };
            Add(_label);

            hueVector = ShaderHueTranslator.GetHueVector(_macro.Hue, IsPartialHue, 1);
            
        }
        public void RePosition()
        {
            if (this.Parent == null)
            {
                return;
            }
            Vector2 newPostion = new Vector2((this.Parent.Width >> 1) - (Width >> 1), (this.Parent.Height >> 1) - (Height >> 1));
            if (X == newPostion.X && Y == newPostion.Y)
            {
                return;
            }
            X = (int)newPostion.X;
            Y = (int)newPostion.Y;

            //_label.Y = (Height >> 1) - (_label.Height >> 1);
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            batcher.Draw
            (
                backgroundTexture,
                new Rectangle
                (
                    x,
                    y,
                    Width,
                    Height
                ),
                hueVector
            );

            if (Graphic == 0)
            {
                RePosition();
                return base.Draw(batcher, x, y); ;
            }
            var texture = GumpsLoader.Instance.GetGumpTexture(Graphic, out Rectangle bounds);


            if (texture != null)
            {
                Rectangle rect = new Rectangle(x, y, Width, Height);

                batcher.Draw
                (
                    texture,
                    rect,
                    bounds,
                    hueVector
                );
            }
            RePosition();
            return base.Draw(batcher, x, y);
        }
    }
}