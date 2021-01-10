#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

#define VALIDATE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClassicUO.Utility.Collections
{
    public class RawList<T> : IEnumerable<T>
    {
        public const uint DefaultCapacity = 4;
        private const float GrowthFactor = 2f;
        private uint _count;
        private T[] _items;

        public RawList() : this(DefaultCapacity)
        {
        }

        public RawList(uint capacity)
        {
#if VALIDATE
            if (capacity > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
#else
            Debug.Assert(capacity <= int.MaxValue);
#endif
            _items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        }

        public uint Count
        {
            get => _count;
            set => Resize(value);
        }


        public T[] Items => _items;

        public ArraySegment<T> ArraySegment => new ArraySegment<T>(_items, 0, (int) _count);

        public ref T this[uint index]
        {
            get
            {
                ValidateIndex(index);

                return ref _items[index];
            }
        }

        public ref T this[int index]
        {
            get
            {
                ValidateIndex(index);

                return ref _items[index];
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ref T item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, (int) (_items.Length * GrowthFactor));
            }

            _items[_count] = item;
            _count += 1;
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, (int) (_items.Length * GrowthFactor));
            }

            _items[_count] = item;
            _count += 1;
        }

        public void AddRange(T[] items)
        {
#if VALIDATE
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
#else
            Debug.Assert(items != null);
#endif

            int requiredSize = (int) (_count + items.Length);

            if (requiredSize > _items.Length)
            {
                Array.Resize(ref _items, (int) (requiredSize * GrowthFactor));
            }

            Array.Copy
            (
                items,
                0,
                _items,
                (int) _count,
                items.Length
            );

            _count += (uint) items.Length;
        }

        public void AddRange(IEnumerable<T> items)
        {
#if VALIDATE
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
#else
            Debug.Assert(items != null);
#endif

            foreach (T item in items)
            {
                Add(item);
            }
        }

        public void Replace(uint index, ref T item)
        {
            ValidateIndex(index);
            _items[index] = item;
        }

        public void Resize(uint count)
        {
            Array.Resize(ref _items, (int) count);
            _count = count;
        }

        public void Replace(uint index, T item)
        {
            Replace(index, ref item);
        }

        public bool Remove(ref T item)
        {
            bool contained = GetIndex(item, out uint index);

            if (contained)
            {
                CoreRemoveAt(index);
            }

            return contained;
        }


        public bool Remove(T item)
        {
            bool contained = GetIndex(item, out uint index);

            if (contained)
            {
                CoreRemoveAt(index);
            }

            return contained;
        }

        public void RemoveAt(uint index)
        {
            ValidateIndex(index);
            CoreRemoveAt(index);
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
        }

        public bool GetIndex(T item, out uint index)
        {
            int signedIndex = Array.IndexOf(_items, item);
            index = (uint) signedIndex;

            return signedIndex != -1;
        }

        public void Sort()
        {
            Sort(null);
        }

        public void Sort(IComparer<T> comparer)
        {
#if VALIDATE
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
#else
            Debug.Assert(comparer != null);
#endif
            Array.Sort(_items, 0, (int) Count, comparer);
        }

        public void TransformAll(Func<T, T> transformation)
        {
#if VALIDATE
            if (transformation == null)
            {
                throw new ArgumentNullException(nameof(transformation));
            }
#else
            Debug.Assert(transformation != null);
#endif

            for (int i = 0; i < _count; i++)
            {
                _items[i] = transformation(_items[i]);
            }
        }

        public ReadOnlyArrayView<T> GetReadOnlyView()
        {
            return new ReadOnlyArrayView<T>(_items, 0, _count);
        }

        public ReadOnlyArrayView<T> GetReadOnlyView(uint start, uint count)
        {
#if VALIDATE
            if (start + count >= _count)
            {
                throw new ArgumentOutOfRangeException();
            }
#else
            Debug.Assert(start + count < _count);
#endif
            return new ReadOnlyArrayView<T>(_items, start, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CoreRemoveAt(uint index)
        {
            _count -= 1;

            Array.Copy
            (
                _items,
                (int) index + 1,
                _items,
                (int) index,
                (int) (_count - index)
            );

            _items[_count] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(uint index)
        {
#if VALIDATE
            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
#else
            Debug.Assert(index < _count);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(int index)
        {
#if VALIDATE
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
#else
            Debug.Assert(index >= 0 && index < _count);
#endif
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly RawList<T> _list;
            private uint _currentIndex;

            public Enumerator(RawList<T> list)
            {
                _list = list;
                _currentIndex = uint.MaxValue;
            }

            public T Current => _list._items[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _currentIndex += 1;

                return _currentIndex < _list._count;
            }

            public void Reset()
            {
                _currentIndex = 0;
            }

            public void Dispose()
            {
            }
        }
    }
}