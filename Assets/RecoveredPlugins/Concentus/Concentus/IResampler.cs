using System;

namespace Concentus {
public interface IResampler : IDisposable
{
	int InputLatency { get; }

	int InputStride { get; set; }

	int OutputLatencySamples { get; }

	TimeSpan OutputLatency { get; }

	int OutputStride { get; set; }

	int Quality { get; set; }

	void GetRateFraction(out int ratio_num, out int ratio_den);

	void GetRates(out int in_rate, out int out_rate);

	void ResetMem();

	void SkipZeroes();

	void Process(int channel_index, Span<float> input, ref int in_len, Span<float> output, ref int out_len);

	void Process(int channel_index, Span<short> input, ref int in_len, Span<short> output, ref int out_len);

	void ProcessInterleaved(Span<float> input, ref int in_len, Span<float> output, ref int out_len);

	void ProcessInterleaved(Span<short> input, ref int in_len, Span<short> output, ref int out_len);

	void SetRateFraction(int ratio_num, int ratio_den, int in_rate, int out_rate);

	void SetRates(int in_rate, int out_rate);
}

}