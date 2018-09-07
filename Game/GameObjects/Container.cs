using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.GameObjects
{
    public class Container : Item
    {
        private readonly Dictionary<Serial, Item> _items = new Dictionary<Serial, Item>();

        public Container(Serial serial) : base(serial)
        {

        }

        public virtual void Add(Item item)
        {

        }

        public virtual void Remove(Serial serial)
        {

        }

        public bool Exists(Serial serial) => _items.ContainsKey(serial);

        public int Count => _items.Count;
    }
}
