using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClassicUO.Game
{
    public struct Hue : IComparable, IComparable<ushort>
    {
        public const ushort Invariant = ushort.MaxValue;
        public static Hue SystemCol = new Hue(0x3B2);
        public static Hue Good = new Hue(68);
        public static Hue Error = new Hue(37);
        public static Hue Warning = new Hue(1174);

        private readonly ushort _value;
        public Hue(ushort hue) { _value = hue; }

        public bool IsInvariant { get { return _value == Invariant; } }

        public static implicit operator Hue(ushort value) { return new Hue(value); }
        public static implicit operator ushort(Hue color) { return color._value; }
        public static bool operator ==(Hue h1, Hue h2) { return h1.IsInvariant || h2.IsInvariant || h1._value == h2._value; }
        public static bool operator !=(Hue h1, Hue h2) { return !h1.IsInvariant && !h2.IsInvariant && h1._value != h2._value; }
        public static bool operator <(Hue h1, Hue h2) { return h1._value < h2._value; }
        public static bool operator >(Hue h1, Hue h2) { return h1._value > h2._value; }

        public int CompareTo(object obj) { return _value.CompareTo(obj); }
        public int CompareTo(ushort other) { return _value.CompareTo(other); }

        public override string ToString() { return $"0x{_value:X4}"; }
        public override int GetHashCode() { return _value.GetHashCode(); }
        public override bool Equals(object obj)
        {
            if (obj is Hue)
                return this == (Hue)obj;
            if (obj is ushort)
                return _value == (ushort)obj;
            return false;
        }

        public static Hue Parse(string str)
        {
            if (str.StartsWith("0x"))
                return ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber);
            return ushort.Parse(str);
        }
    }
}
