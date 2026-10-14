using System.Collections.Generic;

namespace GameKit.Dependencies.Utilities {
public static class ListsFN
{
	public static int AddRangeUnique<T>(this List<T> collection, IEnumerable<T> items)
	{
		int num = 0;
		foreach (T item in items)
		{
			if (!collection.Contains(item))
			{
				collection.Add(item);
				num++;
			}
		}
		return num;
	}

	public static bool AddUnique<T>(this List<T> collection, T item)
	{
		if (!collection.Contains(item))
		{
			collection.Add(item);
			return true;
		}
		return false;
	}
}

}