using System;
using System.Globalization;

namespace ClassicUO.Game
{
    public struct Graphic : IComparable, IComparable<ushort>
    {
        public const ushort Invariant = ushort.MaxValue;
        public static Graphic Invalid = new Graphic(-1);

        private readonly ushort _value;

        public Graphic(in ushort graphic)
        {
            _value = graphic;
        }

        public Graphic(in int graphic)
        {
            _value = (ushort) graphic;
        }

        public bool IsInvariant => _value == Invariant;

        public static implicit operator Graphic(in ushort value)
        {
            return new Graphic(value);
        }

        public static implicit operator ushort(in Graphic color)
        {
            return color._value;
        }

        public static bool operator ==(in Graphic g1, in Graphic g2)
        {
            return g1.IsInvariant || g2.IsInvariant || g1._value == g2._value;
        }

        public static bool operator !=(in Graphic g1, in Graphic g2)
        {
            return !g1.IsInvariant && !g2.IsInvariant && g1._value != g2._value;
        }

        public static bool operator <(in Graphic g1, in Graphic g2)
        {
            return g1._value < g2._value;
        }

        public static bool operator >(in Graphic g1, in Graphic g2)
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
            if (obj is Graphic)
                return this == (Graphic) obj;
            if (obj is ushort)
                return _value == (ushort) obj;
            return false;
        }

        public static Graphic Parse(in string str)
        {
            if (str.StartsWith("0x"))
                return ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber);
            return ushort.Parse(str);
        }
    }
}