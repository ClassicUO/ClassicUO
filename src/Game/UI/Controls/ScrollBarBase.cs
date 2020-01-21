#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    abstract class ScrollBarBase : Control
    {
        protected int _value, _minValue, _maxValue;

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                _value = value;

                if (_value < MinValue)
                    _value = MinValue;
                else if (_value > MaxValue)
                    _value = MaxValue;

                ValueChanged.Raise();
            }
        }

        public int MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue == value)
                    return;

                _minValue = value;

                if (_value < _minValue)
                    _value = _minValue;
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue == value)
                    return;

                if (value < 0)
                    _maxValue = 0;
                else
                    _maxValue = value;

                if (_value > _maxValue)
                    _value = _maxValue;
            }
        }

        public int ScrollStep { get; set; } = 15;



        public event EventHandler ValueChanged;



        protected float GetSliderYPosition()
        {
            if (MaxValue == MinValue)
                return 0f;

            return GetScrollableArea() * ((Value - MinValue) / (float) (MaxValue - MinValue));
        }

        protected abstract float GetScrollableArea();
    }
}