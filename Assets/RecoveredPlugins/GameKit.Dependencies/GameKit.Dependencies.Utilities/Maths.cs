namespace GameKit.Dependencies.Utilities {
public static class Maths
{
	public static sbyte ClampSByte(long value, sbyte min, sbyte max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return (sbyte)value;
	}

	public static double ClampDouble(double value, double min, double max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static byte ClampByte(byte value, byte min, byte max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}

}