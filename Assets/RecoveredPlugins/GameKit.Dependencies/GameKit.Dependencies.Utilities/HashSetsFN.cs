using System.Collections.Generic;

namespace GameKit.Dependencies.Utilities {
public static class HashSetsFN
{
	public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			hashSet.Add(item);
		}
	}

	public static List<T> ToList<T>(this HashSet<T> collection, bool useCache)
	{
		List<T> lst = (useCache ? CollectionCaches<T>.RetrieveList() : new List<T>(collection.Count));
		collection.ToList(ref lst, clearLst: false);
		return lst;
	}

	public static void ToList<T>(this HashSet<T> collection, ref List<T> lst, bool clearLst)
	{
		if (clearLst)
		{
			lst.Clear();
		}
		foreach (T item in collection)
		{
			lst.Add(item);
		}
	}
}

}