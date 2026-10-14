using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Vectors
{
	private static readonly Vector3 VECTOR3_ZERO = new Vector3(0f, 0f, 0f);

	private const float FLOAT_EPSILON = 1E-05f;

	public static float GetRate(this Vector3 a, Vector3 b, float duration, out float distance, uint interval = 1u)
	{
		distance = Vector3.Distance(a, b);
		return distance / (duration * (float)interval);
	}

	public static Vector3 Add(this Vector3 v3, Vector2 v2)
	{
		return v3 + new Vector3(v2.x, v2.y, 0f);
	}

	public static Vector3 Subtract(this Vector3 v3, Vector2 v2)
	{
		return v3 - new Vector3(v2.x, v2.y, 0f);
	}

	public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
	{
		Vector3 vector = b - a;
		return Mathf.Clamp01(Vector3.Dot(value - a, vector) / Vector3.Dot(vector, vector));
	}

	public static bool Near(this Vector3 a, Vector3 b, float tolerance = 0.01f)
	{
		return Vector3.Distance(a, b) <= tolerance;
	}

	public static bool IsNan(this Vector3 source)
	{
		if (!float.IsNaN(source.x) && !float.IsNaN(source.y))
		{
			return float.IsNaN(source.z);
		}
		return true;
	}

	public static Vector3 Lerp3(Vector3 a, Vector3 b, Vector3 c, float percent)
	{
		Vector3 a2 = Vector3.Lerp(a, b, percent);
		Vector3 b2 = Vector3.Lerp(b, c, percent);
		return Vector3.Lerp(a2, b2, percent);
	}

	public static Vector3 Lerp3(Vector3[] vectors, float percent)
	{
		if (vectors.Length < 3)
		{
			Debug.LogWarning("Vectors -> Lerp3 -> Vectors length must be 3.");
			return Vector3.zero;
		}
		return Lerp3(vectors[0], vectors[1], vectors[2], percent);
	}

	public static Vector3 Multiply(this Vector3 src, Vector3 multiplier)
	{
		return new Vector3(src.x * multiplier.x, src.y * multiplier.y, src.z * multiplier.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float FastDistance(Vector3 a, Vector3 b)
	{
		float num = a.x - b.x;
		float num2 = a.y - b.y;
		float num3 = a.z - b.z;
		return (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float FastSqrMagnitude(Vector3 vector)
	{
		return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 FastNormalize(Vector3 value)
	{
		float num = (float)Math.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
		if (num > 1E-05f)
		{
			Vector3 result = default(Vector3);
			result.x = value.x / num;
			result.y = value.y / num;
			result.z = value.z / num;
			return result;
		}
		return VECTOR3_ZERO;
	}

	public static float GetRate(this Vector2 a, Vector2 goal, float duration, out float distance, uint interval = 1u)
	{
		distance = Vector2.Distance(a, goal);
		return distance / (duration * (float)interval);
	}

	public static Vector2 Lerp3(Vector2 a, Vector2 b, Vector2 c, float percent)
	{
		Vector2 a2 = Vector2.Lerp(a, b, percent);
		Vector2 b2 = Vector2.Lerp(b, c, percent);
		return Vector2.Lerp(a2, b2, percent);
	}

	public static Vector2 Lerp2(Vector2[] vectors, float percent)
	{
		if (vectors.Length < 3)
		{
			Debug.LogWarning("Vectors -> Lerp3 -> Vectors length must be 3.");
			return Vector2.zero;
		}
		return Lerp3(vectors[0], vectors[1], vectors[2], percent);
	}

	public static Vector2 Multiply(this Vector2 src, Vector2 multiplier)
	{
		return new Vector2(src.x * multiplier.x, src.y * multiplier.y);
	}
}

}