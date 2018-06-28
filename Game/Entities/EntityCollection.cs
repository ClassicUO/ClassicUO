using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Utility;

namespace ClassicUO.Game.Entities
{
    public class EntityCollection<T> : IEnumerable<T> where T : Entity
    {
        private readonly ConcurrentDictionary<Serial, T> _entities = new ConcurrentDictionary<Serial, T>();
        private readonly List<T> _added = new List<T>(), _removed = new List<T>();

        public event EventHandler<CollectionChangedEventArgs<T>> Added, Removed;
        public void ProcessDelta()
        {
            if (_added.Count > 0)
            {
                var list = new CollectionChangedEventArgs<T>(_added);
                _added.Clear();
                Added.Raise(list);
            }

            if (_removed.Count > 0)
            {
                var list = new CollectionChangedEventArgs<T>(_removed);
                _removed.Clear();
                Removed.Raise(list);
            }
        }

        public bool Contains(Serial serial) => _entities.ContainsKey(serial);

        public T Get(Serial serial)
        {
            _entities.TryGetValue(serial, out T entity);
            return entity;
        }

        public bool Add(T entity)
        {
            if (!_entities.TryAdd(entity.Serial, entity))
                return false;
            _added.Add(entity);
            return true;
        }

        public T Remove(Serial serial)
        {
            if (_entities.TryRemove(serial, out T entity))
                _removed.Add(entity);
            return entity;
        }

        public void Clear()
        {
            _removed.AddRange(this);
            _entities.Clear();
            ProcessDelta();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public IEnumerator<T> GetEnumerator() { return _entities.Select(e => e.Value).GetEnumerator(); }
    }


    public class CollectionChangedEventArgs<T> : EventArgs, IEnumerable<T>
    {
        private readonly IReadOnlyList<T> _data;
        public CollectionChangedEventArgs(IEnumerable<T> list) => _data = list.ToArray();
        public int Count => _data.Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
    }
}
