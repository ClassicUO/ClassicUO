using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.WorldObjects;

namespace ClassicUO.Game
{
    public static class World
    {
        public static HashSet<Item> ToAdd { get; } = new HashSet<Item>();
        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();
        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();
        public static PlayerMobile Player { get; set; }



        public static bool Contains(Serial serial)
        {
            if (serial.IsItem)
                return Items.Contains(serial);
            if (serial.IsMobile)
                return Mobiles.Contains(serial);
            return false;
        }

        public static Entity Get(Serial serial)
        {
            if (serial.IsItem)
                return Items.Get(serial);
            if (serial.IsMobile)
                return Mobiles.Get(serial);
            return null;
        }

        public static Item GetOrCreateItem(Serial serial)
        {
            return Items.Get(serial) ?? new Item(serial);
        }

        public static Mobile GetOrCreateMobile(Serial serial)
        {
            return Mobiles.Get(serial) ?? new Mobile(serial);
        }

        public static bool RemoveItem(Serial serial)
        {
            Item item = Items.Remove(serial);
            if (item == null)
            {
                ToAdd.RemoveWhere(i => i == serial);
                return false;
            }

            foreach (Item i in item.Items)
                RemoveItem(i);
            item.Items.Clear();
            return true;
        }

        public static bool RemoveMobile(Serial serial)
        {
            Mobile mobile = Mobiles.Remove(serial);
            if (mobile == null)
                return false;

            foreach (Item i in mobile.Items)
                RemoveItem(i);
            mobile.Items.Clear();
            return true;
        }

        public static void Clear()
        {
            Player = null;
            Items.Clear();
            Mobiles.Clear();
        }
    }
}
