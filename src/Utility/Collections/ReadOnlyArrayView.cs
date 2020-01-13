#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Collections
{
    public readonly struct ReadOnlyArrayView<T> : IEnumerable<T>
    {
        private readonly T[] _items;
        private readonly uint _start;

        public readonly uint Count;

        public ReadOnlyArrayView(T[] items, uint start, uint count)
        {
#if VALIDATE
            if (items.Length < start + count)
            {
                throw new ArgumentException();
            }
#else
            Debug.Assert(items.Length >= start + count);
#endif
            _items = items;
            _start = start;
            Count = count;
        }

        public T this[uint index]
        {
            get
            {
#if VALIDATE
                if (index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index < Count);
#endif
                return _items[index + _start];
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private ReadOnlyArrayView<T> _view;
            private int _currentIndex;

            public Enumerator(ReadOnlyArrayView<T> view)
            {
                _view = view;
                _currentIndex = (int)view._start;
            }

            public T Current => _view._items[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != (_view._start + _view.Count) - 1)
                {
                    _currentIndex += 1;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _currentIndex = (int)_view._start;
            }

            public void Dispose() { }
        }
    }
}
