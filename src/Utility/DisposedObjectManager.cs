using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    static class DisposedObjectManager<T> where T : class
    {
        private static readonly Queue<T> _disposedList = new Queue<T>();
        private static long _lastTime;

        private const int TIME_TO_CLEAR = 1000;

        public static void Add(T obj)
        {
            _disposedList.Enqueue(obj);
        }

        public static void Update()
        {
            if (_disposedList.Count != 0 && _lastTime < Engine.Ticks)
            {
                int count = Math.Min(100, _disposedList.Count);

                while (count-- != 0)
                {
                    _disposedList.Dequeue();
                }
                
                _lastTime = Engine.Ticks + TIME_TO_CLEAR;
            }
        }
    }
}
