// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SplitMenuGump : Gump
    {
        private bool _firstChange;
        private int _lastValue;
        private readonly Point _offset;
        private readonly Button _okButton;
        private readonly HSliderBar _slider;
        private readonly StbTextBox _textBox;
        private bool _updating;


        public SplitMenuGump(World world, uint serial, Point offset) : base(world, serial, 0)
        {
            Item item = World.Items.Get(serial);

            if (item == null || item.IsDestroyed)
            {
                Dispose();

                return;
            }

            _offset = offset;

            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;

            GumpPic background = new GumpPic(0, 0, 0x085C, 0) { ContainsByBounds = true };
            Add(background);

            Add
            (
                _slider = new HSliderBar
                (
                    29,
                    16,
                    105,
                    1,
                    item.Amount,
                    item.Amount,
                    HSliderBarStyle.BlueWidgetNoBar
                )
            );

            _lastValue = _slider.Value;

            Add
            (
                _okButton = new Button(0, 0x085d, 0x085e, 0x085f)
                {
                    ButtonAction = ButtonAction.Default,
                    X = 102, Y = 37
                }
            );

            _okButton.MouseUp += OkButtonOnMouseClick;

            Add
            (
                _textBox = new StbTextBox(1, isunicode: false, hue: 0x0386, maxWidth: 60)
                {
                    X = 29, Y = 42,
                    Width = 60,
                    Height = 20,
                    NumbersOnly = true
                }
            );

            _textBox.SetText(item.Amount.ToString());
            _textBox.TextChanged += (sender, args) => { UpdateText(); };
            _textBox.SetKeyboardFocus();
            _slider.ValueChanged += (sender, args) => { UpdateText(); };
        }

        private void UpdateText()
        {
            if (_updating)
            {
                return;
            }

            _updating = true;

            if (_slider.Value != _lastValue)
            {
                _textBox.SetText(_slider.Value.ToString());
            }
            else
            {
                if (_textBox.Text.Length == 0)
                {
                    _slider.Value = _slider.MinValue;
                }
                else if (!int.TryParse(_textBox.Text, out int textValue))
                {
                    _textBox.SetText(_slider.Value.ToString());
                }
                else
                {
                    if (textValue != _slider.Value)
                    {
                        if (textValue <= _slider.MaxValue)
                        {
                            _slider.Value = textValue;
                        }
                        else
                        {
                            if (!_firstChange)
                            {
                                string last = _textBox.Text[_textBox.Text.Length - 1].ToString();

                                _slider.Value = int.Parse(last);
                                _firstChange = true;
                            }
                            else
                            {
                                _slider.Value = _slider.MaxValue;
                            }

                            _textBox.SetText(_slider.Value.ToString());
                        }
                    }
                }
            }

            _lastValue = _slider.Value;

            _updating = false;
        }

        private void OkButtonOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                PickUp();
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            PickUp();
        }

        private void PickUp()
        {
            if (_slider.Value > 0)
            {
                GameActions.PickUp(World, LocalSerial, _offset.X, _offset.Y, _slider.Value);
            }

            Dispose();
        }

        public override void Update()
        {
            Item item = World.Items.Get(LocalSerial);

            if (item == null || item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed)
            {
                return;
            }

            base.Update();
        }

        public override void Dispose()
        {
            if (_okButton != null)
            {
                _okButton.MouseUp -= OkButtonOnMouseClick;
            }

            base.Dispose();
        }
    }
}