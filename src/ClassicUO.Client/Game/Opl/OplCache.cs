// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Opl
{
    /// <summary>
    /// Owns the in-memory dictionary of OPL entries keyed by entity serial.
    /// Single responsibility: cache add / lookup / remove. No events, no
    /// network, no UI side effects.
    /// </summary>
    internal sealed class OplCache : IOplCache
    {
        private readonly Dictionary<uint, ItemProperty> _itemsProperties = new();

        public void Set(uint serial, uint revision, string name, string data, int nameCliloc)
        {
            if (!_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                prop = new ItemProperty();
                _itemsProperties[serial] = prop;
            }

            prop.Serial = serial;
            prop.Revision = revision;
            prop.Name = name;
            prop.Data = data;
            prop.NameCliloc = nameCliloc;
        }

        public bool Contains(uint serial) => _itemsProperties.ContainsKey(serial);

        public bool IsRevisionEquals(uint serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                // remove the mask, then fall back to a direct compare.
                return (revision & ~0x40000000) == prop.Revision || revision == prop.Revision;
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

        public void Remove(uint serial) => _itemsProperties.Remove(serial);

        public void Clear() => _itemsProperties.Clear();
    }
}
