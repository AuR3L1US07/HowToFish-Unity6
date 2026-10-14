using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Concentus.Native {
internal static class NativePlatformUtils
{
	private static bool _cachedPlatformInfoExists = false;

	private static OSAndArchitecture _cachedPlatformInfo = new OSAndArchitecture(PlatformOperatingSystem.Unknown, PlatformArchitecture.Unknown);

	private static readonly IDictionary<string, NativeLibraryStatus> _loadedLibraries = new Dictionary<string, NativeLibraryStatus>();

	private static readonly object _mutex = new object();

	private static readonly IReadOnlyDictionary<string, string[]> RidInheritanceMappings = new Dictionary<string, string[]>
	{
		{
			"android",
			new string[4] { "linux-bionic", "linux", "unix", "any" }
		},
		{
			"android-arm",
			new string[8] { "android", "linux-bionic-arm", "linux-bionic", "linux-arm", "linux", "unix-arm", "unix", "any" }
		},
		{
			"android-arm64",
			new string[8] { "android", "linux-bionic-arm64", "linux-bionic", "linux-arm64", "linux", "unix-arm64", "unix", "any" }
		},
		{
			"android-x64",
			new string[8] { "android", "linux-bionic-x64", "linux-bionic", "linux-x64", "linux", "unix-x64", "unix", "any" }
		},
		{
			"android-x86",
			new string[8] { "android", "linux-bionic-x86", "linux-bionic", "linux-x86", "linux", "unix-x86", "unix", "any" }
		},
		{
			"any",
			new string[0]
		},
		{
			"base",
			new string[0]
		},
		{
			"browser",
			new string[1] { "any" }
		},
		{
			"browser-wasm",
			new string[2] { "browser", "any" }
		},
		{
			"freebsd",
			new string[2] { "unix", "any" }
		},
		{
			"freebsd-arm64",
			new string[4] { "freebsd", "unix-arm64", "unix", "any" }
		},
		{
			"freebsd-x64",
			new string[4] { "freebsd", "unix-x64", "unix", "any" }
		},
		{
			"illumos",
			new string[2] { "unix", "any" }
		},
		{
			"illumos-x64",
			new string[4] { "illumos", "unix-x64", "unix", "any" }
		},
		{
			"ios",
			new string[2] { "unix", "any" }
		},
		{
			"ios-arm",
			new string[4] { "ios", "unix-arm", "unix", "any" }
		},
		{
			"ios-arm64",
			new string[4] { "ios", "unix-arm64", "unix", "any" }
		},
		{
			"iossimulator",
			new string[3] { "ios", "unix", "any" }
		},
		{
			"iossimulator-arm64",
			new string[6] { "iossimulator", "ios-arm64", "ios", "unix-arm64", "unix", "any" }
		},
		{
			"iossimulator-x64",
			new string[6] { "iossimulator", "ios-x64", "ios", "unix-x64", "unix", "any" }
		},
		{
			"iossimulator-x86",
			new string[6] { "iossimulator", "ios-x86", "ios", "unix-x86", "unix", "any" }
		},
		{
			"ios-x64",
			new string[4] { "ios", "unix-x64", "unix", "any" }
		},
		{
			"ios-x86",
			new string[4] { "ios", "unix-x86", "unix", "any" }
		},
		{
			"linux",
			new string[2] { "unix", "any" }
		},
		{
			"linux-arm",
			new string[4] { "linux", "unix-arm", "unix", "any" }
		},
		{
			"linux-arm64",
			new string[4] { "linux", "unix-arm64", "unix", "any" }
		},
		{
			"linux-armel",
			new string[4] { "linux", "unix-armel", "unix", "any" }
		},
		{
			"linux-armv6",
			new string[4] { "linux", "unix-armv6", "unix", "any" }
		},
		{
			"linux-bionic",
			new string[3] { "linux", "unix", "any" }
		},
		{
			"linux-bionic-arm",
			new string[6] { "linux-bionic", "linux-arm", "linux", "unix-arm", "unix", "any" }
		},
		{
			"linux-bionic-arm64",
			new string[6] { "linux-bionic", "linux-arm64", "linux", "unix-arm64", "unix", "any" }
		},
		{
			"linux-bionic-x64",
			new string[6] { "linux-bionic", "linux-x64", "linux", "unix-x64", "unix", "any" }
		},
		{
			"linux-bionic-x86",
			new string[6] { "linux-bionic", "linux-x86", "linux", "unix-x86", "unix", "any" }
		},
		{
			"linux-loongarch64",
			new string[4] { "linux", "unix-loongarch64", "unix", "any" }
		},
		{
			"linux-mips64",
			new string[4] { "linux", "unix-mips64", "unix", "any" }
		},
		{
			"linux-musl",
			new string[3] { "linux", "unix", "any" }
		},
		{
			"linux-musl-arm",
			new string[6] { "linux-musl", "linux-arm", "linux", "unix-arm", "unix", "any" }
		},
		{
			"linux-musl-arm64",
			new string[6] { "linux-musl", "linux-arm64", "linux", "unix-arm64", "unix", "any" }
		},
		{
			"linux-musl-armel",
			new string[6] { "linux-musl", "linux-armel", "linux", "unix-armel", "unix", "any" }
		},
		{
			"linux-musl-armv6",
			new string[6] { "linux-musl", "linux-armv6", "linux", "unix-armv6", "unix", "any" }
		},
		{
			"linux-musl-ppc64le",
			new string[6] { "linux-musl", "linux-ppc64le", "linux", "unix-ppc64le", "unix", "any" }
		},
		{
			"linux-musl-riscv64",
			new string[6] { "linux-musl", "linux-riscv64", "linux", "unix-riscv64", "unix", "any" }
		},
		{
			"linux-musl-s390x",
			new string[6] { "linux-musl", "linux-s390x", "linux", "unix-s390x", "unix", "any" }
		},
		{
			"linux-musl-x64",
			new string[6] { "linux-musl", "linux-x64", "linux", "unix-x64", "unix", "any" }
		},
		{
			"linux-musl-x86",
			new string[6] { "linux-musl", "linux-x86", "linux", "unix-x86", "unix", "any" }
		},
		{
			"linux-ppc64le",
			new string[4] { "linux", "unix-ppc64le", "unix", "any" }
		},
		{
			"linux-riscv64",
			new string[4] { "linux", "unix-riscv64", "unix", "any" }
		},
		{
			"linux-s390x",
			new string[4] { "linux", "unix-s390x", "unix", "any" }
		},
		{
			"linux-x64",
			new string[4] { "linux", "unix-x64", "unix", "any" }
		},
		{
			"linux-x86",
			new string[4] { "linux", "unix-x86", "unix", "any" }
		},
		{
			"maccatalyst",
			new string[3] { "ios", "unix", "any" }
		},
		{
			"maccatalyst-arm64",
			new string[6] { "maccatalyst", "ios-arm64", "ios", "unix-arm64", "unix", "any" }
		},
		{
			"maccatalyst-x64",
			new string[6] { "maccatalyst", "ios-x64", "ios", "unix-x64", "unix", "any" }
		},
		{
			"osx",
			new string[2] { "unix", "any" }
		},
		{
			"osx-arm64",
			new string[4] { "osx", "unix-arm64", "unix", "any" }
		},
		{
			"osx-x64",
			new string[4] { "osx", "unix-x64", "unix", "any" }
		},
		{
			"solaris",
			new string[2] { "unix", "any" }
		},
		{
			"solaris-x64",
			new string[4] { "solaris", "unix-x64", "unix", "any" }
		},
		{
			"tvos",
			new string[2] { "unix", "any" }
		},
		{
			"tvos-arm64",
			new string[4] { "tvos", "unix-arm64", "unix", "any" }
		},
		{
			"tvossimulator",
			new string[3] { "tvos", "unix", "any" }
		},
		{
			"tvossimulator-arm64",
			new string[6] { "tvossimulator", "tvos-arm64", "tvos", "unix-arm64", "unix", "any" }
		},
		{
			"tvossimulator-x64",
			new string[6] { "tvossimulator", "tvos-x64", "tvos", "unix-x64", "unix", "any" }
		},
		{
			"tvos-x64",
			new string[4] { "tvos", "unix-x64", "unix", "any" }
		},
		{
			"unix",
			new string[1] { "any" }
		},
		{
			"unix-arm",
			new string[2] { "unix", "any" }
		},
		{
			"unix-arm64",
			new string[2] { "unix", "any" }
		},
		{
			"unix-armel",
			new string[2] { "unix", "any" }
		},
		{
			"unix-armv6",
			new string[2] { "unix", "any" }
		},
		{
			"unix-loongarch64",
			new string[2] { "unix", "any" }
		},
		{
			"unix-mips64",
			new string[2] { "unix", "any" }
		},
		{
			"unix-ppc64le",
			new string[2] { "unix", "any" }
		},
		{
			"unix-riscv64",
			new string[2] { "unix", "any" }
		},
		{
			"unix-s390x",
			new string[2] { "unix", "any" }
		},
		{
			"unix-x64",
			new string[2] { "unix", "any" }
		},
		{
			"unix-x86",
			new string[2] { "unix", "any" }
		},
		{
			"wasi",
			new string[1] { "any" }
		},
		{
			"wasi-wasm",
			new string[2] { "wasi", "any" }
		},
		{
			"win",
			new string[1] { "any" }
		},
		{
			"win-arm",
			new string[2] { "win", "any" }
		},
		{
			"win-arm64",
			new string[2] { "win", "any" }
		},
		{
			"win-x64",
			new string[2] { "win", "any" }
		},
		{
			"win-x86",
			new string[2] { "win", "any" }
		}
	};

	internal static OSAndArchitecture GetCurrentPlatform(TextWriter logger)
	{
		lock (_mutex)
		{
			if (!_cachedPlatformInfoExists)
			{
				_cachedPlatformInfo = GetCurrentPlatformInternal(logger);
			}
			return _cachedPlatformInfo;
		}
	}

	internal static OSAndArchitecture GetCurrentPlatformInternal(TextWriter logger)
	{
		PlatformOperatingSystem platformOperatingSystem = PlatformOperatingSystem.Unknown;
		PlatformArchitecture platformArchitecture = PlatformArchitecture.Unknown;
		if (platformOperatingSystem == PlatformOperatingSystem.Unknown)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				platformOperatingSystem = PlatformOperatingSystem.Windows;
			}
			else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_STORAGE")))
			{
				platformOperatingSystem = PlatformOperatingSystem.Android;
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				platformOperatingSystem = PlatformOperatingSystem.MacOS;
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				platformOperatingSystem = PlatformOperatingSystem.Linux;
			}
		}
		if (platformOperatingSystem != PlatformOperatingSystem.Unknown && platformArchitecture == PlatformArchitecture.Unknown)
		{
			platformArchitecture = TryGetNativeArchitecture(platformOperatingSystem, logger);
		}
		if (platformArchitecture == PlatformArchitecture.Unknown)
		{
			switch (RuntimeInformation.ProcessArchitecture)
			{
			case Architecture.X86:
				platformArchitecture = PlatformArchitecture.I386;
				break;
			case Architecture.X64:
				platformArchitecture = PlatformArchitecture.X64;
				break;
			case Architecture.Arm:
				platformArchitecture = PlatformArchitecture.ArmV7;
				break;
			case Architecture.Arm64:
				platformArchitecture = PlatformArchitecture.Arm64;
				break;
			}
		}
		return new OSAndArchitecture(platformOperatingSystem, platformArchitecture);
	}

	internal static NativeLibraryStatus PrepareNativeLibrary(string libraryName, TextWriter logger)
	{
		logger?.WriteLine("Preparing native library \"{0}\"", libraryName);
		OSAndArchitecture currentPlatform = GetCurrentPlatform(logger);
		logger?.WriteLine("Detected current platform as \"{0}\"", currentPlatform);
		string text = NormalizeLibraryName(libraryName, currentPlatform);
		lock (_mutex)
		{
			if (_loadedLibraries.TryGetValue(text, out var value))
			{
				logger?.WriteLine("Native library \"{0}\" has already been prepared; nothing to do", libraryName);
				return value;
			}
			if (currentPlatform.OS == PlatformOperatingSystem.Android)
			{
				logger?.WriteLine("Probing for " + text + " within local Android .apk");
				if (ProbeLibrary(text, currentPlatform, logger) != NativeLibraryStatus.Available)
				{
					logger?.WriteLine("Native library \"{0}\" was not found in the local .apk.", libraryName);
					return NativeLibraryStatus.Unavailable;
				}
				return NativeLibraryStatus.Available;
			}
			logger?.WriteLine("Probing for an already existing " + text);
			NativeLibraryStatus nativeLibraryStatus = ProbeLibrary(text, currentPlatform, logger);
			if (nativeLibraryStatus == NativeLibraryStatus.Available)
			{
				logger?.WriteLine("Native library \"{0}\" resolved to an already-existing library. Loading current file as-is.", libraryName);
				_loadedLibraries[text] = nativeLibraryStatus;
				return nativeLibraryStatus;
			}
			DeleteLocalLibraryIfPresent(text, logger);
			string path = Path.Combine(Environment.CurrentDirectory, "runtimes");
			List<string> list = PermuteLibraryNames(libraryName, currentPlatform);
			foreach (string item in PermuteArchitectureSpecificDirectoryNames(currentPlatform))
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(path, item, "native"));
				if (!directoryInfo.Exists)
				{
					continue;
				}
				foreach (string item2 in list)
				{
					FileInfo fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, item2));
					if (!fileInfo.Exists)
					{
						continue;
					}
					if (currentPlatform.OS == PlatformOperatingSystem.Windows || currentPlatform.OS == PlatformOperatingSystem.Linux || currentPlatform.OS == PlatformOperatingSystem.MacOS)
					{
						FileInfo fileInfo2 = new FileInfo(Path.Combine(Environment.CurrentDirectory, text));
						try
						{
							logger?.WriteLine("Resolved native library \"" + libraryName + "\" to " + fileInfo.FullName);
							fileInfo.CopyTo(fileInfo2.FullName);
							_loadedLibraries[text] = NativeLibraryStatus.Available;
							return NativeLibraryStatus.Available;
						}
						catch (Exception ex)
						{
							logger?.WriteLine(ex.Message);
							logger?.WriteLine("Could not prepare native library \"" + libraryName + "\" (is the existing library file locked or in use?)");
							_loadedLibraries[text] = NativeLibraryStatus.Unknown;
							return NativeLibraryStatus.Unknown;
						}
					}
					throw new PlatformNotSupportedException($"Don't know yet how to load libraries for {currentPlatform.OS}");
				}
			}
			logger?.WriteLine("Failed to resolve native library \"{0}\".", libraryName);
			_loadedLibraries[text] = NativeLibraryStatus.Unavailable;
			return NativeLibraryStatus.Unavailable;
		}
	}

	internal static string GetRuntimeIdString(this PlatformArchitecture architecture)
	{
		return architecture switch
		{
			PlatformArchitecture.Unknown => "unknown", 
			PlatformArchitecture.Any => "any", 
			PlatformArchitecture.I386 => "x86", 
			PlatformArchitecture.X64 => "x64", 
			PlatformArchitecture.ArmV7 => "arm", 
			PlatformArchitecture.Arm64 => "arm64", 
			PlatformArchitecture.Armel => "armel", 
			PlatformArchitecture.ArmV6 => "armv6", 
			PlatformArchitecture.Mips64 => "mips64", 
			PlatformArchitecture.PowerPC64 => "ppc64le", 
			PlatformArchitecture.RiscFive => "riscv64", 
			PlatformArchitecture.S390x => "s390x", 
			PlatformArchitecture.Loongarch64 => "loongarch64", 
			PlatformArchitecture.Itanium64 => "ia64", 
			_ => throw new PlatformNotSupportedException("No runtime ID defined for " + architecture), 
		};
	}

	internal static string GetRuntimeIdString(this PlatformOperatingSystem os)
	{
		return os switch
		{
			PlatformOperatingSystem.Unknown => "unknown", 
			PlatformOperatingSystem.Any => "any", 
			PlatformOperatingSystem.Windows => "win", 
			PlatformOperatingSystem.Linux => "linux", 
			PlatformOperatingSystem.MacOS => "osx", 
			PlatformOperatingSystem.iOS => "ios", 
			PlatformOperatingSystem.iOS_Simulator => "iossimulator", 
			PlatformOperatingSystem.Android => "android", 
			PlatformOperatingSystem.FreeBSD => "freebsd", 
			PlatformOperatingSystem.Illumos => "illumos", 
			PlatformOperatingSystem.Linux_Bionic => "linux-bionic", 
			PlatformOperatingSystem.Linux_Musl => "linux-musl", 
			PlatformOperatingSystem.MacCatalyst => "maccatalyst", 
			PlatformOperatingSystem.Solaris => "solaris", 
			PlatformOperatingSystem.TvOS => "tvos", 
			PlatformOperatingSystem.TvOS_Simulator => "tvossimulator", 
			PlatformOperatingSystem.Unix => "unix", 
			PlatformOperatingSystem.Browser => "browser", 
			PlatformOperatingSystem.Wasi => "wasi", 
			_ => throw new PlatformNotSupportedException("No runtime ID defined for " + os), 
		};
	}

	internal static string[] GetInheritedRuntimeIds(string runtimeId)
	{
		if (RidInheritanceMappings.TryGetValue(runtimeId, out var value))
		{
			return value;
		}
		return new string[0];
	}

	internal static OSAndArchitecture ParseRuntimeId(ReadOnlySpan<char> runtimeId)
	{
		int num = runtimeId.IndexOf('-');
		if (num < 0)
		{
			return new OSAndArchitecture(TryParseOperatingSystemString(runtimeId), PlatformArchitecture.Unknown);
		}
		return new OSAndArchitecture(TryParseOperatingSystemString(runtimeId.Slice(0, num)), TryParseArchitectureString(runtimeId.Slice(num + 1)));
	}

	internal static PlatformOperatingSystem TryParseOperatingSystemString(string os)
	{
		return TryParseOperatingSystemString(MemoryExtensions.AsSpan(os));
	}

	internal static PlatformOperatingSystem TryParseOperatingSystemString(ReadOnlySpan<char> os)
	{
		if (os.Length >= "win".Length && MemoryExtensions.Equals(os.Slice(0, "win".Length), MemoryExtensions.AsSpan("win"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Windows;
		}
		if (os.Length >= "linux".Length && MemoryExtensions.Equals(os.Slice(0, "linux".Length), MemoryExtensions.AsSpan("linux"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Linux;
		}
		if (os.Length >= "ubuntu".Length && MemoryExtensions.Equals(os.Slice(0, "ubuntu".Length), MemoryExtensions.AsSpan("ubuntu"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Linux;
		}
		if (os.Length >= "debian".Length && MemoryExtensions.Equals(os.Slice(0, "debian".Length), MemoryExtensions.AsSpan("debian"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Linux;
		}
		if (os.Length >= "osx".Length && MemoryExtensions.Equals(os.Slice(0, "osx".Length), MemoryExtensions.AsSpan("osx"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.MacOS;
		}
		if (os.Length >= "ios".Length && MemoryExtensions.Equals(os.Slice(0, "ios".Length), MemoryExtensions.AsSpan("ios"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.iOS;
		}
		if (os.Length >= "iossimulator".Length && MemoryExtensions.Equals(os.Slice(0, "iossimulator".Length), MemoryExtensions.AsSpan("iossimulator"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.iOS_Simulator;
		}
		if (os.Length >= "android".Length && MemoryExtensions.Equals(os.Slice(0, "android".Length), MemoryExtensions.AsSpan("android"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Android;
		}
		if (os.Length >= "freebsd".Length && MemoryExtensions.Equals(os.Slice(0, "freebsd".Length), MemoryExtensions.AsSpan("freebsd"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.FreeBSD;
		}
		if (os.Length >= "illumos".Length && MemoryExtensions.Equals(os.Slice(0, "illumos".Length), MemoryExtensions.AsSpan("illumos"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Illumos;
		}
		if (os.Length >= "linux-bionic".Length && MemoryExtensions.Equals(os.Slice(0, "linux-bionic".Length), MemoryExtensions.AsSpan("linux-bionic"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Linux_Bionic;
		}
		if (os.Length >= "linux-musl".Length && MemoryExtensions.Equals(os.Slice(0, "linux-musl".Length), MemoryExtensions.AsSpan("linux-musl"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Linux_Musl;
		}
		if (os.Length >= "maccatalyst".Length && MemoryExtensions.Equals(os.Slice(0, "maccatalyst".Length), MemoryExtensions.AsSpan("maccatalyst"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.MacCatalyst;
		}
		if (os.Length >= "solaris".Length && MemoryExtensions.Equals(os.Slice(0, "solaris".Length), MemoryExtensions.AsSpan("solaris"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Solaris;
		}
		if (os.Length >= "tvos".Length && MemoryExtensions.Equals(os.Slice(0, "tvos".Length), MemoryExtensions.AsSpan("tvos"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.TvOS;
		}
		if (os.Length >= "tvossimulator".Length && MemoryExtensions.Equals(os.Slice(0, "tvossimulator".Length), MemoryExtensions.AsSpan("tvossimulator"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.TvOS_Simulator;
		}
		if (os.Length >= "unix".Length && MemoryExtensions.Equals(os.Slice(0, "unix".Length), MemoryExtensions.AsSpan("unix"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Unix;
		}
		if (os.Length >= "browser".Length && MemoryExtensions.Equals(os.Slice(0, "browser".Length), MemoryExtensions.AsSpan("browser"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Browser;
		}
		if (os.Length >= "wasi".Length && MemoryExtensions.Equals(os.Slice(0, "wasi".Length), MemoryExtensions.AsSpan("wasi"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformOperatingSystem.Wasi;
		}
		return PlatformOperatingSystem.Unknown;
	}

	internal static PlatformArchitecture TryParseArchitectureString(string arch)
	{
		return TryParseArchitectureString(MemoryExtensions.AsSpan(arch));
	}

	internal static PlatformArchitecture TryParseArchitectureString(ReadOnlySpan<char> arch)
	{
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("any"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Any;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("x86"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.I386;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("x64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.X64;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("arm"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.ArmV7;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("arm64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Arm64;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("armel"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Armel;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("armv6"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.ArmV6;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("mips64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Mips64;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("ppc64le"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.PowerPC64;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("riscv64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.RiscFive;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("s390x"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.S390x;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("loongarch64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Loongarch64;
		}
		if (MemoryExtensions.Equals(arch, MemoryExtensions.AsSpan("ia64"), StringComparison.OrdinalIgnoreCase))
		{
			return PlatformArchitecture.Itanium64;
		}
		return PlatformArchitecture.Unknown;
	}

	internal static PlatformArchitecture TryGetNativeArchitecture(PlatformOperatingSystem os, TextWriter logger)
	{
		switch (os)
		{
		case PlatformOperatingSystem.Windows:
		{
			KernelInteropWindows.SYSTEM_INFO Info = default(KernelInteropWindows.SYSTEM_INFO);
			KernelInteropWindows.GetSystemInfo(ref Info);
			switch (Info.wProcessorArchitecture)
			{
			case 0:
			case 9:
				if (Marshal.SizeOf((IntPtr)0) != 4)
				{
					return PlatformArchitecture.X64;
				}
				return PlatformArchitecture.I386;
			case 5:
				return PlatformArchitecture.ArmV7;
			case 18:
				return PlatformArchitecture.Arm64;
			case 6:
				return PlatformArchitecture.Itanium64;
			default:
				return PlatformArchitecture.Unknown;
			}
		}
		case PlatformOperatingSystem.Linux:
		case PlatformOperatingSystem.Android:
		case PlatformOperatingSystem.Linux_Bionic:
		case PlatformOperatingSystem.Linux_Musl:
		case PlatformOperatingSystem.Unix:
		{
			PlatformArchitecture? platformArchitecture = KernelInteropLinux.TryGetArchForUnix(logger);
			if (platformArchitecture.HasValue)
			{
				return platformArchitecture.Value;
			}
			break;
		}
		}
		return PlatformArchitecture.Unknown;
	}

	private static void DeleteLocalLibraryIfPresent(string normalizedLibraryName, TextWriter logger)
	{
		FileInfo fileInfo = new FileInfo(Path.Combine(Environment.CurrentDirectory, normalizedLibraryName));
		if (fileInfo.Exists)
		{
			try
			{
				logger?.WriteLine("Clobbering existing file " + fileInfo.FullName);
				fileInfo.Delete();
			}
			catch (Exception)
			{
				logger?.WriteLine("Failed to clean up \"" + fileInfo.FullName + "\" (is it locked or in use?)");
			}
		}
	}

	private static List<string> PermuteArchitectureSpecificDirectoryNames(OSAndArchitecture platformInfo)
	{
		string text = platformInfo.OS.GetRuntimeIdString() + "-" + platformInfo.Architecture.GetRuntimeIdString();
		string[] inheritedRuntimeIds = GetInheritedRuntimeIds(text);
		List<string> list = new List<string>(inheritedRuntimeIds.Length + 1);
		list.Add(text);
		if (platformInfo.OS == PlatformOperatingSystem.Windows)
		{
			list.Add("win10-" + platformInfo.Architecture.GetRuntimeIdString());
			list.Add("win81-" + platformInfo.Architecture.GetRuntimeIdString());
			list.Add("win8-" + platformInfo.Architecture.GetRuntimeIdString());
			list.Add("win7-" + platformInfo.Architecture.GetRuntimeIdString());
			list.Add("win10");
			list.Add("win81");
			list.Add("win8");
			list.Add("win7");
		}
		list.AddRange(inheritedRuntimeIds);
		return list;
	}

	private static string LibraryNameWithoutExtension(string libraryName)
	{
		if (!libraryName.Contains("."))
		{
			return libraryName;
		}
		string text = libraryName.ToLowerInvariant();
		if (text.EndsWith(".dll") || text.EndsWith(".so") || text.EndsWith(".dylib"))
		{
			return libraryName.Substring(0, libraryName.LastIndexOf('.'));
		}
		return libraryName;
	}

	private static string NormalizeLibraryName(string requestedName, OSAndArchitecture platformInfo)
	{
		string text = LibraryNameWithoutExtension(requestedName);
		if (platformInfo.OS == PlatformOperatingSystem.Windows)
		{
			return text + ".dll";
		}
		if (platformInfo.OS == PlatformOperatingSystem.Linux || platformInfo.OS == PlatformOperatingSystem.Android || platformInfo.OS == PlatformOperatingSystem.Linux_Bionic || platformInfo.OS == PlatformOperatingSystem.Linux_Musl || platformInfo.OS == PlatformOperatingSystem.Unix)
		{
			if (!text.StartsWith("lib", StringComparison.Ordinal))
			{
				return "lib" + text + ".so";
			}
			return text + ".so";
		}
		if (platformInfo.OS == PlatformOperatingSystem.iOS || platformInfo.OS == PlatformOperatingSystem.iOS_Simulator || platformInfo.OS == PlatformOperatingSystem.MacOS || platformInfo.OS == PlatformOperatingSystem.MacCatalyst)
		{
			if (!text.StartsWith("lib", StringComparison.Ordinal))
			{
				return "lib" + text + ".dylib";
			}
			return text + ".dylib";
		}
		return requestedName;
	}

	private static List<string> PermuteLibraryNames(string requestedName, OSAndArchitecture platformInfo)
	{
		List<string> list = new List<string>(16);
		string text = LibraryNameWithoutExtension(requestedName);
		if (platformInfo.OS == PlatformOperatingSystem.Windows)
		{
			list.Add(text + ".dll");
			list.Add("lib" + text + ".dll");
			if (platformInfo.Architecture == PlatformArchitecture.I386)
			{
				list.Add(text + "_x86.dll");
				list.Add("lib" + text + "_x86.dll");
				list.Add(text + "x86.dll");
				list.Add("lib" + text + "x86.dll");
				list.Add(text + "32.dll");
				list.Add("lib" + text + "32.dll");
				list.Add(text + "_32.dll");
				list.Add("lib" + text + "_32.dll");
				list.Add(text + "-32.dll");
				list.Add("lib" + text + "-32.dll");
			}
			if (platformInfo.Architecture == PlatformArchitecture.X64)
			{
				list.Add(text + "_x64.dll");
				list.Add("lib" + text + "_x64.dll");
				list.Add(text + "x64.dll");
				list.Add("lib" + text + "x64.dll");
				list.Add(text + "64.dll");
				list.Add("lib" + text + "64.dll");
				list.Add(text + "_64.dll");
				list.Add("lib" + text + "_64.dll");
				list.Add(text + "-64.dll");
				list.Add("lib" + text + "-64.dll");
			}
		}
		else if (platformInfo.OS == PlatformOperatingSystem.Linux || platformInfo.OS == PlatformOperatingSystem.Android || platformInfo.OS == PlatformOperatingSystem.Linux_Bionic || platformInfo.OS == PlatformOperatingSystem.Linux_Musl || platformInfo.OS == PlatformOperatingSystem.Unix)
		{
			list.Add(text + ".so");
			list.Add("lib" + text + ".so");
		}
		else if (platformInfo.OS == PlatformOperatingSystem.MacOS || platformInfo.OS == PlatformOperatingSystem.MacCatalyst || platformInfo.OS == PlatformOperatingSystem.iOS || platformInfo.OS == PlatformOperatingSystem.iOS_Simulator)
		{
			list.Add(text + ".dylib");
			list.Add("lib" + text + ".dylib");
		}
		else
		{
			list.Add(requestedName);
		}
		return list;
	}

	internal static NativeLibraryStatus ProbeLibrary(string libName, OSAndArchitecture platformInfo, TextWriter logger)
	{
		try
		{
			if (platformInfo.OS == PlatformOperatingSystem.Windows)
			{
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					logger?.WriteLine("Attempting to load " + libName + " as a windows .dll");
					KernelInteropWindows.GetLastError();
					intPtr = KernelInteropWindows.LoadLibraryExW(libName, IntPtr.Zero, 4096u);
					if (intPtr == IntPtr.Zero)
					{
						uint lastError = KernelInteropWindows.GetLastError();
						if (lastError != 0)
						{
							uint num = 0x80070000u | lastError;
							switch (num)
							{
							case 2147942526u:
								logger?.WriteLine($"Win32 error 0x{num:X8}: File {libName} not found");
								break;
							case 2147942593u:
								logger?.WriteLine($"Win32 error 0x{num:X8}: Invalid binary format while loading library {libName}");
								break;
							default:
								logger?.WriteLine($"Win32 error 0x{num:X8} while loading library {libName}");
								break;
							}
						}
						else
						{
							logger?.WriteLine("Native library " + libName + " not found.");
						}
						return NativeLibraryStatus.Unavailable;
					}
					logger?.WriteLine("Native library " + libName + " found!");
					return NativeLibraryStatus.Available;
				}
				finally
				{
					if (intPtr != IntPtr.Zero)
					{
						KernelInteropWindows.FreeLibrary(intPtr);
					}
				}
			}
			if (platformInfo.OS == PlatformOperatingSystem.Linux || platformInfo.OS == PlatformOperatingSystem.Android || platformInfo.OS == PlatformOperatingSystem.Linux_Bionic || platformInfo.OS == PlatformOperatingSystem.Linux_Musl || platformInfo.OS == PlatformOperatingSystem.Unix)
			{
				IntPtr intPtr2 = IntPtr.Zero;
				try
				{
					logger?.WriteLine("Attempting to load " + libName + " as a linux .so");
					intPtr2 = KernelInteropLinux.dlopen(libName, 2);
					if (intPtr2 == IntPtr.Zero)
					{
						IntPtr intPtr3 = KernelInteropLinux.dlerror();
						if (intPtr3 != IntPtr.Zero)
						{
							string text = Marshal.PtrToStringAnsi(intPtr3);
							if (!string.IsNullOrEmpty(text))
							{
								logger?.WriteLine($"Error while loading library {libName}: {text}");
							}
							else
							{
								logger?.WriteLine(libName + " could not be loaded.");
							}
						}
						else
						{
							logger?.WriteLine(libName + " could not be loaded.");
						}
						return NativeLibraryStatus.Unavailable;
					}
					logger?.WriteLine("Native library " + libName + " found!");
					return NativeLibraryStatus.Available;
				}
				finally
				{
					if (intPtr2 != IntPtr.Zero)
					{
						KernelInteropLinux.dlclose(intPtr2);
					}
				}
			}
			if (platformInfo.OS == PlatformOperatingSystem.MacOS || platformInfo.OS == PlatformOperatingSystem.MacCatalyst || platformInfo.OS == PlatformOperatingSystem.iOS || platformInfo.OS == PlatformOperatingSystem.iOS_Simulator)
			{
				IntPtr intPtr4 = IntPtr.Zero;
				try
				{
					logger?.WriteLine("Attempting to load " + libName + " as a macOS .dylib");
					intPtr4 = KernelInteropMacOS.dlopen(libName, 2);
					if (intPtr4 == IntPtr.Zero)
					{
						IntPtr intPtr5 = KernelInteropMacOS.dlerror();
						if (intPtr5 != IntPtr.Zero)
						{
							string text2 = Marshal.PtrToStringAnsi(intPtr5);
							if (!string.IsNullOrEmpty(text2))
							{
								logger?.WriteLine($"Error while loading library {libName}: {text2}");
							}
							else
							{
								logger?.WriteLine(libName + " could not be loaded.");
							}
						}
						else
						{
							logger?.WriteLine(libName + " could not be loaded.");
						}
						return NativeLibraryStatus.Unavailable;
					}
					logger?.WriteLine("Native library " + libName + " found!");
					return NativeLibraryStatus.Available;
				}
				finally
				{
					if (intPtr4 != IntPtr.Zero)
					{
						KernelInteropMacOS.dlclose(intPtr4);
					}
				}
			}
		}
		catch (Exception value)
		{
			logger?.WriteLine(value);
		}
		return NativeLibraryStatus.Unknown;
	}
}

}