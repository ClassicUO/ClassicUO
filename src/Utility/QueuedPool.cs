using ClassicUO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    internal class QueuedPool<T> where T : class, new()
    {
        private readonly Queue<T> _pool = new Queue<T>();

        public T GetOne()
        {
            T result = null;
            result = _pool.Count > 0 ? _pool.Dequeue() : new T();
            if (result is IPoolable poolable)
                poolable.OnPickup();
            return result;
        }

        public void ReturnOne(T obj)
        {
            if (obj is IPoolable poolable)
                poolable.OnReturn();
            if (obj != null)
                _pool.Enqueue(obj);
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}
