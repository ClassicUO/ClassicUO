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

using System;
using System.Collections.Generic;
using ClassicUO.Data;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Checkbox : Control
    {
        private string _text;
        private FontSettings _fontSettings;
        private float _maxWidth;
        private ushort _hue;

        private bool _isChecked;
        private ushort _inactive, _active;

        public Checkbox
        (
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        )
        {
            _inactive = inactive;
            _active = active;

            var textureInactive = GumpsLoader.Instance.GetGumpTexture(inactive, out var boundsInactive);
            var textureActive = GumpsLoader.Instance.GetGumpTexture(active, out var boundsActive);

            if (textureInactive == null || textureActive == null)
            {
                Dispose();

                return;
            }

            Width = boundsInactive.Width;

            _fontSettings.FontIndex = font == 0xFF ? (byte)(Client.Version >= ClientVersion.CV_305D ? 1 : 0) : font;
            _fontSettings.IsUnicode = isunicode;
            _maxWidth = maxWidth;
            _text = text;

            if (color == 0xFFFF)
            {
                color = 1;
            }

            _hue = (ushort)(color - 1);
            var textSize = UOFontRenderer.Shared.MeasureString(_text.AsSpan(), _fontSettings, 1f, _maxWidth);

            Width += (int)Math.Max(textSize.X, _maxWidth);
            Height = (int) Math.Max(boundsInactive.Width, textSize.Y);

            CanMove = false;
            AcceptMouseInput = true;
        }

        public Checkbox(List<string> parts, string[] lines) : this(ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsChecked = parts[5] == "1";
            LocalSerial = SerialHelper.Parse(parts[6]);
            IsFromServer = true;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                }
            }
        }

        public override ClickPriority Priority => ClickPriority.High;

        public event EventHandler ValueChanged;


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            ResetHueVector();

            bool ok = base.Draw(batcher, x, y);

            var texture = GumpsLoader.Instance.GetGumpTexture(IsChecked ? _active : _inactive, out var bounds);

            Vector2 pos = new Vector2(x, y);

            batcher.Draw
            (
                texture,
                pos,
                bounds,
                HueVector
            );


            Vector3 hueVec = new Vector3(_hue, 0f, Alpha);
            if (_hue != 0)
            {
                hueVec.Y = 1f;
            }
            pos.X += bounds.Width + 2;

            UOFontRenderer.Shared.Draw
            (
                batcher,
                _text.AsSpan(),
                pos,
                1f,
                _fontSettings,
                hueVec,
                false,
                _maxWidth
            );

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
            ValueChanged.Raise(this);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && MouseIsOver)
            {
                IsChecked = !IsChecked;
            }
        }
    }
}