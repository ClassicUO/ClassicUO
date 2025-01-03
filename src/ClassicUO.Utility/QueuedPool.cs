#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;

namespace ClassicUO.Utility
{
    public class QueuedPool<T> where T : class, new()
    {
        private readonly Action<T> _on_pickup;
        private readonly Stack<T> _pool;


        public QueuedPool(int size, Action<T> onpickup = null)
        {
            MaxSize = size;
            _pool = new Stack<T>(size);
            _on_pickup = onpickup;

            for (int i = 0; i < size; i++)
            {
                _pool.Push(new T());
            }
        }


        public int MaxSize { get; }

        public int Remains => MaxSize - _pool.Count;

        public T GetOne()
        {
            T result;

            if (_pool.Count != 0)
            {
                result = _pool.Pop();
            }
            else
            {
                result = new T();
            }

            _on_pickup?.Invoke(result);

            return result;
        }

        public void ReturnOne(T obj)
        {
            if (obj != null)
            {
                _pool.Push(obj);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}