#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal static class MathHelper
    {
        [MethodImpl(256)]
        public static bool InRange(int input, int low, int high)
        {
            return input >= low && input <= high;
        }

        public static int GetDistance(Point current, Point target)
        {
            int distx = Math.Abs(target.X - current.X);
            int disty = Math.Abs(target.Y - current.Y);

            if (disty > distx)
                distx = disty;

            return distx;
        }

        [MethodImpl(256)]
        public static ulong Combine(int val1, int val2)
        {
            return (ulong) val1 | ((ulong) val2 << 32);
        }

        [MethodImpl(256)]
        public static void GetNumbersFromCombine(ulong b, out int val1, out int val2)
        {
            val1 = (int) (0xFFFFFFFF & b);
            val2 = (int) (b >> 32);
        }

        [MethodImpl(256)]
        public static int PercetangeOf(int current, int max)
        {
            return current / max * 100;
        }

        [MethodImpl(256)]
        public static double Hypotenuse(float a, float b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }
    }
}