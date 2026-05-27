// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game
{
    internal static class Pathfinder
    {
        public static void GetNewXY(byte direction, ref int x, ref int y)
        {
            switch (direction & 7)
            {
                case 0: y--; break;
                case 1: x++; y--; break;
                case 2: x++; break;
                case 3: x++; y++; break;
                case 4: y++; break;
                case 5: x--; y++; break;
                case 6: x--; break;
                case 7: x--; y--; break;
            }
        }
    }
}
