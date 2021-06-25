#region license

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
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land : GameObject
    {
        private static Vector3 STRAIGHT_UP = new Vector3(0, 0, 1);
        private const float EPSILON = 0.0001f;

        private static readonly QueuedPool<Land> _pool = new QueuedPool<Land>
        (
            Constants.PREDICTABLE_TILE_COUNT,
            l =>
            {
                l.IsDestroyed = false;
                l.AlphaHue = 255;
                l.NormalTop = l.NormalRight = l.NormalLeft = l.NormalBottom = Vector3.Zero;
                l.YOffsets.Top = l.YOffsets.Right = l.YOffsets.Left = l.YOffsets.Bottom = 0;
                l.MinZ = l.AverageZ = 0;
            }
        );

        public ref LandTiles TileData => ref TileDataLoader.Instance.LandData[Graphic];
        public sbyte AverageZ;
        public bool IsStretched;

        public sbyte MinZ;


        public Vector3 NormalTop, NormalRight, NormalLeft, NormalBottom;
        public ushort OriginalGraphic;
        public UltimaBatcher2D.YOffsets YOffsets;

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
                case 1: return YOffsets.Right >> 2;
                case 2: return YOffsets.Bottom >> 2;
                case 3: return YOffsets.Left >> 2;
                default: return Z;
            }
        }

        public void ApplyStretch(Map.Map map, int x, int y, sbyte z)
        {
            if (IsStretched || TexmapsLoader.Instance.GetValidRefEntry(TileData.TexID).Length <= 0)
            {
                IsStretched = false;
                AverageZ = z;
                MinZ = z;
                return;
            }

            /*  _____ _____
             * | cur | rig |
             * |_____|_____|
             * | lef | bot |
             * |_____|_____|
             *
             */

            sbyte zTop = z;
            sbyte zRight = map.GetTileZ(x + 1, y);
            sbyte zLeft = map.GetTileZ(x, y + 1);
            sbyte zBottom = map.GetTileZ(x + 1, y + 1);

            YOffsets.Top = zTop * 4;
            YOffsets.Right = zRight * 4;
            YOffsets.Left = zLeft * 4;
            YOffsets.Bottom = zBottom * 4;

            if (Math.Abs(zTop - zBottom) > Math.Abs(zLeft - zRight))
            {
                AverageZ = (sbyte) ((zTop + zBottom) >> 1);
            }
            else
            {
                AverageZ = (sbyte) ((zLeft + zRight) >> 1);
            }

            MinZ = Math.Min(zTop, Math.Min(zRight, Math.Min(zLeft, zBottom)));


            /*  _____ _____ _____ _____
             * |     | t10 | t20 |     |
             * |_____|_____|_____|_____|
             * | t01 | cur | t21 | t31 |
             * |_____|_____|_____|_____|
             * | t02 | t12 | t22 | t32 |
             * |_____|_____|_____|_____|
             * |     | t13 | t23 |     |
             * |_____|_____|_____|_____|
             */
            var cur = z;
            var t10 = map.GetTileZ(x, y - 1);
            var t20 = map.GetTileZ(x + 1, y - 1);
            var t01 = map.GetTileZ(x - 1, y);
            var t21 = zRight;
            var t31 = map.GetTileZ(x + 2, y);
            var t02 = map.GetTileZ(x - 1, y + 1);
            var t12 = map.GetTileZ(x, y + 1);
            var t22 = zBottom;
            var t32 = map.GetTileZ(x + 2, y + 1);
            var t13 = map.GetTileZ(x, y + 2);
            var t23 = map.GetTileZ(x + 1, y + 2);


            CalculateNormal(cur, t10, t21, t12, t01, out NormalTop);
            CalculateNormal(t21, t20, t31, t22, cur, out NormalRight);
            CalculateNormal(t22, t21, t32, t23, t12, out NormalBottom);
            CalculateNormal(t12, cur, t22, t13, t02, out NormalLeft);

            //IsStretched = !(zLeft == zRight && zLeft == zBottom && zLeft == z);

            if (CloseEnough(ref NormalTop, ref STRAIGHT_UP) &&
                CloseEnough(ref NormalRight, ref STRAIGHT_UP) &&
                CloseEnough(ref NormalLeft, ref STRAIGHT_UP) &&
                CloseEnough(ref NormalBottom, ref STRAIGHT_UP))
            {
                IsStretched = false;
            }
            else
            {
                IsStretched = true;
            }
        }

        private static void CalculateNormal(sbyte tile, sbyte top, sbyte right, sbyte bottom, sbyte left, out Vector3 normal)
        {
            if (tile == top && tile == right && tile == bottom && tile == left)
            {
                normal.X = 0;
                normal.Y = 0;
                normal.Z = 1f;

                return;
            }

            Vector3 u = new Vector3();
            Vector3 v = new Vector3();
            Vector3 ret = new Vector3();


            // ========================== 
            u.X = -22;
            u.Y = -22;
            u.Z = (left - tile) * 4;

            v.X = -22;
            v.Y = 22;
            v.Z = (bottom - tile) * 4;

            Vector3.Cross(ref v, ref u, out ret);
           // Vector3.Normalize(ref normal, out Vector3 ret);
            // ========================== 


            // ========================== 
            u.X = -22;
            u.Y = 22;
            u.Z = (bottom - tile) * 4;

            v.X = 22;
            v.Y = 22;
            v.Z = (right - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
           // Vector3.Normalize(ref normal, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ========================== 


            // ========================== 
            u.X = 22;
            u.Y = 22;
            u.Z = (right - tile) * 4;

            v.X = 22;
            v.Y = -22;
            v.Z = (top - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
           // Vector3.Normalize(ref normal, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ========================== 


            // ========================== 
            u.X = 22;
            u.Y = -22;
            u.Z = (top - tile) * 4;

            v.X = -22;
            v.Y = -22;
            v.Z = (left - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
            //Vector3.Normalize(ref normal, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ========================== 

            Vector3.Normalize(ref ret, out normal);
        }

        private static void CalculateNormal(Map.Map map, int x, int y, out Vector3 normal)
        {
            //sbyte org = map.GetTileZ(x, y);

            //sbyte n1 = map.GetTileZ(x, y - 1);
            //sbyte n2 = map.GetTileZ(x - 1, y);
            //sbyte n3 = map.GetTileZ(x + 1, y);
            //sbyte n4 = map.GetTileZ(x, y + 1);

            //Vector3 v0 = new Vector3(-22, 00, hl)

            //normal.X = 1.0f * (hr - hl);
            //normal.Y = 1.0f * (hd - hu);
            //normal.Z = 44.0f;


            //sbyte hll = map.GetTileZ(x - 1, y - 1);
            //sbyte hrr = map.GetTileZ(x + 1, y + 1);
            //sbyte hdd = map.GetTileZ(x + 1, y - 1);
            //sbyte huu = map.GetTileZ(x - 1, y + 1);

            //normal.X = (hl - hd) + (hll - hdd);
            //normal.Y = (hr - hu) + (hrr - huu);
            //normal.Z = 22f;

            //Vector3 vec0 = new Vector3(0, -22, (n1 - org) * 4);
            //Vector3 vec1 = new Vector3(-22, 0, (n2 - org) * 4);
            //Vector3 vec2 = new Vector3(22, 0, (n3 - org) * 4);
            //Vector3 vec3 = new Vector3(0, 22, (n4 - org) * 4);

            //Vector3.Subtract(ref vec1, ref vec2, out vec1);
            //Vector3.Subtract(ref vec0, ref vec3, out vec0);



            //Vector3.Cross(ref vec1, ref vec0, out vec0);
            //Vector3.Cross(ref vec3, ref vec2, out vec1);


            // https://stackoverflow.com/questions/44120220/calculating-normals-on-terrain-mesh
            //float sizeFactor = 1.0f / (8.0f * 44f);

            //sbyte nw = map.GetTileZ(x - 1, y - 1);
            //sbyte n = map.GetTileZ(x - 1, y);
            //sbyte ne = map.GetTileZ(x - 1, y + 1);
            //sbyte e = map.GetTileZ(x, y + 1);
            //sbyte se = map.GetTileZ(x + 1, y + 1);
            //sbyte s = map.GetTileZ(x + 1, y);
            //sbyte sw = map.GetTileZ(x + 1, y - 1);
            //sbyte w = map.GetTileZ(x, y - 1);

            //float dydx = ((ne + 2 * e + se) - (nw + 2 * w + sw)) * sizeFactor;
            //float dydz = ((sw + 2 * s + se) - (nw + 2 * n + ne)) * sizeFactor;

            //normal.X = dydx;
            //normal.Y = dydz;
            //normal.Z = 22f;



            sbyte z = map.GetTileZ(x, y);
            sbyte leftZ = map.GetTileZ(x, y + 1);
            sbyte rightZ = map.GetTileZ(x + 1, y);

            Vector3 toLeft = new Vector3(0, 22, (leftZ - z) * 4);
            Vector3 toRight = new Vector3(22, 0, (rightZ - z) * 4);

            Vector3.Cross(ref toRight, ref toLeft, out normal);



            //sbyte toBottom = map.GetTileZ(x + 1, y + 1);
            //Vector3 to


            Vector3.Normalize(ref normal, out normal);
        }

        private static bool CloseEnough(ref Vector3 u, ref Vector3 v)
        {
            if (Math.Abs(u.X - v.X) > 0f)
            {
                return false;
            }

            if (Math.Abs(u.Y - v.Y) > 0f)
            {
                return false;
            }

            if (Math.Abs(u.Z - v.Z) > 0f)
            {
                return false;
            }

            return true;
        }
    }
}