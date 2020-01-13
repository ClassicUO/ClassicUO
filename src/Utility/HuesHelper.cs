#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal static class HuesHelper
    {
        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        [MethodImpl(256)]
        public static (byte, byte, byte, byte) GetBGRA(uint cl)
        {
            return ((byte) (cl & 0xFF), // B
                    (byte) ((cl >> 8) & 0xFF), // G
                    (byte) ((cl >> 16) & 0xFF), // R
                    (byte) ((cl >> 24) & 0xFF) // A
                   );
        }

        [MethodImpl(256)]
        public static uint RgbaToArgb(uint rgba)
        {
            return (rgba >> 8) | (rgba << 24);
        }

        [MethodImpl(256)]
        public static uint Color16To32(ushort c)
        {
            return (uint) (_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16));
        }

        [MethodImpl(256)]
        public static ushort Color32To16(uint c)
        {
            return (ushort) ((((c & 0xFF) << 5) >> 8) | (((((c >> 16) & 0xFF) << 5) >> 8) << 10) | (((((c >> 8) & 0xFF) << 5) >> 8) << 5));
        }

        [MethodImpl(256)]
        public static ushort ConvertToGray(ushort c)
        {
            return (ushort) (((c & 0x1F) * 299 + ((c >> 5) & 0x1F) * 587 + ((c >> 10) & 0x1F) * 114) / 1000);
        }

        [MethodImpl(256)]
        public static ushort ColorToHue(Color c)
        {
            ushort origred = c.R;
            ushort origgreen = c.G;
            ushort origblue = c.B;
            const double scale = 31.0 / 255;
            ushort newred = (ushort)(origred * scale);

            if (newred == 0 && origred != 0)
                newred = 1;
            ushort newgreen = (ushort)(origgreen * scale);

            if (newgreen == 0 && origgreen != 0)
                newgreen = 1;
            ushort newblue = (ushort)(origblue * scale);

            if (newblue == 0 && origblue != 0)
                newblue = 1;

            ushort v = (ushort)((newred << 10) | (newgreen << 5) | (newblue));

            return v;
        }
    }
}