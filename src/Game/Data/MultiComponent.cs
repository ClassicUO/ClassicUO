using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    public struct MultiComponent
    {
        public MultiComponent(Graphic graphic, ushort x, ushort y, sbyte z, uint flags)
        {
            Graphic = graphic;
            Position = new Position(x, y, z);
            Flags = flags;
        }

        public Graphic Graphic { get; }

        public uint Flags { get; }

        public Position Position { get; set; }
    }
}
