﻿using System.Runtime.CompilerServices;

namespace ClassicUO.Utility
{
    internal static class HuesHelper
    {
        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte, byte, byte, byte) GetBGRA(uint cl)
        {
            return ((byte) (cl & 0xFF), // B
                    (byte) ((cl >> 8) & 0xFF), // G
                    (byte) ((cl >> 16) & 0xFF), // R
                    (byte) ((cl >> 24) & 0xFF) // A
                   );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RgbaToArgb(uint rgba)
        {
            return (rgba >> 8) | (rgba << 24);
        }


        public static uint Color16To32(ushort c)
        {
            return (uint) (_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16));
        }

        public static ushort Color32To16(uint c)
        {
            return (ushort) ((((c & 0xFF) * 32) >> 8) | (((((c >> 16) & 0xFF) * 32) >> 8) << 10) | (((((c >> 8) & 0xFF) * 32) >> 8) << 5));
        }

        public static ushort ConvertToGray(ushort c)
        {
            return (ushort) (((c & 0x1F) * 299 + ((c >> 5) & 0x1F) * 587 + ((c >> 10) & 0x1F) * 114) / 1000);
        }
    }
}