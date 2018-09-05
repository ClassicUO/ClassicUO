using System;
using System.Globalization;

namespace ClassicUO.Game
{
    public struct Serial : IComparable, IComparable<uint>
    {
        public static readonly Serial Invalid = new Serial(0);
        public static readonly Serial MinusOne = new Serial(0xFFFFFFFF);

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

        public static bool operator ==(Serial s1,  Serial s2)
        {
            return s1.Value == s2.Value;
        }

        public static bool operator !=(Serial s1,  Serial s2)
        {
            return s1.Value != s2.Value;
        }

        public static bool operator <(Serial s1,  Serial s2)
        {
            return s1.Value < s2.Value;
        }

        public static bool operator >(Serial s1,  Serial s2)
        {
            return s1.Value > s2.Value;
        }

        public int CompareTo(object obj)
        {
            return Value.CompareTo(obj);
        }

        public int CompareTo(uint other)
        {
            return Value.CompareTo(other);
        }

        public override string ToString()
        {
            return string.Format("0x{0:X8}", Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Serial)
            {
                return this == (Serial)obj;
            }

            if (obj is uint)
            {
                return Value == (uint)obj;
            }

            return false;
        }

        public static Serial Parse(string str)
        {
            if (str.StartsWith("0x"))
            {
                return uint.Parse(str.Remove(0, 2), NumberStyles.HexNumber);
            }

            return uint.Parse(str);
        }
    }
}