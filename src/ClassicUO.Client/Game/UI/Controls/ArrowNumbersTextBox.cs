#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    public class ArrowNumbersTextBox : Control
    {
        private const int TIME_BETWEEN_CLICKS = 250;
        private readonly int _Min, _Max;
        private readonly StbTextBox _textBox;
        private uint _timeUntilNextClick;
        private readonly Button _up, _down;

        public ArrowNumbersTextBox
        (
            int x,
            int y,
            int width,
            int raiseamount,
            int minvalue,
            int maxvalue,
            byte font = 0,
            int maxcharlength = -1,
            bool isunicode = true,
            FontStyle style = FontStyle.None,
            ushort hue = 0
        )
        {
            int height = 20;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _Min = minvalue;
            _Max = maxvalue;

            Add
            (
                new ResizePic(0x0BB8)
                {
                    Width = width,
                    Height = height + 4
                }
            );

            _up = new Button(raiseamount, 0x983, 0x984)
            {
                X = width - 12,
                ButtonAction = ButtonAction.Activate
            };

            _up.MouseDown += (sender, e) =>
            {
                if (_up.IsClicked)
                {
                    UpdateValue();
                    _timeUntilNextClick = TIME_BETWEEN_CLICKS * 2;
                }
            };

            Add(_up);

            _down = new Button(-raiseamount, 0x985, 0x986)
            {
                X = width - 12,
                Y = height - 7,
                ButtonAction = ButtonAction.Activate
            };

            _down.MouseDown += (sender, e) =>
            {
                if (_down.IsClicked)
                {
                    UpdateValue();
                    _timeUntilNextClick = TIME_BETWEEN_CLICKS * 2;
                }
            };

            Add(_down);

            Add
            (
                _textBox = new StbTextBox
                (
                    font,
                    maxcharlength,
                    width,
                    isunicode,
                    style,
                    hue
                )
                {
                    X = 2,
                    Y = 2,
                    Height = height,
                    Width = width - 17,
                    NumbersOnly = true
                }
            );
        }

        internal string Text
        {
            get => _textBox?.Text ?? string.Empty;
            set => _textBox?.SetText(value);
        }

        private void UpdateValue()
        {
            int.TryParse(_textBox.Text, out int i);

            if (_up.IsClicked)
            {
                i += _up.ButtonID;
            }
            else
            {
                i += _down.ButtonID;
            }

            ValidateValue(i);
        }

        internal override void OnFocusLost()
        {
            if (IsDisposed)
            {
                return;
            }

            int.TryParse(_textBox.Text, out int i);
            ValidateValue(i);
        }

        private void ValidateValue(int val)
        {
            Tag = val = Math.Max(_Min, Math.Min(_Max, val));
            _textBox.SetText(val.ToString());
        }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_up.IsClicked || _down.IsClicked)
            {
                if (Time.Ticks > _timeUntilNextClick)
                {
                    _timeUntilNextClick = Time.Ticks + TIME_BETWEEN_CLICKS;

                    UpdateValue();
                }
            }

            base.Update();
        }
    }
}