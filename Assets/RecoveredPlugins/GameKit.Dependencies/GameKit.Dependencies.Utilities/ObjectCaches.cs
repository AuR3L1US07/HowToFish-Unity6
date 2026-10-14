using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameKit.Dependencies.Utilities {
public static class ObjectCaches<T> where T : new()
{
	private static readonly Stack<T> _stack = new Stack<T>();

	public static T Retrieve()
	{
		if (!_stack.TryPop(out var result))
		{
			return new T();
		}
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
			_stack.Push(value);
		}
	}
}

}