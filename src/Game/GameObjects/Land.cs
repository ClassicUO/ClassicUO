﻿#region license

// Copyright (c) 2021, andreakarasho
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
using System.Runtime.CompilerServices;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land : GameObject
    {
        private static readonly QueuedPool<Land> _pool = new QueuedPool<Land>
        (
            Constants.PREDICTABLE_TILE_COUNT,
            l =>
            {
                l.IsDestroyed = false;
                l.AlphaHue = 255;
                l.Normal0 = l.Normal1 = l.Normal2 = l.Normal3 = Vector3.Zero;
                l.Rectangle = Rectangle.Empty;
                l.MinZ = l.AverageZ = 0;
            }
        );

        public ref LandTiles TileData => ref TileDataLoader.Instance.LandData[Graphic];
        public sbyte AverageZ;
        public bool IsStretched;

        public sbyte MinZ;


        public Vector3 Normal0, Normal1, Normal2, Normal3;
        public ushort OriginalGraphic;
        public Rectangle Rectangle;


        public static Land Create(ushort graphic)
        {
            Land land = _pool.GetOne();
            land.Graphic = graphic;
            land.OriginalGraphic = graphic;
            land.IsStretched = land.TileData.TexID == 0 && land.TileData.IsWet;
            land.AllowedToDraw = graphic > 2;
            land.UpdateGraphicBySeason();

            return land;
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Destroy();
            _pool.ReturnOne(this);
        }

        public override void UpdateGraphicBySeason()
        {
            Graphic = SeasonManager.GetLandSeasonGraphic(World.Season, OriginalGraphic);
            AllowedToDraw = Graphic > 2;
        }

        public void UpdateZ(int zTop, int zRight, int zBottom, sbyte currentZ)
        {
            if (IsStretched)
            {
                int x = (currentZ << 2) + 1;
                int y = zTop << 2;
                int w = (zRight << 2) - x;
                int h = (zBottom << 2) + 1 - y;

                Rectangle.X = x;
                Rectangle.Y = y;
                Rectangle.Width = w;
                Rectangle.Height = h;

                if (Math.Abs(currentZ - zRight) <= Math.Abs(zBottom - zTop))
                {
                    AverageZ = (sbyte) ((currentZ + zRight) >> 1);
                }
                else
                {
                    AverageZ = (sbyte) ((zBottom + zTop) >> 1);
                }

                MinZ = currentZ;

                if (zTop < MinZ)
                {
                    MinZ = (sbyte) zTop;
                }

                if (zRight < MinZ)
                {
                    MinZ = (sbyte) zRight;
                }

                if (zBottom < MinZ)
                {
                    MinZ = (sbyte) zBottom;
                }
            }
        }

        public int CalculateCurrentAverageZ(int direction)
        {
            int result = GetDirectionZ(((byte) (direction >> 1) + 1) & 3);

            if ((direction & 1) != 0)
            {
                return result;
            }

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetDirectionZ(int direction)
        {
            switch (direction)
            {
                case 1: return Rectangle.Bottom >> 2;
                case 2: return Rectangle.Right >> 2;
                case 3: return Rectangle.Top >> 2;
                default: return Z;
            }
        }


        public unsafe void ApplyStretch(Map.Map map, int x, int y, sbyte z)
        {
            if (IsStretched || TexmapsLoader.Instance.GetTexture(TileData.TexID) == null || !TestStretched(x, y, z, true))
            {
                IsStretched = false;
                MinZ = z;
            }
            else
            {
                IsStretched = true;

                UpdateZ(map.GetTileZ(x, y + 1), map.GetTileZ(x + 1, y + 1), map.GetTileZ(x + 1, y), z);

                int i;
                int j;

                const int SIZE = 3 * 3 * 4;
                Vector3* vectBuffer = stackalloc Vector3[SIZE];

                for (i = -1; i < 2; ++i)
                {
                    int curX = x + i;
                    int curI = i + 1;

                    for (j = -1; j < 2; ++j)
                    {
                        int curY = y + j;
                        int curJ = j + 1;
                        sbyte currentZ = map.GetTileZ(curX, curY);
                        sbyte leftZ = map.GetTileZ(curX, curY + 1);
                        sbyte rightZ = map.GetTileZ(curX + 1, curY);
                        sbyte bottomZ = map.GetTileZ(curX + 1, curY + 1);

                        if (currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ)
                        {
                            for (int k = 0; k < 4; ++k)
                            {
                                ref Vector3 v = ref vectBuffer[k + curJ * 3 + curI * 3 * 4];
                                v.X = 0;
                                v.Y = 0;
                                v.Z = 1;
                            }
                        }
                        else
                        {
                            int half_0 = (currentZ - rightZ) << 2;
                            int half_1 = (leftZ - currentZ) << 2;
                            int half_2 = (rightZ - bottomZ) << 2;
                            int half_3 = (bottomZ - leftZ) << 2;

                            ref Vector3 v0 = ref vectBuffer[0 + curJ * 3 + curI * 3 * 4];
                            v0.X = -22;
                            v0.Y = 22;
                            v0.Z = half_0;
                            MergeAndNormalize(ref v0, -22.0f, -22.0f, half_1);

                            ref Vector3 v1 = ref vectBuffer[1 + curJ * 3 + curI * 3 * 4];
                            v1.X = 22;
                            v1.Y = 22;
                            v1.Z = half_2;
                            MergeAndNormalize(ref v1, -22.0f, 22.0f, half_0);

                            ref Vector3 v2 = ref vectBuffer[2 + curJ * 3 + curI * 3 * 4];
                            v2.X = 22;
                            v2.Y = -22;
                            v2.Z = half_3;
                            MergeAndNormalize(ref v2, 22.0f, 22.0f, half_2);

                            ref Vector3 v3 = ref vectBuffer[3 + curJ * 3 + curI * 3 * 4];
                            v3.X = -22;
                            v3.Y = -22;
                            v3.Z = half_1;
                            MergeAndNormalize(ref v3, 22.0f, -22.0f, half_3);
                        }
                    }
                }

                SumAndNormalize
                (
                    ref vectBuffer[2 + 0 * 3 + 0 * 3 * 4], 
                    ref vectBuffer[1 + 1 * 3 + 0 * 3 * 4], 
                    ref vectBuffer[3 + 0 * 3 + 1 * 3 * 4], 
                    ref vectBuffer[0 + 1 * 3 + 1 * 3 * 4], 
                    out Normal0
                );

                SumAndNormalize
                (
                    ref vectBuffer[2 + 0 * 3 + 1 * 3 * 4],
                    ref vectBuffer[1 + 1 * 3 + 1 * 3 * 4],
                    ref vectBuffer[3 + 0 * 3 + 2 * 3 * 4],
                    ref vectBuffer[0 + 1 * 3 + 2 * 3 * 4],
                    out Normal1
                );

                SumAndNormalize
                (
                    ref vectBuffer[2 + 1 * 3 + 1 * 3 * 4],
                    ref vectBuffer[1 + 2 * 3 + 1 * 3 * 4],
                    ref vectBuffer[3 + 1 * 3 + 2 * 3 * 4],
                    ref vectBuffer[0 + 2 * 3 + 2 * 3 * 4],
                    out Normal2
                );

                SumAndNormalize
                (
                    ref vectBuffer[2 + 1 * 3 + 0 * 3 * 4],
                    ref vectBuffer[1 + 2 * 3 + 0 * 3 * 4],
                    ref vectBuffer[3 + 1 * 3 + 1 * 3 * 4],
                    ref vectBuffer[0 + 2 * 3 + 1 * 3 * 4],
                    out Normal3
                );
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SumAndNormalize
        (
            ref Vector3 v0,
            ref Vector3 v1,
            ref Vector3 v2,
            ref Vector3 v3,
            out Vector3 result
        )
        {
            Vector3.Add(ref v0, ref v1, out v0);
            Vector3.Add(ref v2, ref v3, out v1);
            Vector3.Add(ref v0, ref v1, out result);
            Vector3.Normalize(ref result, out result);
        }

       private static bool TestStretched(int x, int y, sbyte z, bool recurse)
        {
            bool result = false;

            for (int i = -1; i < 2 && !result; ++i)
            {
                for (int j = -1; j < 2 && !result; ++j)
                {
                    if (recurse)
                    {
                        result = TestStretched(x + i, y + j, z, false);
                    }
                    else
                    {
                        sbyte testZ = World.Map.GetTileZ(x + i, y + j);
                        result = testZ != z && testZ != -125;
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MergeAndNormalize(ref Vector3 v, float x, float y, float z)
        {
            float newX = v.Y * z - v.Z * y;
            float newY = v.Z * x - v.X * z;
            float newZ = v.X * y - v.Y * x;
            v.X = newX;
            v.Y = newY;
            v.Z = newZ;

            Vector3.Normalize(ref v, out v);
        }
    }
}