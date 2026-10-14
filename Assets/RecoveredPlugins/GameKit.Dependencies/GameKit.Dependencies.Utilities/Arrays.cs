using System;
using System.Collections.Generic;
using System.Text;
using GameKit.Dependencies.Utilities.Types;

namespace GameKit.Dependencies.Utilities {
public static class Arrays
{
	private static readonly Random _random = new Random();

	private static readonly StringBuilder _stringBuilder = new StringBuilder();

	public static string ToString<T>(this IEnumerable<T> collection, string delimeter = ", ")
	{
		if (collection == null)
		{
			return string.Empty;
		}
		_stringBuilder.Clear();
		foreach (T item in collection)
		{
			_stringBuilder.Append(item.ToString() + delimeter);
		}
		if (_stringBuilder.Length > delimeter.Length)
		{
			_stringBuilder.Length -= delimeter.Length;
		}
		return _stringBuilder.ToString();
	}

	public static bool FastReferenceRemove<T>(this List<T> list, object value)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if ((object)list[i] == value)
			{
				list.FastIndexRemove(i);
				return true;
			}
		}
		return false;
	}

	public static void FastIndexRemove<T>(this List<T> list, int index)
	{
		list[index] = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
	}

	public static void Shuffle<T>(this T[] array)
	{
		int num = array.Length;
		for (int i = 0; i < num - 1; i++)
		{
			int num2 = i + _random.Next(num - i);
			T val = array[num2];
			array[num2] = array[i];
			array[i] = val;
		}
	}

	public static void Shuffle<T>(this List<T> lst)
	{
		int count = lst.Count;
		for (int i = 0; i < count - 1; i++)
		{
			int index = i + _random.Next(count - i);
			T value = lst[index];
			lst[index] = lst[i];
			lst[i] = value;
		}
	}

	public static void AddOrdered<T>(this List<T> collection, T item) where T : IOrderable
	{
		int count = collection.Count;
		int order = item.Order;
		if (count != 0)
		{
			if (order < collection[collection.Count - 1].Order)
			{
				for (int i = 0; i < count; i++)
				{
					if (order <= collection[i].Order)
					{
						collection.Insert(i, item);
						break;
					}
				}
				return;
			}
		}
		collection.Add(item);
	}
}

}