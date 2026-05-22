// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.WorldMap
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
        public string Name;
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

    /// <summary>
    /// Facade over the two world-map collaborators: keeps the existing
    /// public surface (<c>_world.WMapManager.X</c>) and owns gating /
    /// network-request state. Per-serial storage lives in
    /// <see cref="IWorldMapEntityStore"/>; TTL pruning lives in
    /// <see cref="IWorldMapStalenessFilter"/>.
    /// </summary>
    internal sealed class WorldMapEntityManager
    {
        private bool _ackReceived;
        private uint _lastPacketSend, _lastPacketRecv;
        private readonly World _world;
        private readonly IWorldMapEntityStore _store;
        private readonly IWorldMapStalenessFilter _staleness;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public WorldMapEntityManager(World world)
            : this(world, new WorldMapEntityStore())
        {
        }

        /// <summary>Test / extension seam — inject a fake store. Filter is derived.</summary>
        internal WorldMapEntityManager(World world, IWorldMapEntityStore store)
            : this(world, store, new WorldMapStalenessFilter(store))
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal WorldMapEntityManager(World world, IWorldMapEntityStore store, IWorldMapStalenessFilter staleness)
        {
            _world = world;
            _store = store;
            _staleness = staleness;
        }

        public bool Enabled
        {
            get
            {
                return ((_world.ClientFeatures.Flags & CharacterListFlags.CLF_NEW_MOVEMENT_SYSTEM) == 0 || _ackReceived) &&
                        (NetClient.Socket.Encryption == null || NetClient.Socket.Encryption.EncryptionType == 0) &&
                        ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.WorldMapShowParty &&
                        UIManager.GetGump<WorldMapGump>() != null; // horrible, but works
            }
        }

        /// <summary>
        /// Legacy public surface: callers (e.g. <c>WorldMapGump</c>) iterate
        /// <c>WMapManager.Entities.Values</c> directly. Backed by the store.
        /// </summary>
        public Dictionary<uint, WMapEntity> Entities => _store.Entities;

        public void SetACKReceived()
        {
            _ackReceived = true;
        }

        public void SetEnable(bool v)
        {
            if ((_world.ClientFeatures.Flags & CharacterListFlags.CLF_NEW_MOVEMENT_SYSTEM) != 0 && !_ackReceived)
            {
                Log.Warn("Server support new movement system. Can't use the 0xF0 packet to query guild/party position");
                v = false;
            }
            else if (NetClient.Socket.Encryption?.EncryptionType != 0 && !_ackReceived)
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
            string name = null,
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
                Entity ent = _world.Get(serial);

                if (ent != null && !string.IsNullOrEmpty(ent.Name))
                {
                    name = ent.Name;
                }
            }

            _store.AddOrUpdate(serial, x, y, hp, map, isguild, name);
        }

        public void Remove(uint serial) => _store.Remove(serial);

        public void RemoveUnupdatedWEntity() => _staleness.Prune();

        public WMapEntity GetEntity(uint serial)
        {
            _store.TryGet(serial, out WMapEntity entity);
            return entity;
        }

        public void RequestServerPartyGuildInfo(bool force = false)
        {
            if (!force && !Enabled)
            {
                return;
            }

            if (_world.InGame && _lastPacketSend < Time.Ticks)
            {
                //GameActions.Print($"SENDING PACKET! {Time.Ticks}");

                _lastPacketSend = Time.Ticks + 250;

                //if (!force && !_can_send)
                //{
                //    return;
                //}

                NetClient.Socket.Send_QueryGuildPosition();

                if (_world.Party != null && _world.Party.Leader != 0)
                {
                    foreach (PartyMember e in _world.Party.Members)
                    {
                        if (e != null && SerialHelper.IsValid(e.Serial))
                        {
                            Mobile mob = _world.Mobiles.Get(e.Serial);

                            if (mob == null || mob.Distance > _world.ClientViewRange)
                            {
                                NetClient.Socket.Send_QueryPartyPosition();

                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            _store.Clear();
            _ackReceived = false;
            SetEnable(false);
        }
    }
}
