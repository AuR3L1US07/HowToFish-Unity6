using System;
using Concentus.Enums;

namespace Concentus {
public interface IOpusEncoder : IDisposable
{
	OpusApplication Application { get; set; }

	int Bitrate { get; set; }

	int ForceChannels { get; set; }

	OpusBandwidth MaxBandwidth { get; set; }

	OpusBandwidth Bandwidth { get; set; }

	bool UseDTX { get; set; }

	int Complexity { get; set; }

	bool UseInbandFEC { get; set; }

	int PacketLossPercent { get; set; }

	bool UseVBR { get; set; }

	bool UseConstrainedVBR { get; set; }

	OpusSignal SignalType { get; set; }

	int Lookahead { get; }

	int SampleRate { get; }

	int NumChannels { get; }

	uint FinalRange { get; }

	int LSBDepth { get; set; }

	OpusFramesize ExpertFrameDuration { get; set; }

	OpusMode ForceMode { set; }

	bool PredictionDisabled { get; set; }

	int Encode(ReadOnlySpan<short> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes);

	int Encode(ReadOnlySpan<float> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes);

	void ResetState();

	string GetVersionString();
}

}