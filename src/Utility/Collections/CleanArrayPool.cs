using System.Buffers;

namespace ClassicUO.Utility.Collections
{
    public class CleanArrayPool<T>
    {
        private static CleanArrayPool<T> _sharedArrayPool = new CleanArrayPool<T>();
        public static CleanArrayPool<T> Shared => _sharedArrayPool;

        private ArrayPool<T> _pool = ArrayPool<T>.Create(0x100000, 8);

        public T[] Rent(int minimumLength) => _pool.Rent(minimumLength);

        public void Return(T[] array)
        {
            _pool.Return(array, true);
        }
    }
}
