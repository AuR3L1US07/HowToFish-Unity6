using System;
using System.Runtime.InteropServices;

namespace Adrenak.RNNoise4Unity {
public static class Native
{
	public const string LIBRARY_NAME = "rnnoise";

	public const int FRAME_SIZE = 480;

	public const float SIGNAL_SCALE = 32767f;

	public const float SIGNAL_SCALE_INV = 3.051851E-05f;

	[DllImport("rnnoise")]
	public static extern int rnnoise_get_size();

	[DllImport("rnnoise")]
	public static extern int rnnoise_init(IntPtr state, IntPtr model);

	[DllImport("rnnoise")]
	public static extern IntPtr rnnoise_create(IntPtr model);

	[DllImport("rnnoise")]
	public static extern void rnnoise_destroy(IntPtr state);

	[DllImport("rnnoise")]
	public unsafe static extern float rnnoise_process_frame(IntPtr state, float* dataOut, float* dataIn);
}

}