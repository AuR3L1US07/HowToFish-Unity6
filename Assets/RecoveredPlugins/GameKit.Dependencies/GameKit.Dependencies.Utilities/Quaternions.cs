using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Quaternions
{
	public static float GetRate(this Quaternion a, Quaternion goal, float duration, out float angle, uint interval = 1u, float tolerance = 0f)
	{
		angle = a.Angle(goal, precise: true);
		return angle / (duration * (float)interval);
	}

	public static Quaternion Subtract(this Quaternion a, Quaternion b)
	{
		return Quaternion.Inverse(b) * a;
	}

	public static Quaternion Add(this Quaternion a, Quaternion b)
	{
		return a * b;
	}

	public static bool Matches(this Quaternion a, Quaternion b, bool precise = false)
	{
		if (precise)
		{
			if (a.w == b.w && a.x == b.x && a.y == b.y)
			{
				return a.z == b.z;
			}
			return false;
		}
		return a == b;
	}

	public static float Angle(this Quaternion a, Quaternion b, bool precise = false)
	{
		if (precise)
		{
			return Mathf.Acos(Mathf.Min(Mathf.Abs(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w), 1f)) * 2f * 57.29578f;
		}
		return Quaternion.Angle(a, b);
	}
}

}