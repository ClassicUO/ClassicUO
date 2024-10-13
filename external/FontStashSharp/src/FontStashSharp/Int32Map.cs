using System;
using System.Collections;
using System.Collections.Generic;

namespace FontStashSharp
{
	class ThrowHelper
	{
		public static void KeyNotFoundException()
		{
			throw new KeyNotFoundException();
		}

		public static void ArgumentOutOfRangeException()
		{
			throw new ArgumentOutOfRangeException();
		}

		public static void InvalidOperationException()
		{
			throw new InvalidOperationException();
		}

		public static void ArgumentException()
		{
			throw new ArgumentException();
		}
	}

	class SizingHelper
	{
		static readonly int[] Primes = {
			3, 5, 7, 11, 17, 23, 37, 53, 79, 113, 163, 229, 331, 463, 653, 919, 1289, 1811, 2539, 3557, 4987, 6983, 9781, 13693, 19181, 26861, 37607, 52667, 73751, 103289,
			144611, 202471, 283463, 396871, 555637, 777901, 1089091, 1524763, 2134697, 2988607, 4184087, 5857727, 8200847, 11481199, 16073693, 22503181, 31504453, 44106241,
			61748749, 86448259, 121027583, 169438627, 237214097, 332099741, 464939639, 650915521, 911281733, 1275794449, 1786112231
		};

		public static int GetSizingPrime(int min)
		{
			for (var index = 0; index < Primes.Length; ++index)
			{
				var num = Primes[index];
				if (num >= min)
					return num;
			}
			throw new Exception("Trying to find a too large prime.");
		}

		public static int NextSizingPrime(int min)
		{
			for (var index = 0; index < Primes.Length; ++index)
			{
				var num = Primes[index];
				if (num > min)
					return num;
			}
			throw new Exception("Trying to find a too large prime.");
		}
	}

	public class Int32Map<TValue> : IEnumerable<KeyValuePair<int, TValue>>
	{
		int[] _buckets;
		Entry[] _entries;
		int _count;
		int _version;
		int _freeList;
		int _freeCount;

		public Int32Map() : this(0) { }

		public Int32Map(int capacity)
		{
			if (capacity < 0)
				ThrowHelper.ArgumentOutOfRangeException();
			if (capacity > 0)
				Initialize(capacity);
		}

		public int Count
		{
			get { return _count - _freeCount; }
		}

		public TValue this[int key]
		{
			get
			{
				unchecked
				{
					if (_buckets != null)
					{
						var num = key & int.MaxValue;
						for (var index = _buckets[num % _buckets.Length]; index >= 0; index = _entries[index].Next)
							if (_entries[index].Key == key)
								return _entries[index].Value;
					}
					ThrowHelper.KeyNotFoundException();
					return default(TValue);
				}
			}
			set { Insert(key, value, false); }
		}

		IEnumerator<KeyValuePair<int, TValue>> IEnumerable<KeyValuePair<int, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public void Add(int key, TValue value)
		{
			Insert(key, value, true);
		}

		public void Clear()
		{
			if (_count <= 0)
				return;
			for (var index = 0; index < _buckets.Length; ++index)
				_buckets[index] = -1;
			Array.Clear(_entries, 0, _count);
			_freeList = -1;
			_count = 0;
			_freeCount = 0;
			_version = _version + 1;
		}

		public bool ContainsKey(int key)
		{
			return FindEntry(key) >= 0;
		}

		int FindEntry(int key)
		{
			unchecked
			{
				if (_buckets != null)
				{
					var num = key & int.MaxValue;
					for (var index = _buckets[num % _buckets.Length]; index >= 0; index = _entries[index].Next)
						if (_entries[index].Key == key)
							return index;
				}
				return -1;
			}
		}

		void Initialize(int capacity)
		{
			var prime = SizingHelper.GetSizingPrime(capacity);
			_buckets = new int[prime];
			for (var index = 0; index < _buckets.Length; ++index)
				_buckets[index] = -1;
			_entries = new Entry[prime];
			_freeList = -1;
		}

