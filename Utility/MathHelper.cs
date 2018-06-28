using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    public static class MathHelper
    {
        public static bool InRange(int input, int low, int high) => input >= low && input <= high;
    }
}
