using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    public static class MathHelper
    {
        public static bool InRange(in int input, in int low, in int high) => input >= low && input <= high;
    }
}
