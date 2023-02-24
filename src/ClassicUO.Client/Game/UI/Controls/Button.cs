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

using System.Collections.Generic;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal enum ButtonAction
    {
        Default = 0,
        SwitchPage = 0,
        Activate = 1
    }

    internal class Button : Control
    {
        private readonly string _caption;
        private bool _entered;
        private readonly RenderedText[] _fontTexture;
        private ushort _normal, _pressed, _over;

        public Button
        (
            int buttonID,
            ushort normal,
            ushort pressed,
            ushort over = 0,
            string caption = "",
            byte font = 0,
            bool isunicode = true,
            ushort normalHue = ushort.MaxValue,
            ushort hoverHue = ushort.MaxValue
        )
        {
            ButtonID = buttonID;
            _normal = normal;
            _pressed = pressed;
            _over = over;

            if (GumpsLoader.Instance.GetGumpTexture(normal, out var bounds) == null)
            {
                Dispose();

                return;
            }
          
            Width = bounds.Width;
            Height = bounds.Height;
            FontHue = normalHue == ushort.MaxValue ? (ushort) 0 : normalHue;
            HueHover = hoverHue == ushort.MaxValue ? normalHue : hoverHue;

            if (!string.IsNullOrEmpty(caption) && normalHue != ushort.MaxValue)
            {
                _fontTexture = new RenderedText[2];

                _caption = caption;

                _fontTexture[0] = RenderedText.Create(caption, FontHue, font, isunicode);

                if (hoverHue != ushort.MaxValue)
                {
                    _fontTexture[1] = RenderedText.Create(caption, HueHover, font, isunicode);
                }
            }

            CanMove = false;
            AcceptMouseInput = true;
            //CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
        }

        public Button(List<string> parts) : this(parts.Count >= 8 ? int.Parse(parts[7]) : 0, UInt16Converter.Parse(parts[3]), UInt16Converter.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Count >= 6)
            {
                int action = int.Parse(parts[5]);

                ButtonAction = action == 0 ? ButtonAction.SwitchPage : ButtonAction.Activate;
            }

            ToPage = parts.Count >= 7 ? int.Parse(parts[6]) : 0;
            WantUpdateSize = false;
            ContainsByBounds = true;
            IsFromServer = true;
        }

        public bool IsClicked { get; set; }

        public int ButtonID { get; }

        public ButtonAction ButtonAction { get; set; }

        public int ToPage { get; set; }

        public override ClickPriority Priority => ClickPriority.High;

        public ushort ButtonGraphicNormal
        {
            get => _normal;
            set
            {
                _normal = value;

                _ = GumpsLoader.Instance.GetGumpTexture(value, out var bounds);

                Width = bounds.Width;
                Height = bounds.Height;
            }
        }

        public ushort ButtonGraphicPressed
        {
            get => _pressed;
            set
            {
                _pressed = value;

                _ = GumpsLoader.Instance.GetGumpTexture(value, out var bounds);

                Width = bounds.Width;
                Height = bounds.Height;
            }
        }

        public ushort ButtonGraphicOver
        {
            get => _over;
            set
            {
                _over = value;

                _ = GumpsLoader.Instance.GetGumpTexture(value, out var bounds);

                Width = bounds.Width;
                Height = bounds.Height;
            }
        }

        public int Hue { get; set; }
        public ushort FontHue { get; }

        public ushort HueHover { get; }

        public bool FontCenter { get; set; }

        public bool ContainsByBounds { get; set; }

       
        protected override void OnMouseEnter(int x, int y)
        {
            _entered = true;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _entered = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Texture2D texture = null;
            Rectangle bounds = Rectangle.Empty;

            if (_entered || IsClicked)
            {
                if (IsClicked && _pressed > 0)
                {
                    texture = GumpsLoader.Instance.GetGumpTexture(_pressed, out bounds);
                }

                if (texture == null && _over > 0)
                {
                    texture = GumpsLoader.Instance.GetGumpTexture(_over, out bounds);
                }
            }

            if (texture == null)
            {
                texture = GumpsLoader.Instance.GetGumpTexture(_normal, out bounds);
            }

            if (texture == null)
            {
                return false;
            }

            Vector3 hue = ShaderHueTranslator.GetHueVector
                            (
                                Hue,
                                false,
                                Alpha,
                                true
                            );

            batcher.Draw
            (
                texture,
                new Rectangle(x, y, Width, Height),
                bounds,
                hue
            );

            if (!string.IsNullOrEmpty(_caption))
            {
                RenderedText textTexture = _fontTexture[_entered ? 1 : 0];

                if (FontCenter)
                {
                    int yoffset = IsClicked ? 1 : 0;

                    textTexture.Draw(batcher, x + ((Width - textTexture.Width) >> 1), y + yoffset + ((Height - textTexture.Height) >> 1));
                }
                else
                {
                    textTexture.Draw(batcher, x, y);
                }
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsClicked = true;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsClicked = false;

                if (!MouseIsOver)
                {
                    return;
                }

                if (_entered || Client.Game.Scene is GameScene)
                {
                    switch (ButtonAction)
                    {
                        case ButtonAction.SwitchPage:
                            ChangePage(ToPage);

                            break;

                        case ButtonAction.Activate:
                            OnButtonClick(ButtonID);

                            break;
                    }

                    Mouse.LastLeftButtonClickTime = 0;
                    Mouse.CancelDoubleClick = true;
                }
            }
        }

   
        public override bool Contains(int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            return ContainsByBounds ? base.Contains(x, y) : GumpsLoader.Instance.PixelCheck(_normal, x - Offset.X, y - Offset.Y);
        }

        public sealed override void Dispose()
        {
            if (_fontTexture != null)
            {
                foreach (RenderedText t in _fontTexture)
                {
                    t?.Destroy();
                }
            }

            base.Dispose();
        }
    }
}