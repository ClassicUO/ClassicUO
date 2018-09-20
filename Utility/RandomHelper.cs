using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    public static class RandomHelper
    {
        private static readonly Random _random = new Random();

        public static int GetValue(int low, int high) => _random.Next(low, high + 1);
    }
}
