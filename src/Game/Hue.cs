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
using System.Globalization;

namespace ClassicUO.Game
{
    internal struct Hue : IComparable, IComparable<ushort>
    {
        public const ushort INVALID = 0xFFFF;
       
        private readonly ushort _value;

        public Hue(ushort hue)
        {
            _value = hue;
        }

        public static implicit operator Hue(ushort value)
        {
            return new Hue(value);
        }

        public static implicit operator ushort(Hue color)
        {
            return color._value;
        }

        public static bool operator ==(Hue h1, Hue h2)
        {
            return h1._value == h2._value;
        }

        public static bool operator !=(Hue h1, Hue h2)
        {
            return h1._value != h2._value;
        }

        public static bool operator <(Hue h1, Hue h2)
        {
            return h1._value < h2._value;
        }

        public static bool operator >(Hue h1, Hue h2)
        {
            return h1._value > h2._value;
        }

        public int CompareTo(object obj)
        {
            return _value.CompareTo(obj);
        }

        public int CompareTo(ushort other)
        {
            return _value.CompareTo(other);
        }

        public override string ToString()
        {
            return $"0x{_value:X4}";
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Hue hue: return _value == hue._value;
                case ushort @ushort: return _value == @ushort;
                default: return false;
            }
        }

        public static Hue Parse(string str)
        {
            return str.StartsWith("0x") ? ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber) : ushort.Parse(str);
        }
    }
}