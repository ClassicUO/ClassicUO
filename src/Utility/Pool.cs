#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

namespace ClassicUO.Utility
{
    public class Pool<T> where T : class
    {
        private readonly Func<T> _createItem;
        private readonly Deque<T> _freeItems;
        private readonly int _maximum;
        private readonly Action<T> _resetItem;

        public Pool(Func<T> createItem, Action<T> resetItem, int capacity = 16, int maximum = int.MaxValue)
        {
            _createItem = createItem;
            _resetItem = resetItem;
            _maximum = maximum;
            _freeItems = new Deque<T>(capacity);
        }

        public Pool(Func<T> createItem, int capacity = 16, int maximum = int.MaxValue) : this(createItem, _ => { }, capacity, maximum)
        {
        }

        public int AvailableCount => _freeItems.Count;

        public T Obtain()
        {
            if (_freeItems.Count > 0)
                return _freeItems.Pop();

            return _createItem();
        }

        public void Free(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_freeItems.Count < _maximum)
                _freeItems.AddToBack(item);
            _resetItem(item);
        }

        public void Clear()
        {
            _freeItems.Clear();
        }
    }
}