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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class World
    {
        private static readonly EffectManager _effectManager = new EffectManager();


        public static PartyManager Party { get; } = new PartyManager();

        public static HouseManager HouseManager { get; } = new HouseManager();

        public static HashSet<Item> ToAdd { get; } = new HashSet<Item>();

        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();

        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();

        public static PlayerMobile Player { get; set; }

        public static Map.Map Map { get; private set; }

        public static byte ViewRange { get; set; } = 24;

        public static Serial LastAttack { get; set; }

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
                        {
                            Map.Dispose();                       
                        }

                        Position position = Player.Position;
                        Map = null;

                        Map = new Map.Map(value)
                        {
                            Center = new Point(position.X, position.Y)
                        };
                        Map.Initialize();

                        Player.Position = position;
                        Player.AddToTile();

                        Player.ClearSteps();
                        Player.ProcessDelta();                  
                    }
                    else
                    {
                        Map = new Map.Map(value);                       
                        if (Player != null)
                            Map.Center = new Point(Player.X, Player.Y);
                        Map.Initialize();
                    }
                }
            }
        }

        public static bool InGame => Player != null && Map != null;

        public static IsometricLight Light { get; } = new IsometricLight
        {
            Overall = 0, Personal = 0
        };

        public static LockedFeatures ClientLockedFeatures { get; } = new LockedFeatures();

        public static ClientFeatures ClientFlags { get; } = new ClientFeatures();

        public static string ServerName { get; set; }

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
                        if (item.IsMulti)
                        {
                            if (HouseManager.TryToRemove(item, ViewRange))
                                RemoveItem(item);
                        }
                        else
                            RemoveItem(item);
                    }

                    if (item.IsDisposed)
                        Items.Remove(item);
                }


                _effectManager.Update(totalMS, frameMS);
            }
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
                GameActions.RequestMobileStatus(mob);
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

        internal static void AddEffect(GameEffect effect)
        {
            _effectManager.Add(effect);
        }

        public static void AddEffect(GraphicEffectType type, Serial source, Serial target, Graphic graphic, Hue hue, Position srcPos, Position targPos, byte speed, int duration, bool fixedDir, bool doesExplode, bool hasparticles, GraphicEffectBlendMode blendmode)
        {
            _effectManager.Add(type, source, target, graphic, hue, srcPos, targPos, speed, duration, fixedDir, doesExplode, hasparticles, blendmode);
        }

        public static void Clear()
        {
            HouseManager.Clear();
            Items.Clear();
            Mobiles.Clear();
            Player.Dispose();
            Player = null;
            Map.Dispose();
            Map = null;
            ToAdd.Clear();
            IO.UltimaLive.IsUltimaLiveActive = false;
            IO.UltimaLive.ShardName = null;
            ClientFlags.SetFlags(0);
            ClientLockedFeatures.SetFlags(0);
            HouseManager.Clear();
            Party.Members.Clear();
            ServerName = string.Empty;
            LastAttack = 0;        
        }

        private static void InternalMapChangeClear(bool noplayer)
        {
            if (!noplayer)
            {
                Map.Dispose();
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