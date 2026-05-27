// SPDX-License-Identifier: BSD-2-Clause

using System.Globalization;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game
{
    internal static class SerialHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(uint serial)
        {
            return serial > 0 && serial < 0x80000000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMobile(uint serial)
        {
            return serial > 0 && serial < 0x40000000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsItem(uint serial)
        {
            return serial >= 0x40000000 && serial < 0x80000000;
        }

        public static uint Parse(string str)
        {
            if (str.StartsWith("0x"))
            {
                return uint.Parse(str.Remove(0, 2), NumberStyles.HexNumber);
            }

            if (str.Length > 1 && str[0] == '-')
            {
                return (uint) int.Parse(str);
            }

            return uint.Parse(str);
        }
    }
}