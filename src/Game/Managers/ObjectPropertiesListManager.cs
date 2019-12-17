using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    sealed class ObjectPropertiesListManager
    {
        private readonly Dictionary<uint, ItemProperty> _itemsProperties = new Dictionary<uint, ItemProperty>();


        public void Add(uint serial, uint revision, string name, string data)
            => _itemsProperties[serial] = new ItemProperty()
            {
                Serial = serial,
                Revision = revision,
                Name = name,
                Data = data
            };


        public bool Contains(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out var p))
            {
                return p.Revision != 0;
            }

            return false;
        }

        public bool IsRevisionEqual(uint serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out var prop))
            {
                return prop.Revision == revision;
            }

            return false;
        }

        public bool TryGetRevision(uint serial, out uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out var p))
            {
                revision = p.Revision;

                return true;
            }

            revision = 0;
            return false;
        }

        public bool TryGetNameAndData(uint serial, out string name, out string data)
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
        public uint Serial;
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
