using System;
using Concentus.Enums;

namespace Concentus {
public interface IOpusMultiStreamEncoder : IDisposable
{
	OpusApplication Application { get; set; }

	OpusBandwidth Bandwidth { get; set; }

	int Bitrate { get; set; }

	int Complexity { get; set; }

	int NumChannels { get; }

	OpusFramesize ExpertFrameDuration { get; set; }

	uint FinalRange { get; }

	OpusMode ForceMode { set; }

	int Lookahead { get; }

	int LSBDepth { get; set; }

	OpusBandwidth MaxBandwidth { get; set; }

	int PacketLossPercent { get; set; }

	bool PredictionDisabled { get; set; }

	int SampleRate { get; }

	OpusSignal SignalType { get; set; }

	bool UseConstrainedVBR { get; set; }

	bool UseDTX { get; set; }

	bool UseInbandFEC { get; set; }

	bool UseVBR { get; set; }

	int EncodeMultistream(ReadOnlySpan<float> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes);

	int EncodeMultistream(ReadOnlySpan<short> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes);

	void ResetState();

	string GetVersionString();
}

}