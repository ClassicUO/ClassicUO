using System.Globalization;

namespace ClassicUO.Utility
{
    internal static class UInt16Converter
    {
        public static ushort Parse(string str)
        {
            if (str.StartsWith("0x"))
            {
                return ushort.Parse(str.Remove(0, 2), NumberStyles.HexNumber);
            }

            if (str.Length > 1 && str[0] == '-')
            {
                return (ushort) short.Parse(str);
            }

            uint.TryParse(str, out uint v);

            return
                (ushort) v; // some server send 0xFFFF_FFFF in decimal form. C# doesn't like it. It needs a specific conversion
        }
    }
}