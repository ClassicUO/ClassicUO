// SPDX-License-Identifier: BSD-2-Clause

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