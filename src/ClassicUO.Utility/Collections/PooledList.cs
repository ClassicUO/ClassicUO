// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ClassicUO.Utility.Collections
{
    /// <summary>
    /// Method-scoped, ArrayPool-backed list. Use for temporary list-like buffers inside
    /// a method body where allocating a <see cref="System.Collections.Generic.List{T}"/>
    /// would churn the GC. The backing array is rented from <see cref="ArrayPool{T}"/> and
    /// returned on <see cref="Dispose"/>.
    ///
    /// Usage:
    /// <code>
    /// using var list = new PooledList&lt;Foo&gt;();
    /// list.Add(...);
    /// foreach (var x in list.AsSpan) { ... }
    /// </code>
    ///
    /// Always pair with <c>using var</c>. Forgetting to dispose leaks the rented array
    /// back to GC instead of returning it to the pool — not a correctness bug, just a
    /// missed pool reuse.
    /// </summary>
    public ref struct PooledList<T>
    {
        private T[] _array;
        private int _count;

        public PooledList(int initialCapacity)
        {
            _array = ArrayPool<T>.Shared.Rent(initialCapacity);
            _count = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public Span<T> AsSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.AsSpan(0, _count);
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        public void Add(T item)
        {
            if (_array == null || _count == _array.Length)
            {
                Grow();
            }

            _array[_count++] = item;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void RemoveAt(int index)
        {
            _count--;

            if (index < _count)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index);
            }

            _array[_count] = default;
        }

        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(
                    _array,
                    clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>()
                );
                _array = null;
            }

            _count = 0;
        }

        private void Grow()
        {
            int newCapacity = _array == null ? 4 : Math.Max(_array.Length * 2, 4);
            T[] newArray = ArrayPool<T>.Shared.Rent(newCapacity);

            if (_array != null)
            {
                Array.Copy(_array, newArray, _count);
                ArrayPool<T>.Shared.Return(
                    _array,
                    clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>()
                );
            }

            _array = newArray;
        }
    }
}
