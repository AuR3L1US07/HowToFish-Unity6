using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Concentus.Native {
internal class KernelInteropLinux
{
	private interface ILibDl
	{
		IntPtr DLOpen(string fileName, int flags);

		int DLClose(IntPtr handle);

		IntPtr DLError();
	}

	private class LibDL : ILibDl
	{
		public IntPtr DLOpen(string fileName, int flags)
		{
			return dlopen(fileName, flags);
		}

		public int DLClose(IntPtr handle)
		{
			return dlclose(handle);
		}

		public IntPtr DLError()
		{
			return dlerror();
		}

		[DllImport("libdl.so")]
		private static extern IntPtr dlopen(string fileName, int flags);

		[DllImport("libdl.so")]
		private static extern int dlclose(IntPtr handle);

		[DllImport("libdl.so")]
		private static extern IntPtr dlerror();
	}

	private class LibDL2 : ILibDl
	{
		public IntPtr DLOpen(string fileName, int flags)
		{
			return dlopen(fileName, flags);
		}

		public int DLClose(IntPtr handle)
		{
			return dlclose(handle);
		}

		public IntPtr DLError()
		{
			return dlerror();
		}

		[DllImport("libdl.so.2")]
		private static extern IntPtr dlopen(string fileName, int flags);

		[DllImport("libdl.so.2")]
		private static extern int dlclose(IntPtr handle);

		[DllImport("libdl.so.2")]
		private static extern IntPtr dlerror();
	}

	internal const int RTLD_NOW = 2;

	private static Lazy<ILibDl> _libDlImpl = new Lazy<ILibDl>(GetLibDL, LazyThreadSafetyMode.ExecutionAndPublication);

	internal static readonly IReadOnlyDictionary<string, PlatformArchitecture> POSSIBLE_UNIX_MACHINES = new Dictionary<string, PlatformArchitecture>(StringComparer.OrdinalIgnoreCase)
	{
		{
			"amd64",
			PlatformArchitecture.X64
		},
		{
			"x86_64",
			PlatformArchitecture.X64
		},
		{
			"i686-64",
			PlatformArchitecture.X64
		},
		{
			"x86",
			PlatformArchitecture.I386
		},
		{
			"i686",
			PlatformArchitecture.I386
		},
		{
			"i686-AT386",
			PlatformArchitecture.I386
		},
		{
			"i386",
			PlatformArchitecture.I386
		},
		{
			"x86pc",
			PlatformArchitecture.I386
		},
		{
			"i86pc",
			PlatformArchitecture.I386
		},
		{
			"armv6l",
			PlatformArchitecture.ArmV6
		},
		{
			"armv7l",
			PlatformArchitecture.ArmV7
		},
		{
			"arm64",
			PlatformArchitecture.Arm64
		},
		{
			"aarch64",
			PlatformArchitecture.Arm64
		},
		{
			"aarch64_be",
			PlatformArchitecture.Arm64
		},
		{
			"armv8l",
			PlatformArchitecture.Arm64
		},
		{
			"armv8b",
			PlatformArchitecture.Arm64
		},
		{
			"mips64",
			PlatformArchitecture.Mips64
		},
		{
			"ppc64",
			PlatformArchitecture.PowerPC64
		},
		{
			"ppc64le",
			PlatformArchitecture.PowerPC64
		}
	};

	private static ILibDl GetLibDL()
	{
		try
		{
			LibDL libDL = new LibDL();
			libDL.DLError();
			return libDL;
		}
		catch (Exception)
		{
			return new LibDL2();
		}
	}

	public static IntPtr dlopen(string fileName, int flags)
	{
		return _libDlImpl.Value.DLOpen(fileName, flags);
	}

	public static int dlclose(IntPtr handle)
	{
		return _libDlImpl.Value.DLClose(handle);
	}

	public static IntPtr dlerror()
	{
		return _libDlImpl.Value.DLError();
	}

	internal static PlatformArchitecture? TryGetArchForUnix(TextWriter logger)
	{
		try
		{
			logger?.WriteLine("Running uname to determine system info...");
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "uname",
				Arguments = "-m",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			using StreamReader streamReader = process.StandardOutput;
			string text = streamReader.ReadToEnd();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			text = text.Trim();
			if (POSSIBLE_UNIX_MACHINES.TryGetValue(text, out var value))
			{
				logger?.WriteLine("Got architecture from uname: " + text);
				return value;
			}
		}
		catch (Exception value2)
		{
			logger?.WriteLine(value2);
		}
		return null;
	}
}

}