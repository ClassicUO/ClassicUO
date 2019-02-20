#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class SplitMenuGump : Gump
    {
        private readonly HSliderBar _slider;
        private int _lastValue;
        private readonly Button _okButton;
        private readonly TextBox _textBox;
        private readonly Point _offsert;
        private readonly WeakReference _itemGump;

        private bool _firstChange;

        public SplitMenuGump(Item item, Point offset) : base(item, 0)
        {
            Item = item;

            ItemGump itemGump = Engine.UI.GetChildByLocalSerial<ContainerGump, ItemGump>(item.Container, item.Serial);
            if(itemGump != null)
            {
                _itemGump = new WeakReference(itemGump);
                itemGump.Disposed += ItemGumpOnDisposed;
            }
            _offsert = offset;

            CanMove = true;
            AcceptMouseInput = false;

            GumpPic background = new GumpPic(0, 0, 0x085C, 0);
            Add(background);
            Add(_slider = new HSliderBar(29, 16, 105, 1, item.Amount,item.Amount, HSliderBarStyle.BlueWidgetNoBar));
            _lastValue = _slider.Value;

            Add(_okButton = new Button(0, 0x085d, 0x085e, 0x085f)
            {
                ButtonAction = ButtonAction.Default,
                X = 102, Y = 37
            });

            _okButton.MouseClick += OkButtonOnMouseClick;

            Add(_textBox = new TextBox(1, isunicode: false, hue: 0x0386, width: 60, maxWidth: 1000)
            {
                X = 29, Y = 42,
                Width = 60,
                AllowValidateRules = (uint)(Constants.RULES.NUMERIC),
            });
            _textBox.SetText(item.Amount.ToString());
        }

        public Item Item { get; }

        private void ItemGumpOnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }


        private void OkButtonOnMouseClick(object sender, MouseEventArgs e)
        {
            if (_slider.Value > 0)
            {
                GameActions.PickUp(Item, _offsert, _slider.Value);
            }
            Dispose();
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (_slider.Value > 0)
            {
                GameActions.PickUp(Item, _offsert, _slider.Value);
            }
            Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            base.Update(totalMS, frameMS);

            if (_slider.Value != _lastValue)
                _textBox.SetText(_slider.Value.ToString());
            else
            {
                if (_textBox.Text.Length == 0)
                    _slider.Value = _slider.MinValue;
                else if (!int.TryParse(_textBox.Text, out int textValue))
                    _textBox.SetText(_slider.Value.ToString());
                else
                {
                    if (textValue != _slider.Value)
                    {
                        if (textValue <= _slider.MaxValue)
                            _slider.Value = textValue;
                        else
                        {
                            if (!_firstChange)
                            {
                                string last = _textBox.Text[_textBox.Text.Length - 1].ToString();
                                _slider.Value = int.Parse(last);
                                _firstChange = true;
                            }
                            else
                                _slider.Value = _slider.MaxValue;
                            _textBox.SetText(_slider.Value.ToString());
                        }
                    }
                }
            }

            _lastValue = _slider.Value;
        }

        public override void Dispose()
        {
            _okButton.MouseClick -= OkButtonOnMouseClick;

            if(_itemGump != null && _itemGump.IsAlive)
            {
                ItemGump gump = _itemGump.Target as ItemGump;
                if (gump != null)
                {
                    gump.Disposed -= ItemGumpOnDisposed;
                }
            }

            base.Dispose();
        }

    }
}
