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

using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     16-Bit CRC checksum
    /// </summary>
    internal sealed class Crc16
    {
        private static readonly short Polynomial;
        private short m_Crc;

        static Crc16()
        {
            Polynomial = (short) SupportClass.Identity(0x8005);
        }

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public Crc16()
        {
            m_Crc = (short) SupportClass.Identity(0xFFFF);
        }

        /// <summary>
        ///     Feed a bitstring to the crc calculation (length between 0 and 32, not inclusive).
        /// </summary>
        public void add_bits(int bitstring, int length)
        {
            int bitmask = 1 << (length - 1);

            do
            {
                if (((m_Crc & 0x8000) == 0) ^ ((bitstring & bitmask) == 0))
                {
                    m_Crc <<= 1;
                    m_Crc ^= Polynomial;
                }
                else
                    m_Crc <<= 1;
            } while ((bitmask = SupportClass.URShift(bitmask, 1)) != 0);
        }

        /// <summary>
        ///     Return the calculated checksum.
        ///     Erase it for next calls to add_bits().
        /// </summary>
        public short Checksum()
        {
            short sum = m_Crc;
            m_Crc = (short) SupportClass.Identity(0xFFFF);

            return sum;
        }
    }
}