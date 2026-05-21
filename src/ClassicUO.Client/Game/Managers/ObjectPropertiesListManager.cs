// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal sealed class ObjectPropertiesListManager
    {
        private readonly Dictionary<uint, ItemProperty> _itemsProperties = new Dictionary<uint, ItemProperty>();
        private readonly World _world;

        public ObjectPropertiesListManager(World world)
        {
            _world = world;
            EventSink.OplInfoReceived += OnOplInfoReceived;
            EventSink.MegaClilocReceived += OnMegaClilocReceived;
        }

        public void Unsubscribe()
        {
            EventSink.OplInfoReceived -= OnOplInfoReceived;
            EventSink.MegaClilocReceived -= OnMegaClilocReceived;
        }

        private void OnOplInfoReceived(OplInfoReceivedArgs e)
        {
            if (!_world.ClientFeatures.TooltipsEnabled)
            {
                return;
            }

            if (!IsRevisionEquals(e.Serial, e.Revision))
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

            Add(serial, e.Revision, e.Name, e.Properties, e.NameCliloc);

            if (inBuyList && container != null && SerialHelper.IsValid(container.Serial))
            {
                UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, e.Name);
            }
        }

        public void Add(uint serial, uint revision, string name, string data, int namecliloc)
        {
            if (!_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                prop = new ItemProperty();
                _itemsProperties[serial] = prop;
            }
            else
            {

            }

            prop.Serial = serial;
            prop.Revision = revision;
            prop.Name = name;
            prop.Data = data;
            prop.NameCliloc = namecliloc;
        }


        public bool Contains(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                return true; //p.Revision != 0;  <-- revision == 0 can contain the name.
            }

            // if we don't have the OPL of this item, let's request it to the server.
            // Original client seems asking for OPL when character is not running.
            // We'll ask OPL when mouse is over an object.
            PacketHandlers.AddMegaClilocRequest(serial);

            return false;
        }

        public bool IsRevisionEquals(uint serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                return (revision & ~0x40000000) == prop.Revision || // remove the mask
                       revision == prop.Revision;                   // if mask removing didn't work, try a simple compare.
            }

            return false;
        }

        public bool TryGetRevision(uint serial, out uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                revision = p.Revision;

                return true;
            }

            revision = 0;

            return false;
        }

        public bool TryGetNameAndData(uint serial, out string name, out string data)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                name = p.Name;
                data = p.Data;

                return true;
            }

            name = data = null;

            return false;
        }

        public int GetNameCliloc(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                return p.NameCliloc;
            }

            return 0;
        }

        public void Remove(uint serial)
        {
            _itemsProperties.Remove(serial);
        }

        public void Clear()
        {
            _itemsProperties.Clear();
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
