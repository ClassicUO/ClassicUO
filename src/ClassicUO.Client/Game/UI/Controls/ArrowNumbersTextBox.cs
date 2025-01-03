// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal class ArrowNumbersTextBox : Control
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