using System;
using Concentus.Enums;

namespace Concentus {
public interface IOpusDecoder : IDisposable
{
	OpusBandwidth Bandwidth { get; }

	uint FinalRange { get; }

	int Gain { get; set; }

	int LastPacketDuration { get; }

	int NumChannels { get; }

	int Pitch { get; }

	int SampleRate { get; }

	int Decode(ReadOnlySpan<byte> in_data, Span<float> out_pcm, int frame_size, bool decode_fec = false);

	int Decode(ReadOnlySpan<byte> in_data, Span<short> out_pcm, int frame_size, bool decode_fec = false);

	void ResetState();

	string GetVersionString();
}

}