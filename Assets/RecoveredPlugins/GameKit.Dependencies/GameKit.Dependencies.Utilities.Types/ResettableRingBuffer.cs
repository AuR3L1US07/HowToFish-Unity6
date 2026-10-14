using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public class ResettableRingBuffer<T> : IResettable, IEnumerable<T>, IEnumerable where T : IResettable
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private ResettableRingBuffer<T> _enumeratedRingBuffer;

		private T[] _collection;

		private int _entriesEnumerated;

		private int _read;

		private int _startIndex;

		private int _initializeCollectionCount;

		public T Current { get; private set; }

		public int ActualIndex
		{
			get
			{
				int num = _startIndex + (_read - 1);
				int capacity = _enumeratedRingBuffer.Capacity;
				if (num >= capacity)
				{
					num -= capacity;
				}
				return num;
			}
		}

		public int SimulatedIndex => _read - 1;

		private bool _enumerating => _enumeratedRingBuffer != null;

		object IEnumerator.Current => Current;

		public void Initialize(ResettableRingBuffer<T> c)
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

	private bool _atCapacity => _written == Capacity;

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

	public ResettableRingBuffer()
	{
		Initialize(60);
	}

	public void Initialize(int capacity)
	{
		if (capacity <= 0)
		{
			Debug.LogError("Collection length must be larger than 0.");
			return;
		}
		if (Initialized)
		{
			ResetState();
		}
		if (Collection == null)
		{
			GetNewCollection();
		}
		else if (Collection.Length < capacity)
		{
			ArrayPool<T>.Shared.Return(Collection);
			GetNewCollection();
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
		if (Collection != null)
		{
			for (int i = 0; i < Capacity; i++)
			{
				if (i < _written)
				{
					Collection[i].ResetState();
				}
				Collection[i] = default(T);
			}
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
	public void Insert(int simulatedIndex, T data)
	{
		if (!IsInitializedWithError())
		{
			return;
		}
		int realIndex = GetRealIndex(simulatedIndex);
		if (realIndex == -1)
		{
			return;
		}
		int written = _written;
		if (simulatedIndex == written - 1)
		{
			Add(data);
			return;
		}
		bool atCapacity = _atCapacity;
		int num = ((written == Capacity) ? (written - 1) : written);
		if (atCapacity)
		{
			Collection[GetRealIndex(num)].ResetState();
		}
		while (num > simulatedIndex)
		{
			int realIndex2 = GetRealIndex(num, allowUnusedBuffer: true);
			int realIndex3 = GetRealIndex(num - 1);
			Collection[realIndex2] = Collection[realIndex3];
			num--;
		}
		Collection[realIndex] = data;
		if (!atCapacity)
		{
			IncreaseWritten();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Add(T data, bool resetState = true)
	{
		Initialize();
		T result = Collection[WriteIndex];
		if (_atCapacity && resetState)
		{
			result.ResetState();
		}
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
		RemoveRange(fromStart: true, 1, resetRemoved: false);
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
		RemoveRange(fromStart: true, 1, resetRemoved: false);
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
			Debug.LogError($"Index {simulatedIndex} is out of range. Collection count is {_written}, Capacity is {Capacity}");
			return -1;
		}
	}

	public void RemoveRange(bool fromStart, int length, bool resetRemoved = true)
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
		if (resetRemoved)
		{
			if (fromStart)
			{
				for (int i = 0; i < length; i++)
				{
					int realIndex = GetRealIndex(i);
					Collection[realIndex].ResetState();
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					int simulatedIndex = _written - (j + 1);
					int realIndex2 = GetRealIndex(simulatedIndex);
					Collection[realIndex2].ResetState();
				}
			}
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

	private bool IsInitializedWithError()
	{
		if (!Initialized)
		{
			Debug.LogError("RingBuffer has not yet been initialized.");
			return false;
		}
		return true;
	}

	public void ResetState()
	{
		Clear();
		if (Collection != null)
		{
			ArrayPool<T>.Shared.Return(Collection);
			Collection = null;
		}
		Initialized = false;
	}

	public void InitializeState()
	{
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