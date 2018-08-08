using System;
using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Utility
{
    public class Bag<T> : IEnumerable<T>
    {
        private readonly bool _isPrimitive;
        private T[] _items;

        public Bag(in int capacity = 16)
        {
            _isPrimitive = typeof(T).IsPrimitive;
            _items = new T[capacity];
        }

        public int Capacity => _items.Length;
        public bool IsEmpty => Count == 0;
        public int Count { get; private set; }

        public T this[in int index]
        {
            get => _items[index];
            set
            {
                EnsureCapacity(index + 1);
                if (index >= Count)
                    Count = index + 1;
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

        public void Add(in T element)
        {
            EnsureCapacity(Count + 1);
            _items[Count] = element;
            ++Count;
        }

        public void AddRange(in Bag<T> range)
        {
            for (int index = 0, j = range.Count; j > index; ++index)
                Add(range[index]);
        }

        public void Clear()
        {
            if (Count == 0)
                return;

            Count = 0;

            // non-primitive types are cleared so the garbage collector can release them
            if (!_isPrimitive)
                Array.Clear(_items, 0, Count);
        }

        public bool Contains(in T element)
        {
            for (var index = Count - 1; index >= 0; --index)
            {
                if (element.Equals(_items[index]))
                    return true;
            }

            return false;
        }

        public T RemoveAt(in int index)
        {
            var result = _items[index];
            --Count;
            _items[index] = _items[Count];
            _items[Count] = default;
            return result;
        }

        public bool Remove(in T element)
        {
            for (var index = Count - 1; index >= 0; --index)
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

        public bool RemoveAll(in Bag<T> bag)
        {
            var isResult = false;

            for (var index = bag.Count - 1; index >= 0; --index)
            {
                if (Remove(bag[index]))
                    isResult = true;
            }

            return isResult;
        }

        private void EnsureCapacity(in int capacity)
        {
            if (capacity < _items.Length)
                return;

            var newCapacity = Math.Max((int) (_items.Length * 1.5), capacity);
            var oldElements = _items;
            _items = new T[newCapacity];
            Array.Copy(oldElements, 0, _items, 0, oldElements.Length);
        }

        internal struct BagEnumerator : IEnumerator<T>
        {
            private volatile Bag<T> _bag;
            private volatile int _index;

            public BagEnumerator(in Bag<T> bag)
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