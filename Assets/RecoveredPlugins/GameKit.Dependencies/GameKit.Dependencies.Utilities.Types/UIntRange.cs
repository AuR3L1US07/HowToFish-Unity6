using System;

namespace GameKit.Dependencies.Utilities.Types {
[Serializable]
public struct UIntRange
{
	public uint Minimum;

	public uint Maximum;

	public UIntRange(uint minimum, uint maximum)
	{
		Minimum = minimum;
		Maximum = maximum;
	}

	public uint RandomExclusive()
	{
		return UInts.RandomExclusiveRange(Minimum, Maximum);
	}

	public uint RandomInclusive()
	{
		return UInts.RandomInclusiveRange(Minimum, Maximum);
	}

	public uint Clamp(uint value)
	{
		return UInts.Clamp(value, Minimum, Maximum);
	}

	public bool InRange(uint value)
	{
		if (value >= Minimum)
		{
			return value <= Maximum;
		}
		return false;
	}
}

}