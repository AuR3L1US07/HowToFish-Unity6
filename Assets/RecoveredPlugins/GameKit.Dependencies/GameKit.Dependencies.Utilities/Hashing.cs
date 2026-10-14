namespace GameKit.Dependencies.Utilities {
public static class Hashing
{
	private const uint FNV_offset_basis32 = 2166136261u;

	private const uint FNV_prime32 = 16777619u;

	private const ulong FNV_offset_basis64 = 14695981039346656037uL;

	private const ulong FNV_prime64 = 1099511628211uL;

	public static ushort GetStableHashU16(this string txt)
	{
		uint stableHashU = txt.GetStableHashU32();
		return (ushort)((stableHashU >> 16) ^ stableHashU);
	}

	public static uint GetStableHashU32(this string txt)
	{
		uint num = 2166136261u;
		foreach (uint num2 in txt)
		{
			num *= 16777619;
			num ^= num2;
		}
		return num;
	}

	public static ulong GetStableHashU64(this string txt)
	{
		ulong num = 14695981039346656037uL;
		foreach (ulong num2 in txt)
		{
			num *= 1099511628211L;
			num ^= num2;
		}
		return num;
	}
}

}