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
        private const int WIDTH = 400;
        private const int HEIGHT = 400;
        private ScrollArea _scrollArea;
        private Area _optionsArea;
        private Area _previewArea;

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

        private void BuildGump()
        {
            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = 2,
                    Y = 2,
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


            _scrollArea = new ScrollArea(
                5,
                _optionsArea.Y + _optionsArea.Height + 15,
                WIDTH - 15,
                HEIGHT - _optionsArea.Height - 125,
                false
                );
            _scrollArea.AcceptMouseInput = true;
            _scrollArea.CanMove = true;


            _previewArea = new Area();
            _previewArea.X = 0;
            _previewArea.Y = 5;
            _previewArea.Width = _scrollArea.Width - 14;
            _previewArea.Height = _scrollArea.Height - 5;
            _previewArea.AcceptMouseInput = true;
            _previewArea.CanMove = true;

            _scrollArea.Add(_previewArea);


            Add(_scrollArea);

            AddPreview();

            Add
           (
               new Button(1, 0x00EF, 0x00F0, 0x00EE)
               {
                   X = 165,
                   Y = HEIGHT - 50,
                   ButtonAction = ButtonAction.Activate
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
            area.AcceptMouseInput = true;
            area.CanMove = true;


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
                Y = 10,
                IsChecked = _macro.HideLabel
            };

            _hideLabelCheckbox.ValueChanged += (sender, e) =>
            {
                _macro.HideLabel = _hideLabelCheckbox.IsChecked;
                AddPreview();
            };
            area.Add(_hideLabelCheckbox);


            Label _ScaleLbl = new Label
            (
                    "Scale",
                    true,
                    0xFFFF,
                    50,
                    0xFF,
                    FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_LEFT
            )
            {
                X = _hideLabelCheckbox.X + _hideLabelCheckbox.Width + 15,
                Y = _hideLabelCheckbox.Y
            };
            area.Add(_ScaleLbl);

            HSliderBar _scale = new HSliderBar
            (
                _ScaleLbl.X + _ScaleLbl.Width + 15,
                _ScaleLbl.Y + 2,
                180,
                10,
                200,
                _macro.Scale,
                HSliderBarStyle.BlueWidgetNoBar,
                true,
                0xFF,
                0xFFFF
            );
            _scale.ValueChanged += (sender, e) =>
            {
                _macro.Scale = (byte)_scale.Value;
                AddPreview();
            };

            _scale.Add(new AlphaBlendControl(0.3f)
            {
                Hue = 0x0481,
                Width = _scale.Width,
                Height = _scale.Height
            });

            area.Add(_scale);


            ModernColorPicker.HueDisplay _hueDisplay = new ModernColorPicker.HueDisplay(_macro.Hue, null, true)
            {
                X = 10,
                Y = _scale.Y + _scale.Height + 15
            };
            _hueDisplay.HueChanged += (sender, ee) =>
            {
                _macro.Hue = _hueDisplay.Hue;
                AddPreview();
            };
            area.Add(_hueDisplay);

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
                X = _ScaleLbl.X,
                Y = _ColorLabel.Y
            });

            StbTextBox _searchBox = new StbTextBox(0xFF, -1, 65, true, FontStyle.None, 0x0481)
            {
                X = _scale.X,
                Y = _ColorLabel.Y,
                Multiline = false,
                Width = 65,
                Height = 20,
                Text = _macro.Graphic.ToString(),
                NumbersOnly = true
            };

            _searchBox.TextChanged += (sender, e) => {
                if (ushort.TryParse(_searchBox.Text, out var id)){
                    OnGraphicChange(id);
                    return;
                }
                OnGraphicChange(null);
            };
            _searchBox.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            });
            area.Add(_searchBox);


            area.WantUpdateSize = false;
            return area;
        }
        private void OnGraphicChange(ushort? graphic)
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
                    var existing = UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == _macro);
                    if (existing != null)
                    {
                        existing.TheMacro = _macro;
                    }                   
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
}