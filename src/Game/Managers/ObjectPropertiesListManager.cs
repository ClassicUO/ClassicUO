using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    sealed class ObjectPropertiesListManager
    {
        private readonly Dictionary<Serial, ItemProperty> _itemsProperties = new Dictionary<Serial, ItemProperty>();


        public void Add(Serial serial, uint revision, string name, string data)
            => _itemsProperties[serial] = new ItemProperty()
            {
                Serial = serial,
                Revision = revision,
                Name = name,
                Data = data
            };


        public bool Contains(Serial serial)
        {
            if (_itemsProperties.TryGetValue(serial, out var p))
            {
                return p.Revision != 0;
            }

            return false;
        }

        public bool IsRevisionEqual(Serial serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out var prop))
            {
                return prop.Revision == revision;
            }

            return false;
        }

        public bool TryGetRevision(Serial serial, out uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out var p))
            {
                revision = p.Revision;

                return true;
            }

            revision = 0;
            return false;
        }

        public bool TryGetNameAndData(Serial serial, out string name, out string data)
        {
            if (_itemsProperties.TryGetValue(serial, out var p))
            {
                name = p.Name;
                data = p.Data;

                return true;
            }

            name = data = null;
            return false;
        }

        public void Clear()
        {
            _itemsProperties.Clear();
        }
    }

    class ItemProperty
    {
        public Serial Serial;
        public uint Revision;
        public string Name;
        public string Data;


        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Data);

        public string CreateData(bool extended)
        {
            return string.Empty;
        }
    }

}
