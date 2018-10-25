#region license

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
using System.Globalization;

namespace ClassicUO.Game
{
    public struct Graphic : IComparable, IComparable<ushort>
    {
        public const ushort Invariant = ushort.MaxValue;
        public static Graphic Invalid = new Graphic(-1);
        private readonly ushort _value;

        public Graphic(ushort graphic)
        {
            _value = graphic;
        }

        public Graphic(int graphic)
        {
            _value = (ushort) graphic;
        }

        public bool IsInvariant => _value == Invariant;

        public static implicit operator Graphic(ushort value)
        {
            return new Graphic(value);
        }

        public static implicit operator ushort(Graphic color)
        {
            return color._value;
        }

        public static bool operator ==(Graphic g1, Graphic g2)
        {
            return g1._value == g2._value;
        }

        public static bool operator !=(Graphic g1, Graphic g2)
        {
            return g1._value != g2._value;
        }

        public static bool operator <(Graphic g1, Graphic g2)
        {
            return g1._value < g2._value;
        }

        public static bool operator >(Graphic g1, Graphic g2)
        {
            return g1._value > g2._value;
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
            return string.Format("0x{0:X4}", _value);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Graphic graphic:

                    return this == graphic;
                case ushort @ushort:

                    return _value == @ushort;
                default:

                    return false;
            }
        }

        public static Graphic Parse(string str)
        {
            if (str.StartsWith("0x")) return ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber);

            return ushort.Parse(str);
        }
    }
}