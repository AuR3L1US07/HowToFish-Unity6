using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Floats
{
	private static System.Random _random = new System.Random();

	public static float SetIfOverTolerance(this float source, float tolerance, float value)
	{
		if (source >= tolerance)
		{
			source = value;
		}
		return source;
	}

	public static float SetIfUnderTolerance(this float source, float tolerance, float value)
	{
		if (source <= tolerance)
		{
			source = value;
		}
		return source;
	}

	public static float TimeRemainingValue(this float endTime)
	{
		if (endTime - Time.time < 0f)
		{
			return -1f;
		}
		return endTime - Time.time;
	}

	public static int TimeRemainingValue(this float endTime, bool useFloor = true)
	{
		if (endTime - Time.time < 0f)
		{
			return -1;
		}
		float f = endTime - Time.time;
		if (!useFloor)
		{
			return Mathf.CeilToInt(f);
		}
		return Mathf.FloorToInt(f);
	}

	public static string TimeRemainingText(this float value, byte segments, bool emptyOnZero = false)
	{
		if (emptyOnZero && value <= 0f)
		{
			return string.Empty;
		}
		TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(Mathf.RoundToInt(value), 0));
		int num = Mathf.FloorToInt(timeSpan.Hours);
		int num2 = Mathf.FloorToInt(timeSpan.Minutes);
		int num3 = Mathf.FloorToInt(timeSpan.Seconds);
		switch (segments)
		{
		case 1:
			num3 += num2 * 60;
			num3 += num * 3600;
			return $"{num3:D2}";
		case 2:
			num2 += num * 60;
			return $"{num2:D2}:{num3:D2}";
		default:
			return $"{num:D2}:{num2:D2}:{num3:D2}";
		}
	}

	public static float RandomInclusiveRange(float minimum, float maximum)
	{
		double num = Convert.ToDouble(minimum);
		double num2 = Convert.ToDouble(maximum);
		return Convert.ToSingle(_random.NextDouble() * (num2 - num) + num);
	}

	public static float Random01()
	{
		return RandomInclusiveRange(0f, 1f);
	}

	public static bool Near(this float a, float b, float tolerance = 0.01f)
	{
		return Mathf.Abs(a - b) <= tolerance;
	}

	public static float Clamp(float value, float min, float max, ref bool clamped)
	{
		clamped = value < min;
		if (clamped)
		{
			return min;
		}
		clamped = value > min;
		if (clamped)
		{
			return max;
		}
		clamped = false;
		return value;
	}

	public static float Variance(this float source, float variance)
	{
		float num = RandomInclusiveRange(1f - variance, 1f + variance);
		return source * num;
	}

	public static void Variance(this float source, float variance, ref float result)
	{
		float num = RandomInclusiveRange(1f - variance, 1f + variance);
		result = source * num;
	}

	public static float PreciseSign(float value)
	{
		if (value == 0f)
		{
			return 0f;
		}
		return Mathf.Sign(value);
	}

	public static bool InRange(this float source, float rangeMin, float rangeMax)
	{
		if (source >= rangeMin)
		{
			return source <= rangeMax;
		}
		return false;
	}

	public static float RandomlyFlip(this float value)
	{
		if (Ints.RandomInclusiveRange(0, 1) == 0)
		{
			return value;
		}
		return value *= -1f;
	}
}

}