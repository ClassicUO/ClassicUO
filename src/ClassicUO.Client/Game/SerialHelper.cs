#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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