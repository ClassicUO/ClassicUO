#region license

// Copyright (c) 2024, andreakarasho
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

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    public static class HuesHelper
    {
        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B,
            0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RgbaToArgb(uint rgba)
        {
            return (rgba >> 8) | (rgba << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Color16To32(ushort c)
        {
            return (uint) (_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16) | ((c >> 15) * 0xFF) << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Color32To16(uint c)
        {
            return (ushort) ((((c & 0xFF) << 5) >> 8) | (((((c >> 16) & 0xFF) << 5) >> 8) << 10) | (((((c >> 8) & 0xFF) << 5) >> 8) << 5));
        }
    }
}