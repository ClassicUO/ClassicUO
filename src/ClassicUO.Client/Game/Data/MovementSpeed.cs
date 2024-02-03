#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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