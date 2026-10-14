using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
[Serializable]
public struct FloatRange
{
	public float Minimum;

	public float Maximum;

	public FloatRange(float minimum, float maximum)
	{
		Minimum = minimum;
		Maximum = maximum;
	}

	public float RandomInclusive()
	{
		return Floats.RandomInclusiveRange(Minimum, Maximum);
	}

	public float Lerp(float percent)
	{
		return Mathf.Lerp(Minimum, Maximum, percent);
	}
}

}