// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;

namespace ClassicUO.Game
{
    internal static class GameCursor
    {
        public static int GetMouseDirection(int x1, int y1, int to_x, int to_y, int current_facing)
        {
            int shiftX = to_x - x1;
            int shiftY = to_y - y1;
            int hashf = 100 * (Sgn(shiftX) + 2) + 10 * (Sgn(shiftY) + 2);

            if (shiftX != 0 && shiftY != 0)
            {
                shiftX = Math.Abs(shiftX);
                shiftY = Math.Abs(shiftY);

                if (shiftY * 5 <= shiftX * 2)
                {
                    hashf = hashf + 1;
                }
                else if (shiftY * 2 >= shiftX * 5)
                {
                    hashf = hashf + 3;
                }
                else
                {
                    hashf = hashf + 2;
                }
            }
            else if (shiftX == 0)
            {
                if (shiftY == 0)
                {
                    return current_facing;
                }
            }

            switch (hashf)
            {
                case 111: return (int)Direction.West;
                case 112: return (int)Direction.Up;
                case 113: return (int)Direction.North;
                case 120: return (int)Direction.West;
                case 131: return (int)Direction.West;
                case 132: return (int)Direction.Left;
                case 133: return (int)Direction.South;
                case 210: return (int)Direction.North;
                case 230: return (int)Direction.South;
                case 311: return (int)Direction.East;
                case 312: return (int)Direction.Right;
                case 313: return (int)Direction.North;
                case 320: return (int)Direction.East;
                case 331: return (int)Direction.East;
                case 332: return (int)Direction.Down;
                case 333: return (int)Direction.South;
            }

            return current_facing;
        }

        private static int Sgn(int val)
        {
            int a = 0 < val ? 1 : 0;
            int b = val < 0 ? 1 : 0;
            return a - b;
        }
    }
}
