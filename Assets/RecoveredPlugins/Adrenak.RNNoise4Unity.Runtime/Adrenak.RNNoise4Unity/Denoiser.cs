using System;

namespace Adrenak.RNNoise4Unity {
public class Denoiser : IDisposable
{
	private IntPtr state;

	private float[] processingBuffer;

	private int processingBufferDataStart;

	private float[] processedData;

	private int processedDataRemaining;

	public Denoiser()
	{
		state = Native.rnnoise_create(IntPtr.Zero);
		processingBuffer = new float[480];
		processedData = new float[480];
	}

	public unsafe int Denoise(Span<float> buffer, bool finish = false)
	{
		int num = 0;
		fixed (float* dataIn = &processingBuffer[0])
		{
			fixed (float* ptr = buffer)
			{
				while (buffer.Length > 0 || processingBufferDataStart == 480)
				{
					if (processedDataRemaining > 0)
					{
						Span<float> span = buffer;
						if (span.Length > processedDataRemaining)
						{
							span = span.Slice(0, processedDataRemaining);
						}
						Span<float> span2 = MemoryExtensions.AsSpan(processingBuffer);
						span.CopyTo(span2.Slice(processingBufferDataStart));
						processingBufferDataStart += span.Length;
						span2 = MemoryExtensions.AsSpan(processedData);
						Span<float> span3 = span2.Slice(processedData.Length - processedDataRemaining);
						if (span3.Length > buffer.Length)
						{
							span3 = span3.Slice(0, buffer.Length);
						}
						span3.CopyTo(buffer);
						buffer = buffer.Slice(span3.Length);
						processedDataRemaining -= span3.Length;
						num += span3.Length;
					}
					if (processingBufferDataStart > 0 || buffer.Length < 480)
					{
						Span<float> destination = MemoryExtensions.AsSpan(processingBuffer);
						destination = destination.Slice(processingBufferDataStart);
						Span<float> span4 = buffer;
						if (span4.Length > destination.Length)
						{
							span4 = span4.Slice(0, destination.Length);
						}
						span4.CopyTo(destination);
						processingBufferDataStart += span4.Length;
						destination = destination.Slice(span4.Length);
						if (destination.Length == 0 || finish)
						{
							if (destination.Length > 0)
							{
								destination.Fill(0f);
							}
							for (int i = 0; i < 480; i++)
							{
								processingBuffer[i] *= 32767f;
							}
							fixed (float* dataOut = &processedData[0])
							{
								Native.rnnoise_process_frame(state, dataOut, dataIn);
							}
							for (int j = 0; j < 480; j++)
							{
								processedData[j] *= 3.051851E-05f;
							}
							processedDataRemaining = 480;
							Span<float> span5 = MemoryExtensions.AsSpan(processedData);
							if (span5.Length > span4.Length)
							{
								span5 = span5.Slice(0, span4.Length);
							}
							span5.CopyTo(buffer);
							num += span4.Length;
							if (finish)
							{
								processedDataRemaining = 0;
							}
							else
							{
								processedDataRemaining -= span5.Length;
							}
							processingBufferDataStart = 0;
						}
						buffer = buffer.Slice(span4.Length);
					}
					else
					{
						for (int k = 0; k < 480; k++)
						{
							buffer[k] *= 32767f;
						}
						Native.rnnoise_process_frame(state, ptr + num, ptr + num);
						for (int l = 0; l < 480; l++)
						{
							buffer[l] *= 3.051851E-05f;
						}
						buffer = buffer.Slice(480);
						num += 480;
					}
				}
			}
		}
		return num;
	}

	public void Dispose()
	{
		if (state != IntPtr.Zero)
		{
			Native.rnnoise_destroy(state);
			state = IntPtr.Zero;
		}
		processingBuffer = null;
	}
}

}