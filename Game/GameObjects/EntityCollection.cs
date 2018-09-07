#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
using ClassicUO.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.GameObjects
{
    //public class EntityCollection<T> : IEnumerable<T> where T : Entity
    //{
    //    private readonly List<T> _added = new List<T>(), _removed = new List<T>();
    //    private readonly ConcurrentDictionary<Serial, T> _entities = new ConcurrentDictionary<Serial, T>();

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return _entities.Values.GetEnumerator();//_entities.Select(e => e.Value).GetEnumerator();
    //    }

    //    public int Count => _entities.Count;


    //    public event EventHandler<CollectionChangedEventArgs<T>> Added, Removed;

    //    public void ProcessDelta()
    //    {
    //        if (_added.Count > 0)
    //        {
    //            CollectionChangedEventArgs<T> list = new CollectionChangedEventArgs<T>(_added);
    //            _added.Clear();
    //            Added.Raise(list);
    //        }

    //        if (_removed.Count > 0)
    //        {
    //            CollectionChangedEventArgs<T> list = new CollectionChangedEventArgs<T>(_removed);
    //            _removed.Clear();
    //            Removed.Raise(list);
    //        }
    //    }

    //    public bool Contains(Serial serial)
    //    {
    //        return _entities.ContainsKey(serial);
    //    }

    //    public T Get(Serial serial)
    //    {
    //        _entities.TryGetValue(serial, out T entity);
    //        return entity;
    //    }

    //    public bool Add(T entity)
    //    {
    //        if (!_entities.TryAdd(entity.Serial, entity))
    //        {
    //            return false;
    //        }

    //        _added.Add(entity);
    //        return true;
    //    }

    //    public T Remove(Serial serial)
    //    {
    //        if (_entities.TryRemove(serial, out T entity))
    //        {
    //            _removed.Add(entity);
    //        }

    //        return entity;
    //    }

    //    public void Clear()
    //    {
    //        _removed.AddRange(this);
    //        _entities.Clear();
    //        ProcessDelta();
    //    }
    //}


    //public class CollectionChangedEventArgs<T> : EventArgs, IEnumerable<T>
    //{
    //    private readonly IReadOnlyList<T> _data;

    //    public CollectionChangedEventArgs(IEnumerable<T> list)
    //    {
    //        _data = list.ToArray();
    //    }

    //    public int Count => _data.Count;

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return _data.GetEnumerator();
    //    }
    //}
}