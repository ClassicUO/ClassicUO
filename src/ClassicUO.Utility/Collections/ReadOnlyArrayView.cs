#region license

// Copyright (c) 2024, andreakarasho
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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ReadOnlyArrayView<T> _view;
            private int _currentIndex;

            public Enumerator(ReadOnlyArrayView<T> view)
            {
                _view = view;
                _currentIndex = (int) view._start;
            }

            public T Current => _view._items[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _view._start + _view.Count - 1)
                {
                    _currentIndex += 1;

                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _currentIndex = (int) _view._start;
            }

            public void Dispose()
            {
            }
        }
    }
}