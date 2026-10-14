using System;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Microsoft.Win32.SafeHandles;

namespace Concentus.Native {
internal class NativeOpusDecoder : SafeHandleZeroOrMinusOneIsInvalid, IOpusDecoder, IDisposable
{
	private int _sampleRate;

	private int _numChannels;

	private NativeOpusDecoder NativeHandle => this;

	public int SampleRate => _sampleRate;

	public int NumChannels => _numChannels;

	public OpusBandwidth Bandwidth
	{
		get
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4009, out var value);
			return (OpusBandwidth)value;
		}
	}

	public uint FinalRange
	{
		get
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4031, out var value);
			return (uint)value;
		}
	}

	public int Gain
	{
		get
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4045, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4034, value);
		}
	}

	public int LastPacketDuration
	{
		get
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4039, out var value);
			return value;
		}
	}

	public int Pitch
	{
		get
		{
			NativeOpus.opus_decoder_ctl(NativeHandle, 4033, out var value);
			return value;
		}
	}

	internal NativeOpusDecoder()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		NativeOpus.opus_encoder_destroy(handle);
		return true;
	}

	public static NativeOpusDecoder Create(int sampleRate, int channelCount)
	{
		int error;
		NativeOpusDecoder nativeOpusDecoder = NativeOpus.opus_decoder_create(sampleRate, channelCount, out error);
		if (error != 0)
		{
			nativeOpusDecoder.Dispose();
			throw new Exception($"Failed to create opus decoder: error {error}");
		}
		nativeOpusDecoder._sampleRate = sampleRate;
		nativeOpusDecoder._numChannels = channelCount;
		return nativeOpusDecoder;
	}

	public unsafe int Decode(ReadOnlySpan<byte> in_data, Span<float> out_pcm, int frame_size, bool decode_fec = false)
	{
		fixed (float* pcm = out_pcm)
		{
			if (in_data.Length == 0)
			{
				return NativeOpus.opus_decode_float(NativeHandle, null, 0, pcm, frame_size, decode_fec ? 1 : 0);
			}
			fixed (byte* data = in_data)
			{
				return NativeOpus.opus_decode_float(NativeHandle, data, in_data.Length, pcm, frame_size, decode_fec ? 1 : 0);
			}
		}
	}

	public unsafe int Decode(ReadOnlySpan<byte> in_data, Span<short> out_pcm, int frame_size, bool decode_fec = false)
	{
		fixed (short* pcm = out_pcm)
		{
			if (in_data.Length == 0)
			{
				return NativeOpus.opus_decode(NativeHandle, null, 0, pcm, frame_size, decode_fec ? 1 : 0);
			}
			fixed (byte* data = in_data)
			{
				return NativeOpus.opus_decode(NativeHandle, data, in_data.Length, pcm, frame_size, decode_fec ? 1 : 0);
			}
		}
	}

	public void ResetState()
	{
		NativeOpus.opus_decoder_ctl(NativeHandle, 4028, 0);
	}

	public string GetVersionString()
	{
		return Marshal.PtrToStringAnsi(NativeOpus.opus_get_version_string());
	}
}

}