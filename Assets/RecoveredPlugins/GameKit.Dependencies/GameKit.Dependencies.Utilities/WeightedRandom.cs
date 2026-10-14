using System.Collections.Generic;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class WeightedRandom
{
	public static void GetEntries<T>(List<T> source, IntRange countRange, ref Dictionary<T, uint> results, bool allowRepeatingDrops = false) where T : IWeighted
	{
		if (source == null || source.Count == 0)
		{
			Debug.Log("Source list of type " + typeof(T).Name + " cannot be null or empty.");
			return;
		}
		int num = Ints.RandomInclusiveRange(countRange.Minimum, countRange.Maximum);
		if (num == 0)
		{
			return;
		}
		Dictionary<T, byte> value = CollectionCaches<T, byte>.RetrieveDictionary();
		float num2 = 0f;
		for (int i = 0; i < source.Count; i++)
		{
			num2 += source[i].GetWeight();
		}
		List<T> list = CollectionCaches<T>.RetrieveList();
		foreach (T item in source)
		{
			list.Add(item);
		}
		while (results.Count < num)
		{
			int count = results.Count;
			float num3 = num2;
			float num4 = Random.Range(0f, num2);
			for (int j = 0; j < list.Count; j++)
			{
				T key = list[j];
				float weight = key.GetWeight();
				if (num4 <= weight)
				{
					results.TryGetValueIL2CPP(key, out var value2);
					results[key] = value2 + 1;
					if (!allowRepeatingDrops)
					{
						list.RemoveAt(j);
						num2 -= weight;
					}
					break;
				}
				num3 -= weight;
			}
			if (results.Count == count)
			{
				break;
			}
		}
		CollectionCaches<T, byte>.Store(value);
	}
}

}