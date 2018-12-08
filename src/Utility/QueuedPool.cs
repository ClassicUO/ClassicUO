using ClassicUO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    internal class QueuedPool<T> where T : IPoolable, new()
    {
        private readonly Queue<T> _pool = new Queue<T>();

        public T GetOne()
        {
            T result = default;
            if (_pool.Count > 0)
                result = _pool.Dequeue();
            else
                result = new T();
            result?.OnPickup();
            return result;
        }

        public void ReturnOne(T obj)
        {
            obj?.OnReturn();
            if (obj != null)
                _pool.Enqueue(obj);
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}
