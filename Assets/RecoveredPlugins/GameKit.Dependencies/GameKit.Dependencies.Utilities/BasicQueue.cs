using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public class BasicQueue<T>
{
	private T[] Collection = new T[4];

	private T[] _resizeBuffer = new T[0];

	private int _read;

	private int _written;

	public int Capacity => Collection.Length;

	public int Count => _written;

	public int WriteIndex { get; private set; }

	public T this[int simulatedIndex]
	{
		get
		{
			int realIndex = GetRealIndex(simulatedIndex);
			return Collection[realIndex];
		}
		set
		{
			int realIndex = GetRealIndex(simulatedIndex);
			Collection[realIndex] = value;
		}
	}

	public void Enqueue(T data)
	{
		if (_written == Collection.Length)
		{
			Resize();
		}
		if (WriteIndex >= Collection.Length)
		{
			WriteIndex = 0;
		}
		Collection[WriteIndex] = data;
		WriteIndex++;
		_written++;
	}

	public bool TryDequeue(out T result, bool defaultArrayEntry = true)
	{
		if (_written == 0)
		{
			result = default(T);
			return false;
		}
		result = Dequeue(defaultArrayEntry);
		return true;
	}

	public T Dequeue(bool defaultArrayEntry = true)
	{
		if (_written == 0)
		{
			return default(T);
		}
		T result = Collection[_read];
		if (defaultArrayEntry)
		{
			Collection[_read] = default(T);
		}
		_written--;
		_read++;
		if (_read >= Collection.Length)
		{
			_read = 0;
		}
		return result;
	}

	public bool TryPeek(out T result)
	{
		if (_written == 0)
		{
			result = default(T);
			return false;
		}
		result = Peek();
		return true;
	}

	public T Peek()
	{
		if (_written == 0)
		{
			throw new Exception("Queue of type " + typeof(T).Name + " is empty.");
		}
		return Collection[_read];
	}

	public T GetIndexOrDefault(int simulatedIndex)
	{
		int realIndex = GetRealIndex(simulatedIndex, allowUnusedBuffer: false, log: false);
		if (realIndex != -1 && realIndex < Collection.Length)
		{
			return Collection[realIndex];
		}
		return default(T);
	}

	public void Clear()
	{
		_read = 0;
		WriteIndex = 0;
		_written = 0;
		DefaultCollection(Collection);
		DefaultCollection(_resizeBuffer);
		static void DefaultCollection(T[] array)
		{
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i] = default(T);
			}
		}
	}

	private void Resize()
	{
		int written = _written;
		int num = written * 2;
		int read = _read;
		T[] array = _resizeBuffer;
		if (array.Length < num)
		{
			Array.Resize(ref array, num);
		}
		int num2 = written - read;
		Array.Copy(Collection, read, array, 0, num2);
		if (read > 0)
		{
			Array.Copy(Collection, 0, array, num2, read);
		}
		Collection = array;
		_read = 0;
		WriteIndex = written;
	}

	private int GetRealIndex(int simulatedIndex, bool allowUnusedBuffer = false, bool log = true)
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
			if (log)
			{
				Debug.LogError($"Index {simulatedIndex} is out of range. Collection count is {_written}, Capacity is {Capacity}");
			}
			return -1;
		}
	}
}

}