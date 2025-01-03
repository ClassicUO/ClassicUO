// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Utility.Collections
{
    public class Bag<T> : IEnumerable<T>
    {
        private readonly bool _isPrimitive;
        private T[] _items;

        public Bag(int capacity = 16)
        {
            _isPrimitive = typeof(T).IsPrimitive;
            _items = new T[capacity];
        }

        public int Capacity => _items.Length;

        public bool IsEmpty => Count == 0;

        public int Count { get; private set; }

        public T this[int index]
        {
            get => _items[index];
            set
            {
                EnsureCapacity(index + 1);

                if (index >= Count)
                {
                    Count = index + 1;
                }

                _items[index] = value;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new BagEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BagEnumerator(this);
        }

        public void Add(T element)
        {
            EnsureCapacity(Count + 1);
            _items[Count] = element;
            ++Count;
        }

        public void AddRange(Bag<T> range)
        {
            for (int index = 0, j = range.Count; j > index; ++index)
            {
                Add(range[index]);
            }
        }

        public void Clear()
        {
            if (Count == 0)
            {
                return;
            }

            Count = 0;

            // non-primitive types are cleared so the garbage collector can release them
            if (!_isPrimitive)
            {
                Array.Clear(_items, 0, Count);
            }
        }

        public bool Contains(T element)
        {
            for (int index = Count - 1; index >= 0; --index)
            {
                if (element.Equals(_items[index]))
                {
                    return true;
                }
            }

            return false;
        }

        public T RemoveAt(int index)
        {
            T result = _items[index];
            --Count;
            _items[index] = _items[Count];
            _items[Count] = default;

            return result;
        }

        public bool Remove(T element)
        {
            for (int index = Count - 1; index >= 0; --index)
            {
                if (element.Equals(_items[index]))
                {
                    --Count;
                    _items[index] = _items[Count];
                    _items[Count] = default;

                    return true;
                }
            }

            return false;
        }

        public bool RemoveAll(Bag<T> bag)
        {
            bool isResult = false;

            for (int index = bag.Count - 1; index >= 0; --index)
            {
                if (Remove(bag[index]))
                {
                    isResult = true;
                }
            }

            return isResult;
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity < _items.Length)
            {
                return;
            }

            int newCapacity = Math.Max((int) (_items.Length * 1.5), capacity);
            T[] oldElements = _items;
            _items = new T[newCapacity];

            Array.Copy
            (
                oldElements,
                0,
                _items,
                0,
                oldElements.Length
            );
        }

        public struct BagEnumerator : IEnumerator<T>
        {
            private volatile Bag<T> _bag;
            private volatile int _index;

            public BagEnumerator(Bag<T> bag)
            {
                _bag = bag;
                _index = -1;
            }

            T IEnumerator<T>.Current => _bag[_index];

            object IEnumerator.Current => _bag[_index];

            public bool MoveNext()
            {
                return ++_index < _bag.Count;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
            }
        }
    }
}