#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Game.Views;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public class Land : GameObject
    {
        public Rectangle Rectangle;

        public Land(Graphic graphic)
        {
            Graphic = graphic;
        }

        private LandTiles? _tileData;


        public LandTiles TileData
        {
            get
            {
                if (!_tileData.HasValue)
                    _tileData = IO.Resources.TileData.LandData[Graphic];

                return _tileData.Value;
            }
        }

        public sbyte MinZ { get; set; }

        public sbyte AverageZ { get; set; }

        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;

        public bool IsStretched { get; set; }

        protected override View CreateView()
        {
            return new TileView(this);
        }

        public void Calculate(int x, int y, sbyte z)
        {
            ((TileView) View).UpdateStreched(x, y ,z);
        }

        public void UpdateZ(int zTop, int zRight, int zBottom, sbyte currentZ)
        {
            if (IsStretched)
            {
                int x = currentZ * 4 + 1;
                int y = zTop * 4;
                int w = zRight * 4 - x;
                int h = zBottom * 4 + 1 - y;
                Rectangle = new Rectangle(x, y, w, h);
                //int average = AverageZ;

                if (Math.Abs(currentZ - zRight) <= Math.Abs(zBottom - zTop))
                    AverageZ = (sbyte) ((currentZ + zRight) >> 1);
                else
                    AverageZ = (sbyte) ((zBottom + zTop) >> 1);

                //if (AverageZ != average)
                //    Tile.ForceSort();
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

            if ((direction & 1) > 0)
                return result;

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        private int GetDirectionZ(int direction)
        {
            switch (direction)
            {
                case 1: return Rectangle.Bottom >> 2;
                case 2: return Rectangle.Right >> 2;
                case 3: return Rectangle.Top >> 2;
                default: return Position.Z;
            }
        }
    }
}