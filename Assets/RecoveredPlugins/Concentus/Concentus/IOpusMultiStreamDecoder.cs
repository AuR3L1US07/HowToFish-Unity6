using System;
using Concentus.Enums;

namespace Concentus {
public interface IOpusMultiStreamDecoder : IDisposable
{
	OpusBandwidth Bandwidth { get; }

	uint FinalRange { get; }

	int Gain { get; set; }

	int LastPacketDuration { get; }

	int SampleRate { get; }

	int NumChannels { get; }

	int DecodeMultistream(ReadOnlySpan<byte> data, Span<float> out_pcm, int frame_size, bool decode_fec);

	int DecodeMultistream(ReadOnlySpan<byte> data, Span<short> out_pcm, int frame_size, bool decode_fec);

	void ResetState();

	string GetVersionString();
}

}