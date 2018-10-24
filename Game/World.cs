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

using System.Collections.Concurrent;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

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


        public static int MapIndex
        {
            get => Map?.Index ?? -1;
            set
            {
                if (MapIndex != value)
                {
                    InternalMapChangeClear(true);

                    if (Map != null)
                    {
                        if (MapIndex >= 0)
                            IO.Resources.Map.UnloadMap(MapIndex);

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

        public static IsometricLight Light { get; } = new IsometricLight();


        public static void Update(double totalMS, double frameMS)
        {
            if (Player != null)
            {
                foreach (Mobile mob in Mobiles)
                {
                    mob.Update(totalMS, frameMS);

                    if (mob.Distance > ViewRange)
                        RemoveMobile(mob);

                    if (mob.IsDisposed)
                        Mobiles.Remove(mob);
                }

                foreach (Item item in Items)
                {
                    item.Update(totalMS, frameMS);

                    if (item.Distance > ViewRange && item.OnGround)
                    {
                        if (_houses.ContainsKey(item))
                        {
                            if (item.Distance > ViewRange * 2 + 5)
                            {
                                RemoveItem(item);
                                _houses.TryRemove(item, out _);
                            }
                        }
                        else
                            RemoveItem(item);
                    }

                    if (item.IsDisposed)
                        Items.Remove(item);
                }

                //foreach (var k in _houses)
                //{
                //    if (k.Value.Distance > ViewRange * 2 + 5)
                //        k.Value.Dispose();

                //    if (k.Value.IsDisposed)
                //        _houses.TryRemove(k.Key, out _);
                //}
            }
        }


        public static House GetHouse(Serial serial)
        {
            _houses.TryGetValue(serial, out House h);
            return h;
        }

        public static House GetOrCreateHouse(Serial serial)
        {
            return _houses.TryGetValue(serial, out House house) ? house : new House(serial);
        }

        public static void AddOrUpdateHouse(House house)
        {
            _houses.TryAdd(house.Serial, house);
        }

        public static void RemoveHouse(Serial house)
        {
            _houses.TryRemove(house, out House h);
        }


        public static bool Contains(Serial serial)
        {
            if (serial.IsItem) return Items.Contains(serial);

            return serial.IsMobile && Mobiles.Contains(serial);
        }

        public static Entity Get(Serial serial)
        {
            if (serial.IsItem) return Items.Get(serial);

            return serial.IsMobile ? Mobiles.Get(serial) : null;
        }

        public static Item GetOrCreateItem(Serial serial)
        {
            Item item = Items.Get(serial);
            if (item == null || item.IsDisposed)
            {
                Items.Remove(serial);
                item = new Item(serial);
            }

            return item;
        }

        public static Mobile GetOrCreateMobile(Serial serial)
        {
            Mobile mob = Mobiles.Get(serial);
            if (mob == null || mob.IsDisposed)
            {
                Mobiles.Remove(serial);
                mob = new Mobile(serial);
            }

            return mob;
        }

        public static bool RemoveItem(Serial serial)
        {
            Item item = Items.Get(serial);
            if (item == null)
            {
                ToAdd.RemoveWhere(i => i == serial);
                return false;
            }

            if (item.Layer != Layer.Invalid && item.RootContainer.IsMobile)
            {
                Mobile mobile = Mobiles.Get(item.RootContainer);
                if (mobile != null) mobile.Equipment[(int) item.Layer] = null;
            }

            foreach (Item i in item.Items)
                RemoveItem(i);

            item.Items.Clear();
            item.Dispose();
            return true;
        }

        public static bool RemoveMobile(Serial serial)
        {
            Mobile mobile = Mobiles.Get(serial);
            if (mobile == null) return false;

            foreach (Item i in mobile.Items) RemoveItem(i);

            mobile.Items.Clear();
            mobile.Dispose();
            return true;
        }

        public static void Clear()
        {
            Items.Clear();
            Mobiles.Clear();
            Player = null;
            Map = null;
            ToAdd.Clear();
        }

        private static void InternalMapChangeClear(bool noplayer)
        {
            if (!noplayer)
            {
                Map = null;
                Player.Dispose();
                Player = null;
            }

            foreach (Item item in Items)
            {
                if (noplayer && Player != null && !Player.IsDisposed)
                {
                    if (item.RootContainer == Player)
                        continue;
                }

                RemoveItem(item);
            }

            foreach (Mobile mob in Mobiles)
            {
                if (noplayer && Player != null && !Player.IsDisposed)
                {
                    if (mob == Player)
                        continue;
                }

                RemoveMobile(mob);
            }
        }
    }
}