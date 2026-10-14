using System;

namespace GameKit.Dependencies.Utilities.Types {
[Serializable]
public struct IntRange
{
	public int Minimum;

	public int Maximum;

	public IntRange(int minimum, int maximum)
	{
		Minimum = minimum;
		Maximum = maximum;
	}

	public float RandomExclusive()
	{
		return Ints.RandomExclusiveRange(Minimum, Maximum);
	}

	public float RandomInclusive()
	{
		return Ints.RandomInclusiveRange(Minimum, Maximum);
	}

	public int Clamp(int value)
	{
		if (value < Minimum)
		{
			return Minimum;
		}
		if (value > Maximum)
		{
			return Maximum;
		}
		return value;
	}
}

}