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

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Data
{
    internal static class MovementSpeed
    {
        private const int STEP_DELAY_MOUNT_RUN = 100;
        private const int STEP_DELAY_MOUNT_WALK = 200;
        private const int STEP_DELAY_RUN = 200;
        private const int STEP_DELAY_WALK = 400;

        public static int TimeToCompleteMovement(Mobile mobile, bool run)
        {
            bool mounted = mobile.IsMounted || mobile.SpeedMode == CharacterSpeedType.FastUnmount || mobile.SpeedMode == CharacterSpeedType.FastUnmountAndCantRun || mobile.IsFlying;

            if (mounted) return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;

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
                    x = -checkX;
                else
                    x = checkX;
            }

            int valueY = (int) y;

            if (Math.Abs(valueY) > checkY)
            {
                if (valueY < 0)
                    y = -checkY;
                else
                    y = checkY;
            }
        }
    }
}