using System;
using System.Runtime.InteropServices;

namespace Concentus.Native {
internal class KernelInteropWindows
{
	internal struct SYSTEM_INFO
	{
		internal ushort wProcessorArchitecture;

		internal ushort wReserved;

		internal uint dwPageSize;

		internal IntPtr lpMinimumApplicationAddress;

		internal IntPtr lpMaximumApplicationAddress;

		internal IntPtr dwActiveProcessorMask;

		internal uint dwNumberOfProcessors;

		internal uint dwProcessorType;

		internal uint dwAllocationGranularity;

		internal ushort wProcessorLevel;

		internal ushort wProcessorRevision;
	}

	internal const uint LOAD_LIBRARY_AS_DATAFILE = 2u;

	internal const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 4096u;

	internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;

	internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;

	internal const ushort PROCESSOR_ARCHITECTURE_ARM = 5;

	internal const ushort PROCESSOR_ARCHITECTURE_ARM64 = 18;

	internal const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;

	internal const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = ushort.MaxValue;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool FreeLibrary(IntPtr hModule);

	[DllImport("kernel32.dll")]
	internal static extern uint GetLastError();

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern void GetSystemInfo(ref SYSTEM_INFO Info);
}

}