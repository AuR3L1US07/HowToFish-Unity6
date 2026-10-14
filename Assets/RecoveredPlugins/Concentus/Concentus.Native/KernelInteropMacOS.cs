using System;
using System.Runtime.InteropServices;

namespace Concentus.Native {
internal class KernelInteropMacOS
{
	internal const int RTLD_NOW = 2;

	[DllImport("libSystem.dylib")]
	internal static extern IntPtr dlopen(string fileName, int flags);

	[DllImport("libSystem.dylib")]
	internal static extern int dlclose(IntPtr handle);

	[DllImport("libSystem.dylib")]
	internal static extern IntPtr dlerror();
}

}