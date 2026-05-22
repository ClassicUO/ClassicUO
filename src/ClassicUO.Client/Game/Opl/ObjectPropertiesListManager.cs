// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Opl;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Opl
{
    /// <summary>
    /// Facade over the OPL cache collaborator: keeps the existing public
    /// surface (<c>_world.OPL.X</c>) and owns the EventSink subscriptions.
    /// Per-serial Name / Properties / Revision / NameCliloc storage lives
    /// in a dedicated cohesive class under <see cref="Game.Opl"/>.
    /// </summary>
    internal sealed class ObjectPropertiesListManager : IEventListener
    {
        private readonly IOplCache _cache;
        private readonly World _world;

        /// <summary>Production composition root. Defaults to the concrete cache.</summary>
        public ObjectPropertiesListManager(World world)
            : this(world, new OplCache())
        {
        }

        /// <summary>Test / extension seam — inject a fake cache.</summary>
        internal ObjectPropertiesListManager(World world, IOplCache cache)
        {
            _world = world;
            _cache = cache;
        }

        public void Subscribe()
        {
            EventSink.OplInfoReceived += OnOplInfoReceived;
            EventSink.MegaClilocReceived += OnMegaClilocReceived;
        }

        public void Unsubscribe()
        {
            EventSink.OplInfoReceived -= OnOplInfoReceived;
            EventSink.MegaClilocReceived -= OnMegaClilocReceived;
        }

        // ---- Cache facade ----
        public bool IsRevisionEquals(uint serial, uint revision) => _cache.IsRevisionEquals(serial, revision);
        public bool TryGetRevision(uint serial, out uint revision) => _cache.TryGetRevision(serial, out revision);
        public bool TryGetNameAndData(uint serial, out string name, out string data) => _cache.TryGetNameAndData(serial, out name, out data);
        public int GetNameCliloc(uint serial) => _cache.GetNameCliloc(serial);
        public void Add(uint serial, uint revision, string name, string data, int namecliloc) => _cache.Set(serial, revision, name, data, namecliloc);
        public void Remove(uint serial) => _cache.Remove(serial);
        public void Clear() => _cache.Clear();

        /// <summary>
        /// Cache miss requests an OPL fetch from the server. Original
        /// behaviour: callers (e.g. GameCursor hover) used <c>Contains</c> as
        /// both a presence check and an implicit request trigger.
        /// </summary>
        public bool Contains(uint serial)
        {
            if (_cache.Contains(serial))
            {
                return true; // revision == 0 can still contain the name.
            }

            // if we don't have the OPL of this item, let's request it to the server.
            // Original client seems asking for OPL when character is not running.
            // We'll ask OPL when mouse is over an object.
            PacketHandlers.AddMegaClilocRequest(serial);

            return false;
        }

        // ---- Event handlers ----
        private void OnOplInfoReceived(OplInfoReceivedArgs e)
        {
            if (!_world.ClientFeatures.TooltipsEnabled)
            {
                return;
            }

            if (!_cache.IsRevisionEquals(e.Serial, e.Revision))
            {
                PacketHandlers.AddMegaClilocRequest(e.Serial);
            }
        }

        private void OnMegaClilocReceived(MegaClilocReceivedArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            uint serial = e.Serial;

            Entity entity = _world.Mobiles.Get(serial);

            if (entity == null)
            {
                if (SerialHelper.IsMobile(serial))
                {
                    Log.Warn("Searching a mobile into World.Items from MegaCliloc packet");
                }

                entity = _world.Items.Get(serial);
            }

            // Original behaviour assigned entity.Name only when the cliloc list was non-empty;
            // we use a non-empty Name as the equivalent guard since name was initialized to "".
            if (entity != null && !SerialHelper.IsMobile(serial) && !string.IsNullOrEmpty(e.Name))
            {
                entity.Name = e.Name;
            }

            Item container = null;

            if (entity is Item it && SerialHelper.IsValid(it.Container))
            {
                container = _world.Items.Get(it.Container);
            }

            bool inBuyList = false;

            if (container != null)
            {
                inBuyList =
                    container.Layer == Layer.ShopBuy
                    || container.Layer == Layer.ShopBuyRestock
                    || container.Layer == Layer.ShopSell;
            }

            _cache.Set(serial, e.Revision, e.Name, e.Properties, e.NameCliloc);

            if (inBuyList && container != null && SerialHelper.IsValid(container.Serial))
            {
                UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, e.Name);
            }
        }
    }

    internal class ItemProperty
    {
        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Data);
        public string Data;
        public string Name;
        public uint Revision;
        public uint Serial;
        public int NameCliloc;

        public string CreateData(bool extended)
        {
            return string.Empty;
        }
    }
}
