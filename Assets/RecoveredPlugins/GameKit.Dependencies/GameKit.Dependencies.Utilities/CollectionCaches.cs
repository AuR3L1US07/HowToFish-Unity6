using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameKit.Dependencies.Utilities {
public static class CollectionCaches<T1, T2>
{
	private static readonly Stack<Dictionary<T1, T2>> _dictionaryCache = new Stack<Dictionary<T1, T2>>();

	public static Dictionary<T1, T2> RetrieveDictionary()
	{
		if (!_dictionaryCache.TryPop(out var result))
		{
			return new Dictionary<T1, T2>();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref Dictionary<T1, T2> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(Dictionary<T1, T2> value)
	{
		if (value != null)
		{
			value.Clear();
			_dictionaryCache.Push(value);
		}
	}
}
public static class CollectionCaches<T>
{
	private static readonly Stack<T[]> _arrayCache = new Stack<T[]>();

	private static readonly Stack<List<T>> _listCache = new Stack<List<T>>();

	private static readonly Stack<SortedSet<T>> _sortedSetCache = new Stack<SortedSet<T>>();

	private static readonly Stack<Queue<T>> _queueCache = new Stack<Queue<T>>();

	private static readonly Stack<BasicQueue<T>> _basicQueueCache = new Stack<BasicQueue<T>>();

	private static readonly Stack<HashSet<T>> _hashSetCache = new Stack<HashSet<T>>();

	public static T[] RetrieveArray()
	{
		if (!_arrayCache.TryPop(out var result))
		{
			return new T[0];
		}
		return result;
	}

	public static List<T> RetrieveList()
	{
		if (!_listCache.TryPop(out var result))
		{
			return new List<T>();
		}
		return result;
	}

	public static SortedSet<T> RetrieveSortedSet()
	{
		if (!_sortedSetCache.TryPop(out var result))
		{
			return new SortedSet<T>();
		}
		return result;
	}

	public static Queue<T> RetrieveQueue()
	{
		if (!_queueCache.TryPop(out var result))
		{
			return new Queue<T>();
		}
		return result;
	}

	public static BasicQueue<T> RetrieveBasicQueue()
	{
		if (!_basicQueueCache.TryPop(out var result))
		{
			return new BasicQueue<T>();
		}
		return result;
	}

	public static Queue<T> RetrieveQueue(T entry)
	{
		if (!_queueCache.TryPop(out var result))
		{
			result = new Queue<T>();
		}
		result.Enqueue(entry);
		return result;
	}

	public static List<T> RetrieveList(T entry)
	{
		if (!_listCache.TryPop(out var result))
		{
			result = new List<T>();
		}
		result.Add(entry);
		return result;
	}

	public static HashSet<T> RetrieveHashSet()
	{
		if (!_hashSetCache.TryPop(out var result))
		{
			return new HashSet<T>();
		}
		return result;
	}

	public static HashSet<T> RetrieveHashSet(T entry)
	{
		if (!_hashSetCache.TryPop(out var result))
		{
			return new HashSet<T>();
		}
		result.Add(entry);
		return result;
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
				value[i] = default(T);
			}
			_arrayCache.Push(value);
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
			value.Clear();
			_listCache.Push(value);
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
		if (value != null)
		{
			value.Clear();
			_sortedSetCache.Push(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref Queue<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(Queue<T> value)
	{
		if (value != null)
		{
			value.Clear();
			_queueCache.Push(value);
		}
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
			value.Clear();
			_basicQueueCache.Push(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref HashSet<T> value)
	{
		Store(value);
		value = null;
	}

	public static void Store(HashSet<T> value)
	{
		if (value != null)
		{
			value.Clear();
			_hashSetCache.Push(value);
		}
	}
}

}