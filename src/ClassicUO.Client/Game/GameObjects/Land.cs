// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land : GameObject
    {
        //private static readonly QueuedPool<Land> _pool = new QueuedPool<Land>
        //(
        //    Constants.PREDICTABLE_TILE_COUNT,
        //    l =>
        //    {
        //        l.IsDestroyed = false;
        //        l.AlphaHue = 255;
        //        l.NormalTop = l.NormalRight = l.NormalLeft = l.NormalBottom = Vector3.Zero;
        //        l.YOffsets.Top = l.YOffsets.Right = l.YOffsets.Left = l.YOffsets.Bottom = 0;
        //        l.MinZ = l.AverageZ = 0;
        //    }
        //);

        public ref LandTiles TileData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Client.Game.UO.FileManager.TileData.LandData[Graphic];
        }
        public sbyte AverageZ;
        public bool IsStretched;
        public sbyte MinZ;
        public Vector3 NormalTop, NormalRight, NormalLeft, NormalBottom;
        public ushort OriginalGraphic;
        public UltimaBatcher2D.YOffsets YOffsets;

        private Land(World world) : base(world) { }

        public static Land Create(World world, ushort graphic)
        {
            Land land = new Land(world); // _pool.GetOne();
            land.AlphaHue = 0xFF;
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
            //_pool.ReturnOne(this);
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
            if (IsStretched || Client.Game.UO.FileManager.Texmaps.File.GetValidRefEntry(TileData.TexID).Length <= 0)
            {
                IsStretched = false;
                AverageZ = z;
                MinZ = z;

                return;
            }

            /*  _____ _____
             * | top | rig |
             * |_____|_____|
             * | lef | bot |
             * |_____|_____|
             */
            sbyte zTop = z;
            sbyte zRight = map.GetTileZ(x + 1, y);
            sbyte zLeft = map.GetTileZ(x, y + 1);
            sbyte zBottom = map.GetTileZ(x + 1, y + 1);

            YOffsets.Top = zTop * 4;
            YOffsets.Right = zRight * 4;
            YOffsets.Left = zLeft * 4;
            YOffsets.Bottom = zBottom * 4;

            if (Math.Abs(zTop - zBottom) <= Math.Abs(zLeft - zRight))
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
             * | t01 |  z  | t21 | t31 |
             * |_____|_____|_____|_____|
             * | t02 | t12 | t22 | t32 |
             * |_____|_____|_____|_____|
             * |     | t13 | t23 |     |
             * |_____|_____|_____|_____|
             */
            sbyte t10 = map.GetTileZ(x, y - 1);
            sbyte t20 = map.GetTileZ(x + 1, y - 1);
            sbyte t01 = map.GetTileZ(x - 1, y);
            sbyte t21 = zRight;
            sbyte t31 = map.GetTileZ(x + 2, y);
            sbyte t02 = map.GetTileZ(x - 1, y + 1);
            sbyte t12 = zLeft;
            sbyte t22 = zBottom;
            sbyte t32 = map.GetTileZ(x + 2, y + 1);
            sbyte t13 = map.GetTileZ(x, y + 2);
            sbyte t23 = map.GetTileZ(x + 1, y + 2);


            IsStretched |= CalculateNormal(z, t10, t21, t12, t01, out NormalTop);
            IsStretched |= CalculateNormal(t21, t20, t31, t22, z, out NormalRight);
            IsStretched |= CalculateNormal(t22, t21, t32, t23, t12, out NormalBottom);
            IsStretched |= CalculateNormal(t12, z, t22, t13, t02, out NormalLeft);
        }

        private static bool CalculateNormal(sbyte tile, sbyte top, sbyte right, sbyte bottom, sbyte left, out Vector3 normal)
        {
            if (tile == top && tile == right && tile == bottom && tile == left)
            {
                normal.X = 0;
                normal.Y = 0;
                normal.Z = 1f;

                return false;
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
            // ==========================


            // ==========================
            u.X = -22;
            u.Y = 22;
            u.Z = (bottom - tile) * 4;

            v.X = 22;
            v.Y = 22;
            v.Z = (right - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
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
            Vector3.Add(ref ret, ref normal, out ret);
            // ==========================


            Vector3.Normalize(ref ret, out normal);

            return true;
        }
    }
}
