using System.Runtime.CompilerServices;

namespace GameKit.Dependencies.Utilities {
public static class UInts
{
	public static string Pad(this uint value, int padding)
	{
		if (padding < 0)
		{
			padding = 0;
		}
		return value.ToString().PadLeft(padding, '0');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RandomInclusiveRange(uint minimum, uint maximum)
	{
		return (uint)Ints.RandomInclusiveRange((int)minimum, (int)maximum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RandomExclusiveRange(uint minimum, uint maximum)
	{
		return (uint)Ints.RandomExclusiveRange((int)minimum, (int)maximum);
	}

	public static uint Clamp(uint value, uint minimum, uint maximum)
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

	public static uint Min(uint a, uint b)
	{
		if (a >= b)
		{
			return b;
		}
		return a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValuesMatch(params uint[] values)
	{
		return Ints.ValuesMatch((int[])(object)values);
	}
}

}