using System.Runtime.CompilerServices;
using System.Text;

namespace GameKit.Dependencies.Utilities {
public static class Bytes
{
	private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string Pad(this byte value, int padding)
	{
		return Ints.PadInt(value, padding);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte RandomInclusiveRange(byte minimum, byte maximum)
	{
		return (byte)Ints.RandomInclusiveRange(minimum, maximum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte RandomExclusiveRange(byte minimum, byte maximum)
	{
		return (byte)Ints.RandomExclusiveRange(minimum, maximum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Clamp(byte value, byte minimum, byte maximum)
	{
		return (byte)Ints.Clamp(value, minimum, maximum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Min(byte a, byte b)
	{
		if (a >= b)
		{
			return b;
		}
		return a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValuesMatch(params byte[] values)
	{
		return Ints.ValuesMatch((int[])(object)values);
	}

	public static string ToString(this byte[] bytes, int offset, int count)
	{
		return _encoding.GetString(bytes, offset, count);
	}
}

}