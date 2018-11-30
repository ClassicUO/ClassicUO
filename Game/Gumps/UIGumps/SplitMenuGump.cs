﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class SplitMenuGump : Gump
    {
        private readonly HSliderBar _slider;
        private int _lastValue;
        private readonly Button _okButton;
        private readonly TextBox _textBox;
        private readonly Point _offsert;

        public SplitMenuGump(Item item, Point offset) : base(item, 0)
        {
            Item = item;
            _offsert = offset;

            CanMove = true;
            AcceptMouseInput = false;

            GumpPic background = new GumpPic(0, 0, 0x085C, 0);
            AddChildren(background);
            AddChildren(_slider = new HSliderBar(29, 16, 105, 0, item.Amount,item.Amount, HSliderBarStyle.BlueWidgetNoBar));
            _lastValue = _slider.Value;

            AddChildren(_okButton = new Button(0, 0x085d, 0x085e, 0x085f)
            {
                ButtonAction = ButtonAction.Default,
                X = 102, Y = 37
            });

            _okButton.MouseClick += OkButtonOnMouseClick;

            AddChildren(_textBox = new TextBox(1, isunicode: false, hue: 0x0386, width: 60, maxWidth: 1000)
            {
                X = 29, Y = 42,
                NumericOnly = true,
            });
            _textBox.SetText(item.Amount.ToString());
        }

        public Item Item { get; }

        private void OkButtonOnMouseClick(object sender, MouseEventArgs e)
        {
            if (_slider.Value > 0)
            {
                GameActions.PickUp(Item, _offsert, _slider.Value);
            }
            Dispose();
        }

        public override void OnKeybaordReturn(int textID, string text)
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
            base.Dispose();
        }

    }
}
