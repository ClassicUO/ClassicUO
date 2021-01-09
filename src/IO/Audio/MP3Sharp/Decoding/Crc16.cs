#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
                {
                    m_Crc <<= 1;
                }
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