		void Insert(int key, TValue value, bool add)
		{
			unchecked
			{
				if (_buckets == null)
					Initialize(0);
				var num1 = key & int.MaxValue;
				var index1 = num1 % _buckets.Length;
				var num2 = 0;
				for (var index2 = _buckets[index1]; index2 >= 0; index2 = _entries[index2].Next)
				{
					if (_entries[index2].Key == key)
					{
						if (add)
							ThrowHelper.ArgumentException();
						_entries[index2].Value = value;
						_version = _version + 1;
						return;
					}
					++num2;
				}
				int index3;
				if (_freeCount > 0)
				{
					index3 = _freeList;
					_freeList = _entries[index3].Next;
					_freeCount = _freeCount - 1;
				}
				else
				{
					if (_count == _entries.Length)
					{
						Resize();
						index1 = num1 % _buckets.Length;
					}
					index3 = _count;
					_count = _count + 1;
				}
				_entries[index3].HashCode = num1;
				_entries[index3].Next = _buckets[index1];
				_entries[index3].Key = key;
				_entries[index3].Value = value;
				_buckets[index1] = index3;
				_version = _version + 1;
				if (num2 <= 100)
					return;
				Resize(_entries.Length + _entries.Length / 4 + 1);
			}
		}

		void Resize()
		{
			Resize(SizingHelper.NextSizingPrime(_count));
		}

		void Resize(int newSize)
		{
			var numArray = new int[newSize];
			for (var index = 0; index < numArray.Length; ++index)
				numArray[index] = -1;
			var entryArray = new Entry[newSize];
			Array.Copy(_entries, 0, entryArray, 0, _count);
			for (var index1 = 0; index1 < _count; ++index1)
				if (entryArray[index1].HashCode >= 0)
				{
					var index2 = entryArray[index1].HashCode % newSize;
					entryArray[index1].Next = numArray[index2];
					numArray[index2] = index1;
				}
			_buckets = numArray;
			_entries = entryArray;
		}

		public bool Remove(int key)
		{
			unchecked
			{
				if (_buckets != null)
				{
					var num = key & int.MaxValue;
					var index1 = num % _buckets.Length;
					var index2 = -1;
					for (var index3 = _buckets[index1]; index3 >= 0; index3 = _entries[index3].Next)
					{
						if (_entries[index3].Key == key)
						{
							if (index2 < 0)
								_buckets[index1] = _entries[index3].Next;
							else
								_entries[index2].Next = _entries[index3].Next;
							_entries[index3].HashCode = -1;
							_entries[index3].Next = _freeList;
							_entries[index3].Key = default(int);
							_entries[index3].Value = default(TValue);
							_freeList = index3;
							_freeCount = _freeCount + 1;
							_version = _version + 1;
							return true;
						}
						index2 = index3;
					}
				}
				return false;
			}
		}

		public bool TryGetValue(int key, out TValue value)
		{
			unchecked
			{
				if (_buckets != null)
				{
					var num = key & int.MaxValue;
					for (var index = _buckets[num % _buckets.Length]; index >= 0; index = _entries[index].Next)
						if (_entries[index].Key == key)
						{
							value = _entries[index].Value;
							return true;
						}
				}
				value = default(TValue);
				return false;
			}
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		struct Entry
		{
			public int HashCode;
			public int Next;
			public int Key;
			public TValue Value;
		}

		public struct Enumerator : IEnumerator<KeyValuePair<int, TValue>>
		{
			readonly Int32Map<TValue> _parent;
			readonly int _version;
			int _index;
			KeyValuePair<int, TValue> _current;

			public void Reset()
			{
				throw new NotImplementedException();
			}

			object IEnumerator.Current
			{
				get { return _current; }
			}

			public KeyValuePair<int, TValue> Current
			{
				get { return _current; }
			}

			internal Enumerator(Int32Map<TValue> parent)
			{
				_parent = parent;
				_version = parent._version;
				_index = 0;
				_current = new KeyValuePair<int, TValue>();
			}

			public bool MoveNext()
			{
				if (_version != _parent._version)
					ThrowHelper.InvalidOperationException();
				for (; (uint)_index < (uint)_parent._count; _index = _index + 1)
					if (_parent._entries[_index].HashCode >= 0)
					{
						_current = new KeyValuePair<int, TValue>(_parent._entries[_index].Key, _parent._entries[_index].Value);
						_index = _index + 1;
						return true;
					}
				_index = _parent._count + 1;
				_current = new KeyValuePair<int, TValue>();
				return false;
			}

			public void Dispose() { }
		}
	}
}
