using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    public sealed class MultiInfo
    {
        public MultiInfo(short x, short y)
        {
            X = x;
            Y = y;
        }

        public short X { get; }

        public short Y { get; }

        public short MinX { get; set; }

        public short MaxX { get; set; }

        public short MinY { get; set; }

        public short MaxY { get; set; }
    }
}
