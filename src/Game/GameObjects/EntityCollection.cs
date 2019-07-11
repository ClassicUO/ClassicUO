#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal class EntityCollection<T> : IEnumerable<T> where T : Entity
    {
        private readonly List<Serial> _added = new List<Serial>(), _removed = new List<Serial>();
        private readonly Dictionary<Serial, T> _entities = new Dictionary<Serial, T>();

        public int Count => _entities.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        public event EventHandler<CollectionChangedEventArgs<Serial>> Added, Removed;

        public void ProcessDelta()
        {
            if (_added.Count != 0)
            {
                CollectionChangedEventArgs<Serial> list = new CollectionChangedEventArgs<Serial>(_added);
                _added.Clear();
                Added.Raise(list);
            }

            if (_removed.Count != 0)
            {
                CollectionChangedEventArgs<Serial> list = new CollectionChangedEventArgs<Serial>(_removed);
                _removed.Clear();
                Removed.Raise(list);
            }
        }

        public bool Contains(Serial serial)
        {
            return _entities.ContainsKey(serial);
        }

        public T Get(Serial serial)
        {
            _entities.TryGetValue(serial, out T entity);

            return entity;
        }

        public bool Add(T entity)
        {
            if (_entities.ContainsKey(entity.Serial))
                return false;

            _entities[entity.Serial] = entity;
            _added.Add(entity.Serial);

            return true;
        }

        public void Remove(Serial serial)
        {
            if (_entities.Remove(serial)) _removed.Add(serial);
        }

        public void Replace(T entity, Serial newSerial)
        {
            if (_entities.Remove(entity.Serial))
            {
                foreach (Item i in entity.Items)
                    i.Container = newSerial;

                _entities[newSerial] = entity;
                entity.Serial = newSerial;
            }
        }

        public void Clear()
        {
            _removed.AddRange(this.Select(s => s.Serial));
            _entities.Clear();
            ProcessDelta();
        }
    }

    internal class CollectionChangedEventArgs<T> : EventArgs, IEnumerable<T>
    {
        private readonly List<T> _data;

        public CollectionChangedEventArgs(IEnumerable<T> list)
        {
            _data = list.ToList();
        }

        public int Count => _data.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}