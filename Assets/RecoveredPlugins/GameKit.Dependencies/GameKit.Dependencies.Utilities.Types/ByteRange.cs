using System;

namespace GameKit.Dependencies.Utilities.Types {
[Serializable]
public struct ByteRange
{
	public byte Minimum;

	public byte Maximum;

	public ByteRange(byte minimum, byte maximum)
	{
		Minimum = minimum;
		Maximum = maximum;
	}

	public byte RandomExclusive()
	{
		return Bytes.RandomExclusiveRange(Minimum, Maximum);
	}

	public byte RandomInclusive()
	{
		return Bytes.RandomInclusiveRange(Minimum, Maximum);
	}

	public byte Clamp(byte value)
	{
		return Bytes.Clamp(value, Minimum, Maximum);
	}

	public bool InRange(byte value)
	{
		if (value >= Minimum)
		{
			return value <= Maximum;
		}
		return false;
	}
}

}