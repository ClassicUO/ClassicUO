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

        private static int NORMAL_LENGTH = 22;

        private void CalculateNormal(Map.Map map, int x, int y, out Vector3 normal)
        {
            sbyte z = map.GetTileZ(x, y);
            sbyte leftZ = map.GetTileZ(x, y + 1);
            sbyte rightZ = map.GetTileZ(x + 1, y);

            Vector3 toLeft = new Vector3(0, NORMAL_LENGTH, (leftZ - z) * 4);
            Vector3 toRight = new Vector3(NORMAL_LENGTH, 0, (rightZ - z) * 4);

            Vector3.Cross(ref toRight, ref toLeft, out normal);
            Vector3.Normalize(ref normal, out normal);
        }

        private static Vector3 STRAIGHT_UP = new Vector3(0, 0, 1);
        private static float EPSILON = 0.0001f;

        private bool CloseEnough(Vector3 u, Vector3 v)
        {
            if (Math.Abs(u.X - v.X) > EPSILON)
            {
                return false;
            }

            if (Math.Abs(u.Y - v.Y) > EPSILON)
            {
                return false;
            }

            if (Math.Abs(u.Z - v.Z) > EPSILON)
            {
                return false;
            }

            return true;
        }

        public unsafe void ApplyStretch(Map.Map map, int x, int y, sbyte z)
        {
            if (IsStretched || TexmapsLoader.Instance.GetValidRefEntry(TileData.TexID).Length <= 0)
            {
                IsStretched = false;
                AverageZ = z;
                MinZ = z;
                return;
            }

            int zTop = z;
            int zRight = map.GetTileZ(x + 1, y);
            int zLeft = map.GetTileZ(x, y + 1);
            int zBottom = map.GetTileZ(x + 1, y + 1);

            YOffsets.Top = zTop * 4;
            YOffsets.Right = zRight * 4;
            YOffsets.Left = zLeft * 4;
            YOffsets.Bottom = zBottom * 4;

            if (Math.Abs(zTop - zBottom) <= Math.Abs(zLeft - zRight))
            {
                AverageZ = (sbyte)((zTop + zBottom) >> 1);
            }
            else
            {
                AverageZ = (sbyte)((zLeft + zRight) >> 1);
            }

            //AverageZ = (sbyte) Math.Floor((zTop + zRight + zLeft + zBottom) / 4f);
            MinZ = (sbyte) Math.Min(zTop, Math.Min(zRight, Math.Min(zLeft, zBottom)));
            
            CalculateNormal(map, x, y, out NormalTop);
            CalculateNormal(map, x + 1, y, out NormalRight);
            CalculateNormal(map, x, y + 1, out NormalLeft);
            CalculateNormal(map, x + 1, y + 1, out NormalBottom);

            if (CloseEnough(NormalTop, STRAIGHT_UP) && CloseEnough(NormalRight, STRAIGHT_UP) &&
                CloseEnough(NormalLeft, STRAIGHT_UP) && CloseEnough(NormalBottom, STRAIGHT_UP))
            {
                IsStretched = false;
            }
            else
            {
                IsStretched = true;
            }
        }
    }
}