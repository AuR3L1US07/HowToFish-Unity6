using System.Collections.Generic;

namespace GameKit.Dependencies.Utilities {
public static class DictionaryFN
{
	public static bool TryGetValueIL2CPP<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, out TValue value)
	{
		return dict.TryGetValue(key, out value);
	}

	public static List<TValue> ValuesToList<TKey, TValue>(this IDictionary<TKey, TValue> dict, bool useCache)
	{
		List<TValue> result = (useCache ? CollectionCaches<TValue>.RetrieveList() : new List<TValue>(dict.Count));
		dict.ValuesToList(ref result, clearLst: false);
		return result;
	}

	public static void ValuesToList<TKey, TValue>(this IDictionary<TKey, TValue> dict, ref List<TValue> result, bool clearLst)
	{
		if (clearLst)
		{
			result.Clear();
		}
		foreach (TValue value in dict.Values)
		{
			result.Add(value);
		}
	}

	public static List<TKey> KeysToList<TKey, TValue>(this IDictionary<TKey, TValue> dict, bool useCache)
	{
		List<TKey> result = (useCache ? CollectionCaches<TKey>.RetrieveList() : new List<TKey>(dict.Count));
		dict.KeysToList(ref result, clearLst: false);
		return result;
	}

	public static void KeysToList<TKey, TValue>(this IDictionary<TKey, TValue> dict, ref List<TKey> result, bool clearLst)
	{
		result.Clear();
		foreach (TKey key in dict.Keys)
		{
			result.Add(key);
		}
	}
}

}