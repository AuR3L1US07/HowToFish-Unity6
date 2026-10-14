using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public class RingBuffer<T> : IEnumerable<T>, IEnumerable
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private RingBuffer<T> _enumeratedRingBuffer;

		private T[] _collection;

		private int _entriesEnumerated;

		private int _startIndex;

		private int _initializeCollectionCount;

		public T Current { get; private set; }

		private bool _enumerating => _enumeratedRingBuffer != null;

		object IEnumerator.Current => Current;

		public void Initialize(RingBuffer<T> c)
		{
			if (c.Count != 0)
			{
				_entriesEnumerated = 0;
				_startIndex = c.GetRealIndex(0);
				_enumeratedRingBuffer = c;
				_collection = c.Collection;
				_initializeCollectionCount = c.Count;
				Current = default(T);
			}
		}

		public bool MoveNext()
		{
			if (!_enumerating)
			{
				return false;
			}
			int count = _enumeratedRingBuffer.Count;
			if (count != _initializeCollectionCount)
			{
				Debug.LogError(_enumeratedRingBuffer.GetType().Name + " collection was modified during enumeration.");
				_entriesEnumerated = count;
			}
			if (_entriesEnumerated >= count)
			{
				Reset();
				return false;
			}
			int num = _startIndex + _entriesEnumerated;
			int capacity = _enumeratedRingBuffer.Capacity;
			if (num >= capacity)
			{
				num -= capacity;
			}
			Current = _collection[num];
			_entriesEnumerated++;
			return true;
		}

		public void Reset()
		{
			_enumeratedRingBuffer = null;
			_collection = null;
			Current = default(T);
		}

		public void Dispose()
		{
		}
	}

	public int Capacity;

	public T[] Collection = new T[0];

	private int _written;

	private Enumerator _enumerator;

	public const int DEFAULT_CAPACITY = 60;

	public int WriteIndex { get; private set; }

	public int Count => _written;

	public bool Initialized { get; private set; }

	public T this[int simulatedIndex]
	{
		get
		{
			int realIndex = GetRealIndex(simulatedIndex);
			if (realIndex >= 0)
			{
				return Collection[realIndex];
			}
			return default(T);
		}
		set
		{
			int realIndex = GetRealIndex(simulatedIndex);
			if (realIndex >= 0)
			{
				Collection[realIndex] = value;
			}
		}
	}

	public RingBuffer()
	{
		Initialize(60);
	}

	public RingBuffer(int capacity)
	{
		Initialize(capacity);
	}

	public void Initialize(int capacity)
	{
		if (capacity <= 0)
		{
			Debug.LogError("Collection length must be larger than 0.");
			return;
		}
		if (Collection == null)
		{
			GetNewCollection();
		}
		else if (Collection.Length < capacity)
		{
			Clear();
			ArrayPool<T>.Shared.Return(Collection);
			GetNewCollection();
		}
		else
		{
			Clear();
		}
		Capacity = capacity;
		Initialized = true;
		void GetNewCollection()
		{
			Collection = ArrayPool<T>.Shared.Rent(capacity);
		}
	}

	public void Initialize()
	{
		if (!Initialized)
		{
			Debug.Log($"RingBuffer for type {typeof(T).FullName} is being initialized with a default capacity of {60}.");
			Initialize(60);
		}
	}

	public void Clear()
	{
		for (int i = 0; i < Capacity; i++)
		{
			Collection[i] = default(T);
		}
		_written = 0;
		WriteIndex = 0;
		_enumerator.Reset();
	}

	[Obsolete("This method no longer functions. Use Clear() instead.")]
	public void Reset()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Insert(int simulatedIndex, T data)
	{
		Initialize();
		int written = _written;
		if (simulatedIndex == 0 && written == 0)
		{
			return Add(data);
		}
		int realIndex = GetRealIndex(simulatedIndex);
		if (realIndex == -1)
		{
			return default(T);
		}
		if (simulatedIndex == written - 1)
		{
			return Add(data);
		}
		for (int num = ((written == Capacity) ? (written - 1) : written); num > simulatedIndex; num--)
		{
			int realIndex2 = GetRealIndex(num, allowUnusedBuffer: true);
			int realIndex3 = GetRealIndex(num - 1);
			Collection[realIndex2] = Collection[realIndex3];
		}
		T result = Collection[realIndex];
		Collection[realIndex] = data;
		if (written < Capacity)
		{
			IncreaseWritten();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Add(T data)
	{
		Initialize();
		T result = Collection[WriteIndex];
		Collection[WriteIndex] = data;
		IncreaseWritten();
		return result;
	}

	public T Dequeue()
	{
		if (_written == 0)
		{
			return default(T);
		}
		int realIndex = GetRealIndex(0);
		T result = Collection[realIndex];
		RemoveRange(fromStart: true, 1);
		return result;
	}

	public bool TryDequeue(out T result)
	{
		if (_written == 0)
		{
			result = default(T);
			return false;
		}
		int realIndex = GetRealIndex(0);
		result = Collection[realIndex];
		RemoveRange(fromStart: true, 1);
		return true;
	}

	public T Enqueue(T data)
	{
		return Add(data);
	}

	private void IncreaseWritten()
	{
		int capacity = Capacity;
		WriteIndex++;
		_written++;
		if (WriteIndex >= capacity)
		{
			WriteIndex = 0;
		}
		if (_written > capacity)
		{
			_written = capacity;
		}
	}

	private int GetRealIndex(int simulatedIndex, bool allowUnusedBuffer = false)
	{
		if (simulatedIndex >= Capacity)
		{
			return ReturnError();
		}
		int written = _written;
		if (simulatedIndex >= written && !allowUnusedBuffer)
		{
			return ReturnError();
		}
		int num = Capacity - written + simulatedIndex + WriteIndex;
		if (num >= Capacity)
		{
			num -= Capacity;
		}
		return num;
		int ReturnError()
		{
			Debug.LogError($"Index {simulatedIndex} is out of range. Written count is {_written}, Capacity is {Capacity}");
			return -1;
		}
	}

	public void RemoveRange(bool fromStart, int length)
	{
		if (length == 0)
		{
			return;
		}
		if (length < 0)
		{
			Debug.LogError("Negative values cannot be removed.");
			return;
		}
		if (length >= _written)
		{
			Clear();
			return;
		}
		_written -= length;
		if (!fromStart)
		{
			WriteIndex -= length;
			if (WriteIndex < 0)
			{
				WriteIndex += Capacity;
			}
		}
	}

	public Enumerator GetEnumerator()
	{
		Initialize();
		_enumerator.Initialize(this);
		return _enumerator;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}

}