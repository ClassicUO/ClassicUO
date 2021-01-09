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

using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InRange(int input, int low, int high)
        {
            return input >= low && input <= high;
        }

        public static int GetDistance(Point current, Point target)
        {
            int distx = Math.Abs(target.X - current.X);
            int disty = Math.Abs(target.Y - current.Y);

            if (disty > distx)
            {
                distx = disty;
            }

            return distx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Combine(int val1, int val2)
        {
            return (ulong) val1 | ((ulong) val2 << 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetNumbersFromCombine(ulong b, out int val1, out int val2)
        {
            val1 = (int) (0xFFFFFFFF & b);
            val2 = (int) (b >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PercetangeOf(int current, int max)
        {
            if (current <= 0 || max <= 0)
            {
                return 0;
            }

            return current * 100 / max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PercetangeOf(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                {
                    max = 100;
                }

                if (max > 1)
                {
                    max = maxValue * max / 100;
                }
            }

            return max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Hypotenuse(float a, float b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }
    }
}