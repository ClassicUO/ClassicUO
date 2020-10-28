using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    internal class EntityCollection<T> : IEnumerable<T> where T : Entity
    {
        private readonly Dictionary<uint, T> _entities = new Dictionary<uint, T>();

        public int Count => _entities.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }


        public bool Contains(uint serial)
        {
            return _entities.ContainsKey(serial);
        }

        public T Get(uint serial)
        {
            _entities.TryGetValue(serial, out T entity);

            return entity;
        }

        public bool Add(T entity)
        {
            if (_entities.ContainsKey(entity.Serial))
            {
                return false;
            }

            _entities[entity.Serial] = entity;

            return true;
        }

        public void Remove(uint serial)
        {
            _entities.Remove(serial);
        }

        public void Replace(T entity, uint newSerial)
        {
            if (_entities.Remove(entity.Serial))
            {
                for (LinkedObject i = entity.Items; i != null; i = i.Next)
                {
                    Item it = (Item) i;
                    it.Container = newSerial;
                }

                _entities[newSerial] = entity;
                entity.Serial = newSerial;
            }
        }

        public void Clear()
        {
            _entities.Clear();
        }
    }
}