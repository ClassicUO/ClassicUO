using System.Collections.Generic;

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Utility
{
    internal class QueuedPool<T> where T : class, IPoolObject, new()
    {
        private readonly Stack<T> _pool;

        private int _maxSize;

        public QueuedPool(int size = 0)
        {
            _maxSize = size;
            _pool = new Stack<T>(size);

            for (int i = 0; i < size; i++)
                _pool.Push(new T());
        }

        public T GetOne()
        {
            T result = _pool.Count > 0 ? _pool.Pop() : _maxSize == 0 ? new T() : null;

            if (result.InUse)
            {

            }

            result.InUse = true;
            //if (result is IPoolable poolable)
            //    poolable.OnPickup();

            return result;
        }

        public void ReturnOne(T obj)
        {
            if (obj != null)
            {
                if (_maxSize != 0 && _pool.Count + 1 > _maxSize)
                {

                }

                obj.InUse = false;
                _pool.Push(obj);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}