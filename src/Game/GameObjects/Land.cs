#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land : GameObject
    {
        private static Vector3[,,] _vectCache = new Vector3[3, 3, 4];
        private static readonly Queue<Land> _pool = new Queue<Land>();

        static Land()
        {
            for (int i = 0; i < Constants.PREDICTABLE_TILE_COUNT; i++)
                _pool.Enqueue(new Land());
        }

        public static Land Create(ushort graphic)
        {
            if (_pool.Count != 0)
            {
                var l = _pool.Dequeue();
                l.Graphic = graphic;
                l.OriginalGraphic = graphic;
                l.IsDestroyed = false;
                l.AlphaHue = 255;
                l.IsStretched = l.TileData.TexID == 0 && l.TileData.IsWet;
                l.AllowedToDraw = l.Graphic > 2;
                l.Normal0 = l.Normal1 = l.Normal2 = l.Normal3 = Vector3.Zero;
                l.Rectangle = Rectangle.Empty;
                l.MinZ = l.AverageZ = 0;
                l.Texture = null;
                l.Bounds = Rectangle.Empty;
                return l;
            }

            Log.Debug(string.Intern("Created new Land"));

            return new Land(graphic);
        }




        private Land()
        {

        }

        public ushort OriginalGraphic;

        private Land(ushort graphic)
        {
            OriginalGraphic = Graphic = graphic;
            IsStretched = TileData.TexID == 0 && TileData.IsWet;

            AllowedToDraw = Graphic > 2;

            AlphaHue = 255;
        }

       

        public override void Destroy()
        {
            if (IsDestroyed)
                return;

            base.Destroy();
            _pool.Enqueue(this);
        }

        public Vector3 Normal0, Normal1, Normal2, Normal3;
        public Rectangle Rectangle;

        public ref LandTiles TileData => ref TileDataLoader.Instance.LandData[Graphic];

        public sbyte MinZ;
        public sbyte AverageZ;
        public bool IsStretched;


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
                int y = (zTop << 2);
                int w = (zRight << 2) - x;
                int h = (zBottom << 2) + 1 - y;

                Rectangle.X = x;
                Rectangle.Y = y;
                Rectangle.Width = w;
                Rectangle.Height = h;

                if (Math.Abs(currentZ - zRight) <= Math.Abs(zBottom - zTop))
                    AverageZ = (sbyte) ((currentZ + zRight) >> 1);
                else
                    AverageZ = (sbyte) ((zBottom + zTop) >> 1);

                MinZ = currentZ;

                if (zTop < MinZ)
                    MinZ = (sbyte) zTop;

                if (zRight < MinZ)
                    MinZ = (sbyte) zRight;

                if (zBottom < MinZ)
                    MinZ = (sbyte) zBottom;
            }
        }

        public int CalculateCurrentAverageZ(int direction)
        {
            int result = GetDirectionZ(((byte) (direction >> 1) + 1) & 3);

            if ((direction & 1) != 0)
                return result;

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        [MethodImpl(256)]
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


        public void ApplyStrech(int x, int y, sbyte z)
        {
            Map.Map map = World.Map;

            if (IsStretched || TexmapsLoader.Instance.GetTexture(TileData.TexID) == null || !TestStretched(x, y, z, true))
            {
                IsStretched = false;
                MinZ = z;
            }
            else
            {
                IsStretched = true;
                UpdateZ(
                    map.GetTileZ(x, y + 1),
                    map.GetTileZ(x + 1, y + 1),
                    map.GetTileZ(x + 1, y),
                    z);

                //Vector3[,,] vec = new Vector3[3, 3, 4];

                int i;
                int j;

                for (i = -1; i < 2; i++)
                {
                    int curX = x + i;
                    int curI = i + 1;

                    for (j = -1; j < 2; j++)
                    {
                        int curY = y + j;
                        int curJ = j + 1;
                        sbyte currentZ = map.GetTileZ(curX, curY);
                        sbyte leftZ = map.GetTileZ(curX, curY + 1);
                        sbyte rightZ = map.GetTileZ(curX + 1, curY);
                        sbyte bottomZ = map.GetTileZ(curX + 1, curY + 1);

                        if (currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                ref var v = ref _vectCache[curI, curJ, k];
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

                            ref var v0 = ref _vectCache[curI, curJ, 0];
                            v0.X = -22;
                            v0.Y = 22;
                            v0.Z = half_0;
                            MergeAndNormalize(ref v0, -22.0f, -22.0f, half_1);


                            ref var v1 = ref _vectCache[curI, curJ, 1];
                            v1.X = 22;
                            v1.Y = 22;
                            v1.Z = half_2;
                            MergeAndNormalize(ref v1, -22.0f, 22.0f, half_0);

                            ref var v2 = ref _vectCache[curI, curJ, 2];
                            v2.X = 22;
                            v2.Y = -22;
                            v2.Z = half_3;
                            MergeAndNormalize(ref v2, 22.0f, 22.0f, half_2);

                            ref var v3 = ref _vectCache[curI, curJ, 3];
                            v3.X = -22;
                            v3.Y = -22;
                            v3.Z = half_1;
                            MergeAndNormalize(ref v3, 22.0f, -22.0f, half_3);
                        }
                    }
                }

                i = 1;
                j = 1;

                // 0
                SumAndNormalize(
                     ref _vectCache,
                     i - 1, j - 1, 2,
                     i - 1, j, 1,
                     i, j - 1, 3,
                     i, j, 0,
                     out Normal0);

                // 1
                SumAndNormalize(
                    ref _vectCache,
                    i, j - 1, 2,
                    i, j, 1,
                    i + 1, j - 1, 3,
                    i + 1, j, 0,
                    out Normal1);

                // 2
                SumAndNormalize(
                    ref _vectCache,
                    i, j, 2,
                    i, j + 1, 1,
                    i + 1, j, 3,
                    i + 1, j + 1, 0,
                    out Normal2);

                // 3
                SumAndNormalize(
                    ref _vectCache,
                    i - 1, j, 2,
                    i - 1, j + 1, 1,
                    i, j, 3,
                    i, j + 1, 0,
                    out Normal3);
            }
        }


        [MethodImpl(256)]
        private static void SumAndNormalize(
            ref Vector3[,,] vec,
            int index0_x, int index0_y, int index0_z,
            int index1_x, int index1_y, int index1_z,
            int index2_x, int index2_y, int index2_z,
            int index3_x, int index3_y, int index3_z,
            out Vector3 result)
        {
            Vector3.Add(ref vec[index0_x, index0_y, index0_z], ref vec[index1_x, index1_y, index1_z], out var v0Result);
            Vector3.Add(ref vec[index2_x, index2_y, index2_z], ref vec[index3_x, index3_y, index3_z], out var v1Result);
            Vector3.Add(ref v0Result, ref v1Result, out result);
            Vector3.Normalize(ref result, out result);
        }

        private static bool TestStretched(int x, int y, sbyte z, bool recurse)
        {
            bool result = false;

            for (int i = -1; i < 2 && !result; i++)
            {
                for (int j = -1; j < 2 && !result; j++)
                {
                    if (recurse)
                        result = TestStretched(x + i, y + j, z, false);
                    else
                    {
                        sbyte testZ = World.Map.GetTileZ(x + i, y + j);
                        result = testZ != z && testZ != -125;
                    }
                }
            }

            return result;
        }

        [MethodImpl(256)]
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