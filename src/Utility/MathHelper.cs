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
            {
                distx = disty;
            }

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
            if (current <= 0 || max <= 0)
            {
                return 0;
            }

            return current * 100 / max;
        }

        [MethodImpl(256)]
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

        [MethodImpl(256)]
        public static double Hypotenuse(float a, float b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }
    }
}