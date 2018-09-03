using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ClassicUO.Game
{
    public static class World
    {
        private static readonly ConcurrentDictionary<Serial, House> _houses = new ConcurrentDictionary<Serial, House>();
        public static HashSet<Item> ToAdd { get; } = new HashSet<Item>();
        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();
        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();
        public static PlayerMobile Player { get; set; }
        public static Facet Map { get; private set; }
        public static byte ViewRange { get; set; } = 24;

        public static List<GameText> OverHeads { get; } = new List<GameText>();

        public static int MapIndex
        {
            get => Map?.Index ?? -1;
            set
            {
                if (MapIndex != value)
                {
                    Clear(true);

                    if (Map != null)
                    {
                        Position position = Player.Position;
                        Player.Map = null;
                        Map = null;
                        Map = new Facet(value);
                        Player.Map = Map;
                        Player.Position = position;
                        Player.ClearSteps();
                        Player.ProcessDelta();
                    }
                    else
                    {
                        Map = new Facet(value);
                        if (Player != null)
                        {
                            Player.Map = Map;
                            Map.Center = new Point(Player.Position.X, Player.Position.Y);
                        }
                    }
                }
            }
        }


        public static bool InGame => Player != null && Map != null;

        public static long Ticks { get; set; }
        public static IsometricLight Light { get; } = new IsometricLight();


        public static void Update(in double frameMS)
        {
            if (Player != null)
            {
                foreach (Mobile mob in Mobiles)
                {
                    mob.View.Update(frameMS);

                    if (mob.Distance > ViewRange)
                    {
                        mob.Dispose();
                        RemoveMobile(mob);
                    }
                }

                foreach (Item item in Items)
                {
                    item.View.Update(frameMS);

                    if (item.Distance > ViewRange && item.OnGround)
                    {
                        item.Dispose();
                        RemoveItem(item);
                    }
                }
            }
        }


        public static House GetHouse(in Serial serial)
        {
            _houses.TryGetValue(serial, out House h);
            return h;
        }

        public static House GetOrCreateHouse(in Serial serial)
        {
            if (_houses.TryGetValue(serial, out House house))
            {
                return house;
            }

            return new House(serial);
        }

        public static void AddOrUpdateHouse(in House house)
        {
            _houses.TryAdd(house.Serial, house);
        }

        public static void RemoveHouse(in Serial house)
        {
            _houses.TryRemove(house, out House h);
        }


        public static bool Contains(in Serial serial)
        {
            if (serial.IsItem)
            {
                return Items.Contains(serial);
            }

            if (serial.IsMobile)
            {
                return Mobiles.Contains(serial);
            }

            return false;
        }

        public static Entity Get(in Serial serial)
        {
            if (serial.IsItem)
            {
                return Items.Get(serial);
            }

            if (serial.IsMobile)
            {
                return Mobiles.Get(serial);
            }

            return null;
        }

        public static Item GetOrCreateItem(in Serial serial)
        {
            return Items.Get(serial) ?? new Item(serial);
        }

        public static Mobile GetOrCreateMobile(in Serial serial)
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

            if (item.Layer != Layer.Invalid && item.RootContainer.IsMobile)
            {
                var mobile = Mobiles.Get(item.RootContainer);
                if (mobile != null)
                {
                    mobile.Equipment[(int)item.Layer] = null;
                }
            }


            foreach (Item i in item.Items)
            {
                RemoveItem(i);
            }

            item.Items.Clear();

            item.Dispose();
            return true;
        }

        public static bool RemoveMobile(in Serial serial)
        {
            Mobile mobile = Mobiles.Remove(serial);
            if (mobile == null)
            {
                return false;
            }

            foreach (Item i in mobile.Items)
            {
                RemoveItem(i);
            }

            mobile.Items.Clear();

            mobile.Dispose();
            return true;
        }

        public static void Clear(in bool noplayer = false)
        {
            OverHeads.Clear();

            if (!noplayer)
            {
                Map = null;
                Player = null;
            }

            foreach (Item item in Items)
            {
                if (noplayer)
                {
                    if (item.RootContainer == Player)
                    {
                        continue;
                    }
                }

                RemoveItem(item);
            }

            foreach (Mobile mob in Mobiles)
            {
                if (noplayer)
                {
                    if (mob == Player)
                    {
                        continue;
                    }
                }

                RemoveMobile(mob);
            }
        }
    }
}