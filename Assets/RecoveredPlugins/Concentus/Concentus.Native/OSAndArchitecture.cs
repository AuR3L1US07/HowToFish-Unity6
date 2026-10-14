using System;

namespace Concentus.Native {
internal readonly struct OSAndArchitecture : IEquatable<OSAndArchitecture>
{
	public PlatformOperatingSystem OS { get; }

	public PlatformArchitecture Architecture { get; }

	public OSAndArchitecture(PlatformOperatingSystem OS, PlatformArchitecture architecture)
	{
		this.OS = OS;
		Architecture = architecture;
	}

	public override string ToString()
	{
		return OS.GetRuntimeIdString() + "-" + Architecture.GetRuntimeIdString();
	}

	public override int GetHashCode()
	{
		return OS.GetHashCode() * 17 + Architecture.GetHashCode() * 37119;
	}

	public override bool Equals(object other)
	{
		if (other == null || GetType() != other.GetType())
		{
			return false;
		}
		return Equals((OSAndArchitecture)other);
	}

	public bool Equals(OSAndArchitecture other)
	{
		if (OS == other.OS)
		{
			return Architecture == other.Architecture;
		}
		return false;
	}

	public static bool operator ==(OSAndArchitecture left, OSAndArchitecture right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(OSAndArchitecture left, OSAndArchitecture right)
	{
		return !(left == right);
	}
}

}