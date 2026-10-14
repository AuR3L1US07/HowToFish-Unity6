using System;

namespace GameKit.Dependencies.Utilities {
public static class Enums
{
	public const int SHIFT_EVERYTHING_INT = -1;

	public const uint SHIFT_EVERYTHING_UINT = uint.MaxValue;

	public static T FromString<T>(string text, T defaultValue)
	{
		if (string.IsNullOrEmpty(text))
		{
			return defaultValue;
		}
		if (!Enum.IsDefined(typeof(T), text))
		{
			return defaultValue;
		}
		return (T)Enum.Parse(typeof(T), text, ignoreCase: true);
	}

	public static bool ContainsAllocated(this Enum whole, Enum part)
	{
		ulong num = Convert.ToUInt64(whole);
		ulong num2 = Convert.ToUInt64(part);
		return (num & num2) != 0;
	}

	public static bool ReverseContains(this Enum whole, Enum part)
	{
		ulong num = Convert.ToUInt64(whole);
		return (Convert.ToUInt64(part) & num) != 0;
	}

	public static bool Equals(this Enum value, Enum target)
	{
		ulong num = Convert.ToUInt64(value);
		ulong num2 = Convert.ToUInt64(target);
		return num == num2;
	}

	public static bool SameType(Enum a, Enum b)
	{
		return a.GetType() == b.GetType();
	}

	public static int GetHighestValue<T>()
	{
		Type typeFromHandle = typeof(T);
		int num = 0;
		foreach (T value in Enum.GetValues(typeFromHandle))
		{
			int val = Convert.ToInt32(Enum.Parse(typeFromHandle, value.ToString()));
			num = Math.Max(num, val);
		}
		return num;
	}
}

}