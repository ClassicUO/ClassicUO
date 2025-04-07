// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Services;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Sdk;

namespace ClassicUO.Game.Managers
{
    internal class WMapEntity
    {
        public WMapEntity(uint serial)
        {
            Serial = serial;

            //var mob = World.Mobiles.Get(serial);

            //if (mob != null)
            //    GetName();
        }

        public bool IsGuild;
        public uint LastUpdate;
        public string? Name;
        public readonly uint Serial;
        public int X, Y, HP, Map;

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

    internal sealed class WorldMapEntityManager
    {
        private bool _ackReceived;
        private uint _lastUpdate, _lastPacketSend, _lastPacketRecv;
        private readonly List<WMapEntity> _toRemove = new List<WMapEntity>();
        private readonly WorldService _worldService = ServiceProvider.Get<WorldService>();
        private readonly NetClientService _netClientService = ServiceProvider.Get<NetClientService>();
        private readonly PacketHandlerService _packetHandlerService = ServiceProvider.Get<PacketHandlerService>();


        public bool Enabled
        {
            get
            {
                return ((_worldService.World.ClientFeatures.Flags & CharacterListFlags.CLF_NEW_MOVEMENT_SYSTEM) == 0 || _ackReceived) &&
                        (_netClientService.Socket.Encryption == null || _netClientService.Socket.Encryption.EncryptionType == 0) &&
                        ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.WorldMapShowParty &&
                        ServiceProvider.Get<GuiService>().GetGump<WorldMapGump>() != null; // horrible, but works
            }
        }

        public Dictionary<uint, WMapEntity> Entities { get; } = new ();

        public void SetACKReceived()
        {
            _ackReceived = true;
        }

        public void SetEnable(bool v)
        {
            if ((_worldService.World.ClientFeatures.Flags & CharacterListFlags.CLF_NEW_MOVEMENT_SYSTEM) != 0 && !_ackReceived)
            {
                Log.Warn("Server support new movement system. Can't use the 0xF0 packet to query guild/party position");
                v = false;
            }
            else if (_netClientService.Socket.Encryption != null && _netClientService.Socket.Encryption.EncryptionType != 0 && !_ackReceived)
            {
                Log.Warn("Server has encryption. Can't use the 0xF0 packet to query guild/party position");
                v = false;
            }

            if (v)
            {
                RequestServerPartyGuildInfo(true);
            }
        }

        public void AddOrUpdate
        (
            uint serial,
            int x,
            int y,
            int hp,
            int map,
            bool isguild,
            string? name = null,
            bool from_packet = false
        )
        {
            if (from_packet)
            {
                _lastPacketRecv = Time.Ticks + 10000;
            }
            else if (_lastPacketRecv < Time.Ticks)
            {
                return;
            }

            if (!Enabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                var ent = _worldService.World.Get(serial);

                if (ent != null && !string.IsNullOrEmpty(ent.Name))
                {
                    name = ent.Name;
                }
            }

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

                if (string.IsNullOrEmpty(entity.Name) && !string.IsNullOrEmpty(name))
                {
                    entity.Name = name;
                }
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
            {
                return;
            }

            _lastUpdate = Time.Ticks + 1000;

            long ticks = Time.Ticks - 1000;

            foreach (WMapEntity entity in Entities.Values)
            {
                if (entity.LastUpdate < ticks)
                {
                    _toRemove.Add(entity);
                }
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

        public WMapEntity? GetEntity(uint serial)
        {
            Entities.TryGetValue(serial, out var entity);

            return entity;
        }

        public void RequestServerPartyGuildInfo(bool force = false)
        {
            if (!force && !Enabled)
            {
                return;
            }

            if (_worldService.World.InGame && _lastPacketSend < Time.Ticks)
            {
                //GameActions.Print($"SENDING PACKET! {Time.Ticks}");

                _lastPacketSend = Time.Ticks + 250;

                //if (!force && !_can_send)
                //{
                //    return;
                //}

                _packetHandlerService.Out.Send_QueryGuildPosition();

                if (ServiceProvider.Get<ManagersService>().Party != null && ServiceProvider.Get<ManagersService>().Party.Leader != 0)
                {
                    foreach (var e in ServiceProvider.Get<ManagersService>().Party.Members)
                    {
                        if (e != null && SerialHelper.IsValid(e.Serial))
                        {
                            var mob = _worldService.World.Mobiles.Get(e.Serial);

                            if (mob == null || mob.Distance > _worldService.World.ClientViewRange)
                            {
                                _packetHandlerService.Out.Send_QueryPartyPosition();

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
            _ackReceived = false;
            SetEnable(false);
        }
    }
}