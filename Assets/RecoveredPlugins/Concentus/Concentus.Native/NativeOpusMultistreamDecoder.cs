using System;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Microsoft.Win32.SafeHandles;

namespace Concentus.Native {
internal class NativeOpusMultistreamDecoder : SafeHandleZeroOrMinusOneIsInvalid, IOpusMultiStreamDecoder, IDisposable
{
	private int _sampleRate;

	private int _numChannels;

	private NativeOpusMultistreamDecoder NativeHandle => this;

	public int SampleRate => _sampleRate;

	public int NumChannels => _numChannels;

	public OpusBandwidth Bandwidth
	{
		get
		{
			NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4009, out var value);
			return (OpusBandwidth)value;
		}
	}

	public uint FinalRange
	{
		get
		{
			NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4031, out var value);
			return (uint)value;
		}
	}

	public int Gain
	{
		get
		{
			NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4045, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4034, value);
		}
	}

	public int LastPacketDuration
	{
		get
		{
			NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4039, out var value);
			return value;
		}
	}

	internal NativeOpusMultistreamDecoder()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		NativeOpus.opus_multistream_decoder_destroy(handle);
		return true;
	}

	public unsafe static NativeOpusMultistreamDecoder Create(int sampleRate, int channelCount, int streams, int coupledStreams, byte[] channelMapping)
	{
		fixed (byte* mapping = channelMapping)
		{
			int error;
			NativeOpusMultistreamDecoder nativeOpusMultistreamDecoder = NativeOpus.opus_multistream_decoder_create(sampleRate, channelCount, streams, coupledStreams, mapping, out error);
			if (error != 0)
			{
				nativeOpusMultistreamDecoder.Dispose();
				throw new Exception($"Failed to create opus MS decoder: error {error}");
			}
			nativeOpusMultistreamDecoder._sampleRate = sampleRate;
			nativeOpusMultistreamDecoder._numChannels = channelCount;
			return nativeOpusMultistreamDecoder;
		}
	}

	public unsafe int DecodeMultistream(ReadOnlySpan<byte> data, Span<float> out_pcm, int frame_size, bool decode_fec)
	{
		fixed (float* pcm = out_pcm)
		{
			if (data.Length == 0)
			{
				return NativeOpus.opus_multistream_decode_float(NativeHandle, null, 0, pcm, frame_size, decode_fec ? 1 : 0);
			}
			fixed (byte* data2 = data)
			{
				return NativeOpus.opus_multistream_decode_float(NativeHandle, data2, data.Length, pcm, frame_size, decode_fec ? 1 : 0);
			}
		}
	}

	public unsafe int DecodeMultistream(ReadOnlySpan<byte> data, Span<short> out_pcm, int frame_size, bool decode_fec)
	{
		fixed (short* pcm = out_pcm)
		{
			if (data.Length == 0)
			{
				return NativeOpus.opus_multistream_decode(NativeHandle, null, 0, pcm, frame_size, decode_fec ? 1 : 0);
			}
			fixed (byte* data2 = data)
			{
				return NativeOpus.opus_multistream_decode(NativeHandle, data2, data.Length, pcm, frame_size, decode_fec ? 1 : 0);
			}
		}
	}

	public void ResetState()
	{
		NativeOpus.opus_multistream_decoder_ctl(NativeHandle, 4028, 0);
	}

	public string GetVersionString()
	{
		return Marshal.PtrToStringAnsi(NativeOpus.opus_get_version_string());
	}
}

}