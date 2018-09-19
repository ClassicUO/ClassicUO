#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
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

        public static long Ticks { get; set; }
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
                        RemoveItem(item);

                    if (item.IsDisposed)
                        Items.Remove(item);
                }
            }
        }


        public static House GetHouse(Serial serial)
        {
            _houses.TryGetValue(serial, out House h);
            return h;
        }

        public static House GetOrCreateHouse(Serial serial)
        {
            if (_houses.TryGetValue(serial, out House house))
            {
                return house;
            }

            return new House(serial);
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

        public static Entity Get(Serial serial)
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

            item.Dispose();
            return true;
        }

        public static bool RemoveMobile(Serial serial)
        {
            Mobile mobile = Mobiles.Get(serial);
            if (mobile == null)
            {
                return false;
            }

            foreach (Item i in mobile.Items)
            {
                RemoveItem(i);
            }

       
            mobile.Dispose();
            return true;
        }

        public static void Clear(bool noplayer = false)
        {
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
    //public static class World
    //{
    //    private static readonly ConcurrentDictionary<Serial, House> _houses = new ConcurrentDictionary<Serial, House>();
    //    //public static HashSet<Item> ToAdd { get; } = new HashSet<Item>();
    //    //public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();
    //    //public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();
    //    public static PlayerMobile Player { get; set; }
    //    public static Facet Map { get; private set; }
    //    public static byte ViewRange { get; set; } = 24;

    //    public static int MapIndex
    //    {
    //        get => Map?.Index ?? -1;
    //        set
    //        {
    //            if (MapIndex != value)
    //            {
    //                //Clear(true);

    //                Clear();

    //                if (Map != null)
    //                {
    //                    Position position = Player.Position;
    //                    Player.Map = null;
    //                    Map = null;
    //                    Map = new Facet(value);
    //                    Player.Map = Map;
    //                    Player.Position = position;
    //                    Player.ClearSteps();
    //                    Player.ProcessDelta();
    //                }
    //                else
    //                {
    //                    Map = new Facet(value);
    //                    if (Player != null)
    //                    {
    //                        Player.Map = Map;
    //                        Map.Center = new Point(Player.Position.X, Player.Position.Y);
    //                    }
    //                }
    //            }
    //        }
    //    }


    //    public static bool InGame => Player != null && Map != null;
    //    public static long Ticks { get; set; }
    //    public static IsometricLight Light { get; } = new IsometricLight();


    //    private static readonly Dictionary<Serial, Entity> _objects = new Dictionary<Serial, Entity>();
    //    private static readonly Queue<Entity> _queuedObjects = new Queue<Entity>();
    //    private static readonly Queue<Serial> _toRemove = new Queue<Serial>();
    //    private static readonly List<Serial> _retained = new List<Serial>();
    //    private static readonly Func<Serial, Mobile> _funcMobile = (s) => new Mobile(s);
    //    private static readonly Func<Serial, PlayerMobile> _funcPlayerMobile = (s) => new PlayerMobile(s);
    //    private static readonly Func<Serial, Item> _funcItem = (s) => new Item(s);
    //    private static bool _isUpdating;

    //    public static void Clear(bool clearPlayer = false)
    //    {
    //        _retained.Clear();
    //        if (!clearPlayer)
    //        {
    //            if (Player != null)
    //                Retain(Player, _retained);
    //        }

    //        foreach (var e in _objects.Values)
    //        {
    //            if (!_retained.Contains(e.Serial))
    //                e.Dispose();
    //        }
    //    }

    //    private static void Retain(Mobile mob, List<Serial> retained)
    //    {
    //        retained.Add(mob);

    //        for (int i = (int)Layer.RightHand; i <= (int)Layer.Bank; i++)
    //        {
    //            var e = mob.Equipment[i];
    //            if (e != null && !e.IsDisposed)
    //            {
    //                retained.Add(e);

    //                if (e.Items.Count > 0)
    //                    RecursiveRetain(e, retained);
    //            }
    //        }
    //    }

    //    private static void RecursiveRetain(Item container, List<Serial> retained)
    //    {
    //        foreach (var e in container.Items)
    //        {
    //            if (e.Value != null && !e.Value.IsDisposed)
    //            {
    //                retained.Add(e.Key);
    //                if (e.Value.Items.Count > 0)
    //                    RecursiveRetain(e.Value, retained);
    //            }
    //        }
    //    }

    //    public static T GetOrCreate<T>(Serial serial, bool createPlayer = false) where T : Entity
    //    {
    //        if (!serial.IsValid)
    //            return default;

    //        var obj = Get(serial);
    //        if (obj == null)
    //        {
    //            if (createPlayer)
    //                obj = _funcPlayerMobile(serial);
    //            else
    //            {
    //                if (typeof(T) == typeof(Mobile))
    //                    obj = _funcMobile(serial);
    //                else if (typeof(T) == typeof(Item))
    //                    obj = _funcItem(serial);
    //                else
    //                    throw new Exception("What kind of serial is it?");
    //            }

    //            if (_isUpdating)
    //                _queuedObjects.Enqueue((T)obj);
    //            else
    //                _objects.Add(serial, (T)obj);
    //        }

    //        return (T)obj;
    //    }


    //    public static Entity Get(Serial serial)
    //    {
    //        if (_objects.TryGetValue(serial, out var entity) && entity.IsDisposed)
    //        {
    //            _objects.Remove(serial);
    //            return null;
    //        }
    //        return entity;
    //    }

    //    public static T Get<T>(Serial serial) where T : Entity
    //        => (T)Get(serial);


    //    public static bool Exists(Serial serial) => Get(serial) != null;

    //    public static void Remove(Serial serial)
    //        => Get(serial)?.Dispose();


    //    public static void Update(double totalMS, double frameMS)
    //    {
    //        if (Player == null)
    //            return;

    //        _isUpdating = true;

    //        foreach (var k in _objects)
    //        {
    //            k.Value.Update(frameMS);

    //            if (k.Value.Distance > ViewRange && ((k.Key.IsItem && ((Item)k.Value).OnGround) || k.Key.IsMobile))
    //                k.Value.Dispose();

    //            if (k.Value.IsDisposed)
    //                _toRemove.Enqueue(k.Key);
    //        }

    //        while (_toRemove.Count > 0)
    //            _objects.Remove(_toRemove.Dequeue());

    //        _isUpdating = false;

    //        while (_queuedObjects.Count > 0)
    //        {
    //            var e = _queuedObjects.Dequeue();
    //            _objects.Add(e.Serial, e);
    //        }
    //    }


    //    //public static void Update(double totalMS, double frameMS)
    //    //{
    //    //    //if (Player != null)
    //    //    //{
    //    //    //    foreach (var mob in Mobiles)
    //    //    //    {
    //    //    //        mob.Update(frameMS);

    //    //    //        if (mob.Distance > ViewRange)
    //    //    //        {
    //    //    //            mob.Dispose();
    //    //    //            RemoveMobile(mob);
    //    //    //        }

    //    //    //        if (mob.IsDisposed)
    //    //    //            Mobiles.Remove(mob);
    //    //    //    }

    //    //    //    foreach (var item in Items)
    //    //    //    {
    //    //    //        item.Update(frameMS);

    //    //    //        if (item.Distance > ViewRange && item.OnGround)
    //    //    //        {
    //    //    //            item.Dispose();
    //    //    //            RemoveItem(item);
    //    //    //        }

    //    //    //        if (item.IsDisposed)
    //    //    //            Items.Remove(item);
    //    //    //    }

    //    //    //}
    //    //}


    //    public static House GetHouse(Serial serial)
    //    {
    //        _houses.TryGetValue(serial, out House h);
    //        return h;
    //    }

    //    public static House GetOrCreateHouse(Serial serial)
    //    {
    //        if (_houses.TryGetValue(serial, out House house))
    //        {
    //            return house;
    //        }

    //        return new House(serial);
    //    }

    //    public static void AddOrUpdateHouse(House house)
    //    {
    //        _houses.TryAdd(house.Serial, house);
    //    }

    //    public static void RemoveHouse(Serial house)
    //    {
    //        _houses.TryRemove(house, out House h);
    //    }


    //    //public static bool Contains(Serial serial)
    //    //{
    //    //    if (serial.IsItem)
    //    //    {
    //    //        return Items.Contains(serial);
    //    //    }

    //    //    if (serial.IsMobile)
    //    //    {
    //    //        return Mobiles.Contains(serial);
    //    //    }

    //    //    return false;
    //    //}

    //    //public static Entity Get(Serial serial)
    //    //{
    //    //    if (serial.IsItem)
    //    //    {
    //    //        return Items.Get(serial);
    //    //    }

    //    //    if (serial.IsMobile)
    //    //    {
    //    //        return Mobiles.Get(serial);
    //    //    }

    //    //    return null;
    //    //}

    //    //public static Item GetOrCreateItem(Serial serial)
    //    //{
    //    //    return Items.Get(serial) ?? new Item(serial);
    //    //}

    //    //public static Mobile GetOrCreateMobile(Serial serial)
    //    //{
    //    //    return Mobiles.Get(serial) ?? new Mobile(serial);
    //    //}

    //    //public static bool RemoveItem(Serial serial)
    //    //{
    //    //    Item item = Items.Get(serial);
    //    //    if (item == null)
    //    //    {
    //    //        ToAdd.RemoveWhere(i => i == serial);
    //    //        return false;
    //    //    }

    //    //    if (item.Layer != Layer.Invalid && item.RootContainer.IsMobile)
    //    //    {
    //    //        var mobile = Mobiles.Get(item.RootContainer);
    //    //        if (mobile != null)
    //    //        {
    //    //            mobile.Equipment[(int)item.Layer] = null;
    //    //        }
    //    //    }

    //    //    item.Dispose();
    //    //    return true;
    //    //}

    //    //public static bool RemoveMobile(Serial serial)
    //    //{
    //    //    Mobile mobile = Mobiles.Get(serial);
    //    //    if (mobile == null)
    //    //        return false;

    //    //    mobile.Dispose();
    //    //    return true;
    //    //}

    //    //public static void Clear(bool noplayer = false)
    //    //{
    //    //    if (!noplayer)
    //    //    {
    //    //        Map = null;
    //    //        Player = null;
    //    //    }

    //    //    foreach (Item item in Items)
    //    //    {
    //    //        if (noplayer)
    //    //        {
    //    //            if (item.RootContainer == Player)
    //    //            {
    //    //                continue;
    //    //            }
    //    //        }

    //    //        RemoveItem(item);
    //    //    }

    //    //    foreach (Mobile mob in Mobiles)
    //    //    {
    //    //        if (noplayer)
    //    //        {
    //    //            if (mob == Player)
    //    //            {
    //    //                continue;
    //    //            }
    //    //        }

    //    //        RemoveMobile(mob);
    //    //    }
    //    //}
    //}
}