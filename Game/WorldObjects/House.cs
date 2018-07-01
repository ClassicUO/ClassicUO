using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects
{
    public class House : Item
    {
        public House(Serial serial) : base(serial)
        {
            Items = new List<Static>();
        }

        public uint Revision { get; set; }

        public new List<Static> Items { get; }
    }
}
