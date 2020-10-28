using System;

namespace ClassicUO.Utility
{
    internal static class RandomHelper
    {
        private static readonly Random _random = new Random();

        /// <summary>
        ///     Returns a random number between low and high, inclusive of both low and high.
        /// </summary>
        public static int GetValue(int low, int high)
        {
            return _random.Next(low, high + 1);
        }

        /// <summary>
        ///     Returns a non-negative random integer.
        /// </summary>
        public static int GetValue()
        {
            return _random.Next();
        }
    }
}