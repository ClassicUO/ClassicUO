// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    public static class MathHelper
    {
        public static readonly float MachineEpsilonFloat = GetMachineEpsilonFloat();


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
        public static ulong Combine(uint val1, uint val2)
        {
            return (val1 | (val2 << 32));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleBetweenVectors(Vector2 from, Vector2 to)
        {
            return (float) Math.Atan2(to.Y - from.Y, to.X - from.X);
        }

        private static float GetMachineEpsilonFloat()
        {
            float machineEpsilon = 1.0f;
            float comparison;

            /* Keep halving the working value of machineEpsilon until we get a number that
			 * when added to 1.0f will still evaluate as equal to 1.0f.
			 */
            do
            {
                machineEpsilon *= 0.5f;
                comparison = 1.0f + machineEpsilon;
            }
            while (comparison > 1.0f);

            return machineEpsilon;
        }
    }
}