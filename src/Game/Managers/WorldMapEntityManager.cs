#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Network;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    class WMapEntity
    {
        public WMapEntity(uint serial)
        {
            Serial = serial;

            //var mob = World.Mobiles.Get(serial);

            //if (mob != null)
            //    GetName();
        } 

        public readonly uint Serial;
        public int X, Y, HP, Map;
        public uint LastUpdate;
        public bool IsGuild;
        public string Name;

        //public string GetName()
        //{
        //    Entity e = World.Get(Serial);

        //    if (e != null && !e.IsDestroyed && !string.IsNullOrEmpty(e.Name) && Name != e.Name)
        //    {
        //        Name = e.Name;
        //    }

        //    return string.IsNullOrEmpty(Name) ? "<out of range>" : Name;
        //}
    }

    class WorldMapEntityManager
    {
        public readonly Dictionary<uint, WMapEntity> Entities = new Dictionary<uint, WMapEntity>();

        private readonly List<WMapEntity> _toRemove = new List<WMapEntity>();

        private uint _lastUpdate, _lastPacketSend, _lastPacketRecv;
        private bool _ack_received;

        /// <summary>
        /// If WorldMapGump is not visible, disable it
        /// </summary>
        public bool Enabled { get; private set; }

        public void SetACKReceived()
            => _ack_received = true;

        public void SetEnable(bool v)
        {
            if ((World.ClientFeatures.Flags & CharacterListFlags.CLF_NEW_MOVEMENT_SYSTEM) != 0 && !_ack_received)
            {
                Log.Warn("Server support new movement system. Can't use the 0xF0 packet to query guild/party position");
                v = false;
            }
            else if (EncryptionHelper.Type != 0 && !_ack_received)
            {
                Log.Warn("Server has encryption. Can't use the 0xF0 packet to query guild/party position");
                v = false;
            }

            Enabled = v;

            if (v)
                RequestServerPartyGuildInfo(true);
        }
 
        public void AddOrUpdate(uint serial, int x, int y, int hp, int map, bool isguild, string name = null, bool from_packet = false)
        {
            if (from_packet)
            {
                _can_send = true;
                _lastPacketRecv = Time.Ticks + 10000;
            }
            else if (_lastPacketRecv < Time.Ticks)
            {
                return;
            }

            if (!Enabled)
                return;
        
            if (!Entities.TryGetValue(serial, out var entity) || entity == null)
            {
                entity = new WMapEntity(serial)
                {
                    X = x, Y = y, HP = hp, Map = map,
                    LastUpdate = Time.Ticks + 1000,
                    IsGuild = isguild,
                    Name = name
                };

                Entities[serial] = entity;
            }
            else
            {
                entity.X = x;
                entity.Y = y;
                entity.HP = hp;
                entity.Map = map;
                entity.IsGuild = isguild;
                entity.LastUpdate = Time.Ticks + 1000;

                if (name != null)
                    entity.Name = name;
            }
        }

        public void Remove(uint serial)
        {
            if (Entities.ContainsKey(serial))
            {
                Entities.Remove(serial);
            }
        }

        public void RemoveUnupdatedWEntity()
        {
            if (_lastUpdate > Time.Ticks)
                return;

            _lastUpdate = Time.Ticks + 1000;

            long ticks = Time.Ticks - 1000;

            foreach (WMapEntity entity in Entities.Values)
            {
                if (entity.LastUpdate < ticks)
                    _toRemove.Add(entity);
            }

            if (_toRemove.Count != 0)
            {
                foreach (WMapEntity entity in _toRemove)
                {
                    Entities.Remove(entity.Serial);
                }

                _toRemove.Clear();
            }
        }

        public WMapEntity GetEntity(uint serial)
        {
            Entities.TryGetValue(serial, out var entity);

            return entity;
        }

        private bool _can_send;

        public void RequestServerPartyGuildInfo(bool force = false)
        {
            if (!force && !Enabled)
                return;

            if (World.InGame && _lastPacketSend < Time.Ticks)
            {
                _lastPacketSend = Time.Ticks + 250;

                //if (!force && !_can_send)
                //{
                //    return;
                //}

                NetClient.Socket.Send(new PQueryGuildPosition());

                if (World.Party != null && World.Party.Leader != 0)
                {
                    foreach (var e in World.Party.Members)
                    {
                        if (e != null && SerialHelper.IsValid(e.Serial))
                        {
                            var mob = World.Mobiles.Get(e.Serial);

                            if (mob == null || mob.Distance > World.ClientViewRange)
                            {
                                NetClient.Socket.Send(new PQueryPartyPosition());
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            Entities.Clear();
            _ack_received = false;
            SetEnable(false);
        }
    }
}
