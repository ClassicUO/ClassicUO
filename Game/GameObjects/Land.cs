using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Views;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public class Land : GameObject
    {
        public Land(Graphic graphic) 
        {
            Graphic = graphic;
        }

        public LandTiles TileData => IO.Resources.TileData.LandData[Graphic];


        protected override View CreateView()
        {
            return new TileView(this);
        }

        public Rectangle Rectangle;

        public sbyte MinZ { get; set; }

        public sbyte AverageZ { get; set; }

        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;

        public bool IsStretched { get; set; }

        public void Calculate() => ((TileView)View).UpdateStreched(World.Map);

        public void UpdateZ(int zTop, int zRight, int zBottom)
        {
            if (IsStretched)
            {
                int x = Position.Z * 4 + 1;
                int y = zTop * 4;
                int w = zRight * 4 - x;
                int h = zBottom * 4 + 1 - y;
                Rectangle = new Rectangle(x, y, w, h);
                int average = AverageZ;

                if (Math.Abs(Position.Z - zRight) <= Math.Abs(zBottom - zTop))
                    AverageZ = (sbyte)((Position.Z + zRight) >> 1);
                else
                    AverageZ = (sbyte)((zBottom + zTop) >> 1);

                if (AverageZ != average)
                    Tile.ForceSort();
                MinZ = Position.Z;

                if (zTop < MinZ)
                    MinZ = (sbyte)zTop;

                if (zRight < MinZ)
                    MinZ = (sbyte)zRight;

                if (zBottom < MinZ)
                    MinZ = (sbyte)zBottom;
            }
        }

        public int CalculateCurrentAverageZ(int direction)
        {
            int result = GetDirectionZ(((byte)(direction >> 1) + 1) & 3);

            if ((direction & 1) > 0)
                return result;

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        private int GetDirectionZ(int direction)
        {
            switch (direction)
            {
                case 1: return Rectangle.Bottom / 4;
                case 2: return Rectangle.Right / 4;
                case 3: return Rectangle.Top / 4;
                default: return Position.Z;
            }
        }


    }
}
