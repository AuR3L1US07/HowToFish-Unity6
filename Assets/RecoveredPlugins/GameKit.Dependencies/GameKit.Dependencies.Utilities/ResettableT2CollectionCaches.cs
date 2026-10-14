using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameKit.Dependencies.Utilities {
public static class ResettableT2CollectionCaches<T1, T2> where T2 : IResettable, new()
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
		foreach (T2 value2 in value.Values)
		{
			ResettableObjectCaches<T2>.Store(value2);
		}
		value.Clear();
		CollectionCaches<T1, T2>.Store(value);
	}
}

}