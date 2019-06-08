using System;
using System.Collections.Generic;

using ClassicUO.Game.Map;
using ClassicUO.Interfaces;

namespace ClassicUO.Utility
{
    internal class QueuedPool<T> where T : class, new()
    {
        private readonly Queue<T> _pool;

        public QueuedPool(int size = 0)
        {
            _pool = new Queue<T>(size);

            for (int i = 0; i < size; i++)
                _pool.Enqueue(new T());
        }

        public T GetOne()
        {
            T result = null;
            result = _pool.Count > 0 ? _pool.Dequeue() : new T();

            //if (result is IPoolable poolable)
            //    poolable.OnPickup();

            return result;
        }

        public void ReturnOne(T obj)
        {
            if (obj != null)
                _pool.Enqueue(obj);
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}