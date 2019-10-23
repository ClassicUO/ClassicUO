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
    internal readonly struct Graphic : IComparable<ushort>
    {
        public bool Equals(Graphic other)
        {
            return Value == other.Value;
        }

        public const ushort INVARIANT = ushort.MaxValue;
        public const ushort INVALID = 0xFFFF;

        public Graphic(ushort graphic)
        {
            Value = graphic;
        }

        public readonly ushort Value;

        public static implicit operator Graphic(ushort value)
        {
            return new Graphic(value);
        }

        public static implicit operator ushort(Graphic color)
        {
            return color.Value;
        }

        public static bool operator ==(Graphic g1, Graphic g2)
        {
            return g1.Value == g2.Value;
        }

        public static bool operator !=(Graphic g1, Graphic g2)
        {
            return g1.Value != g2.Value;
        }

        public static bool operator <(Graphic g1, Graphic g2)
        {
            return g1.Value < g2.Value;
        }

        public static bool operator >(Graphic g1, Graphic g2)
        {
            return g1.Value > g2.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is Graphic other && Equals(other);
        }

        public int CompareTo(ushort other)
        {
            return Value.CompareTo(other);
        }

        public override string ToString()
        {
            return $"{Value} (0x{Value:X4})";
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static Graphic Parse(string str)
        {
            if (str.StartsWith("0x"))
                return ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber);

            if (str.Length > 1 && str[0] == '-')
                return (ushort) short.Parse(str);


            return (ushort) uint.Parse(str); // some server send 0xFFFF_FFFF in decimal form. C# doesn't like it. It needs a specific conversion
        }
    }
}
