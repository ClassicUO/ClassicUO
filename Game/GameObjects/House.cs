using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
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