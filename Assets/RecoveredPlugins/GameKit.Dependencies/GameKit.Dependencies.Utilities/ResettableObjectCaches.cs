using System.Runtime.CompilerServices;

namespace GameKit.Dependencies.Utilities {
public static class ResettableObjectCaches<T> where T : IResettable, new()
{
	public static T Retrieve()
	{
		T result = ObjectCaches<T>.Retrieve();
		result.InitializeState();
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void StoreAndDefault(ref T value)
	{
		Store(value);
		value = default(T);
	}

	public static void Store(T value)
	{
		if (value != null)
		{
			value.ResetState();
			ObjectCaches<T>.Store(value);
		}
	}
}

}