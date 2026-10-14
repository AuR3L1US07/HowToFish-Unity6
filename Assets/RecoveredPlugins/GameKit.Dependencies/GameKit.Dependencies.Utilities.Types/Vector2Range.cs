using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
[Serializable]
public struct Vector2Range
{
	public FloatRange X;

	public FloatRange Y;

	public Vector2Range(Vector2 minimum, Vector2 maximum)
	{
		X = new FloatRange(minimum.x, maximum.x);
		Y = new FloatRange(minimum.y, maximum.y);
	}

	public Vector2Range(FloatRange minimum, FloatRange maximum)
	{
		X = minimum;
		Y = maximum;
	}

	public Vector2 RandomInclusive()
	{
		return new Vector2(Floats.RandomInclusiveRange(X.Minimum, X.Maximum), Floats.RandomInclusiveRange(Y.Minimum, Y.Maximum));
	}
}

}