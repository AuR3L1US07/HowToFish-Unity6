using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Ints
{
	private static System.Random _random = new System.Random();

	public static string PadInt(int value, int padding)
	{
		return value.ToString().PadLeft(padding, '0');
	}

	public static int RandomInclusiveRange(int minimum, int maximum)
	{
		return _random.Next(minimum, maximum + 1);
	}

	public static int RandomExclusiveRange(int minimum, int maximum)
	{
		return _random.Next(minimum, maximum);
	}

	public static int Clamp(int value, int minimum, int maximum)
	{
		if (value < minimum)
		{
			value = minimum;
		}
		else if (value > maximum)
		{
			value = maximum;
		}
		return value;
	}

	public static bool ValuesMatch(params int[] values)
	{
		if (values.Length == 0)
		{
			Debug.Log("Ints -> ValuesMatch -> values array is empty.");
			return false;
		}
		int num = values[0];
		for (int i = 1; i < values.Length; i++)
		{
			if (num != values[i])
			{
				return false;
			}
		}
		return true;
	}
}

}