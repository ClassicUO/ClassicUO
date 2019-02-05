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
    internal readonly struct Serial : IComparable<uint>
    {
        public bool Equals(Serial other)
        {
            return Value == other.Value;
        }

        public const uint INVALID = 0;
        public const uint MINUS_ONE = 0xFFFF_FFFF;

        public Serial(uint serial)
        {
            Value = serial;
        }

        public bool IsMobile => Value > 0 && Value < 0x40000000;

        public bool IsItem => Value >= 0x40000000 && Value < 0x80000000;

        public bool IsValid => Value > 0 && Value < 0x80000000;

        public uint Value { get; }

        public static implicit operator Serial(uint value)
        {
            return new Serial(value);
        }

        public static implicit operator uint(Serial serial)
        {
            return serial.Value;
        }

        public static bool operator ==(Serial s1, Serial s2)
        {
            return s1.Value == s2.Value;
        }

        public static bool operator !=(Serial s1, Serial s2)
        {
            return s1.Value != s2.Value;
        }

        public static bool operator <(Serial s1, Serial s2)
        {
            return s1.Value < s2.Value;
        }

        public static bool operator >(Serial s1, Serial s2)
        {
            return s1.Value > s2.Value;
        }

        public int CompareTo(uint other)
        {
            return Value.CompareTo(other);
        }

        public override string ToString()
        {
            return $"0x{Value:X8}";
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is Serial other && Equals(other);
        }

        public static Serial Parse(string str)
        {
            if (str.StartsWith("0x")) return uint.Parse(str.Remove(0, 2), NumberStyles.HexNumber);

            return uint.Parse(str);
        }
    }
}