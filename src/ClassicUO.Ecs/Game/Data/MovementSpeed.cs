// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Data
{
    internal static class MovementSpeed
    {
        public const int STEP_DELAY_MOUNT_RUN = 100;
        public const int STEP_DELAY_MOUNT_WALK = 200;
        public const int STEP_DELAY_RUN = 200;
        public const int STEP_DELAY_WALK = 400;

        public static int TimeToCompleteMovement(bool run, bool mounted)
        {
            if (mounted)
            {
                return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;
            }

            return run ? STEP_DELAY_RUN : STEP_DELAY_WALK;
        }

        public static void GetPixelOffset(byte dir, ref float x, ref float y, float framesPerTile)
        {
            float step_NESW_D = 44.0f / framesPerTile;
            float step_NESW = 22.0f / framesPerTile;
            int checkX = 22;
            int checkY = 22;

            switch (dir & 7)
            {
                case 0: //W
                {
                    x *= step_NESW;
                    y *= -step_NESW;

                    break;
                }

                case 1: //NW
                {
                    x *= step_NESW_D;
                    checkX = 44;
                    y = 0.0f;

                    break;
                }

                case 2: //N
                {
                    x *= step_NESW;
                    y *= step_NESW;

                    break;
                }

                case 3: //NE
                {
                    x = 0.0f;
                    y *= step_NESW_D;
                    checkY = 44;

                    break;
                }

                case 4: //E
                {
                    x *= -step_NESW;
                    y *= step_NESW;

                    break;
                }

                case 5: //SE
                {
                    x *= -step_NESW_D;
                    checkX = 44;
                    y = 0.0f;

                    break;
                }

                case 6: //S
                {
                    x *= -step_NESW;
                    y *= -step_NESW;

                    break;
                }

                case 7: //SW
                {
                    x = 0.0f;
                    y *= -step_NESW_D;
                    checkY = 44;

                    break;
                }
            }

            int valueX = (int) x;

            if (Math.Abs(valueX) > checkX)
            {
                if (valueX < 0)
                {
                    x = -checkX;
                }
                else
                {
                    x = checkX;
                }
            }

            int valueY = (int) y;

            if (Math.Abs(valueY) > checkY)
            {
                if (valueY < 0)
                {
                    y = -checkY;
                }
                else
                {
                    y = checkY;
                }
            }
        }
    }
}