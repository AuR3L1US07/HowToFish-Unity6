using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameKit.Dependencies.Utilities.Types;

namespace GameKit.Dependencies.Utilities {
public static class ResettableCollectionCaches<T1, T2> where T1 : IResettable, new() where T2 : IResettable, new()
{
	public static Dictionary<T1, T2> RetrieveDictionary()
	{
		return CollectionCaches<T1, T2>.RetrieveDictionary();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref Dictionary<T1, T2> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(Dictionary<T1, T2> value)
	{
		if (value == null)
		{
			return;
		}
		foreach (KeyValuePair<T1, T2> item in value)
		{
			ResettableObjectCaches<T1>.Store(item.Key);
			ResettableObjectCaches<T2>.Store(item.Value);
		}
		value.Clear();
		CollectionCaches<T1, T2>.Store(value);
	}
}
public static class ResettableCollectionCaches<T> where T : IResettable, new()
{
	private static readonly Stack<ResettableRingBuffer<T>> _resettableRingBufferCache = new Stack<ResettableRingBuffer<T>>();

	public static ResettableRingBuffer<T> RetrieveRingBuffer()
	{
		if (!_resettableRingBufferCache.TryPop(out var result))
		{
			return new ResettableRingBuffer<T>();
		}
		return result;
	}

	public static T[] RetrieveArray()
	{
		return CollectionCaches<T>.RetrieveArray();
	}

	public static List<T> RetrieveList()
	{
		return CollectionCaches<T>.RetrieveList();
	}

	public static SortedSet<T> RetrieveSortedSet()
	{
		return CollectionCaches<T>.RetrieveSortedSet();
	}

	public static HashSet<T> RetrieveHashSet()
	{
		return CollectionCaches<T>.RetrieveHashSet();
	}

	public static Queue<T> RetrieveQueue()
	{
		return CollectionCaches<T>.RetrieveQueue();
	}

	public static BasicQueue<T> RetrieveBasicQueue()
	{
		return CollectionCaches<T>.RetrieveBasicQueue();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref ResettableRingBuffer<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(ResettableRingBuffer<T> value)
	{
		if (value != null)
		{
			value.ResetState();
			_resettableRingBufferCache.Push(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref T[] value, int count)
	{
		Store(value, count);
		value = null;
	}

	public static void Store(T[] value, int count)
	{
		if (value != null)
		{
			for (int i = 0; i < count; i++)
			{
				ResettableObjectCaches<T>.Store(value[i]);
			}
			CollectionCaches<T>.Store(value, count);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref List<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(List<T> value)
	{
		if (value != null)
		{
			for (int i = 0; i < value.Count; i++)
			{
				ResettableObjectCaches<T>.Store(value[i]);
			}
			value.Clear();
			CollectionCaches<T>.Store(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref SortedSet<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(SortedSet<T> value)
	{
		if (value == null)
		{
			return;
		}
		foreach (T item in value)
		{
			ResettableObjectCaches<T>.Store(item);
		}
		value.Clear();
		CollectionCaches<T>.Store(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref HashSet<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(HashSet<T> value)
	{
		if (value == null)
		{
			return;
		}
		foreach (T item in value)
		{
			ResettableObjectCaches<T>.Store(item);
		}
		value.Clear();
		CollectionCaches<T>.Store(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref Queue<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(Queue<T> value)
	{
		if (value == null)
		{
			return;
		}
		foreach (T item in value)
		{
			ResettableObjectCaches<T>.Store(item);
		}
		value.Clear();
		CollectionCaches<T>.Store(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref BasicQueue<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(BasicQueue<T> value)
	{
		if (value != null)
		{
			T result;
			while (value.TryDequeue(out result))
			{
				ResettableObjectCaches<T>.Store(result);
			}
			value.Clear();
			CollectionCaches<T>.Store(value);
		}
	}
}

}