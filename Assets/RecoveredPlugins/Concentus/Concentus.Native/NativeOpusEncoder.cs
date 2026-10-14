using System;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Microsoft.Win32.SafeHandles;

namespace Concentus.Native {
internal class NativeOpusEncoder : SafeHandleZeroOrMinusOneIsInvalid, IOpusEncoder, IDisposable
{
	private int _sampleRate;

	private int _numChannels;

	private NativeOpusEncoder NativeHandle => this;

	public int SampleRate => _sampleRate;

	public int NumChannels => _numChannels;

	public int Complexity
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4011, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4010, value);
		}
	}

	public bool UseDTX
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4017, out var value);
			return value != 0;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4016, value ? 1 : 0);
		}
	}

	public int Bitrate
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4003, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4002, value);
		}
	}

	public OpusMode ForceMode
	{
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 11002, (int)value);
		}
	}

	public bool UseVBR
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4007, out var value);
			return value != 0;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4006, value ? 1 : 0);
		}
	}

	public OpusApplication Application
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4001, out var value);
			return (OpusApplication)value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4000, (int)value);
		}
	}

	public int ForceChannels
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4023, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4022, value);
		}
	}

	public OpusBandwidth MaxBandwidth
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4005, out var value);
			return (OpusBandwidth)value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4004, (int)value);
		}
	}

	public OpusBandwidth Bandwidth
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4009, out var value);
			return (OpusBandwidth)value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4008, (int)value);
		}
	}

	public bool UseInbandFEC
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4013, out var value);
			return value != 0;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4012, value ? 1 : 0);
		}
	}

	public int PacketLossPercent
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4015, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4014, value);
		}
	}

	public bool UseConstrainedVBR
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4021, out var value);
			return value != 0;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4020, value ? 1 : 0);
		}
	}

	public OpusSignal SignalType
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4025, out var value);
			return (OpusSignal)value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4024, (int)value);
		}
	}

	public int Lookahead
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4027, out var value);
			return value;
		}
	}

	public uint FinalRange
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4031, out var value);
			return (uint)value;
		}
	}

	public int LSBDepth
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4037, out var value);
			return value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4036, value);
		}
	}

	public OpusFramesize ExpertFrameDuration
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4041, out var value);
			return (OpusFramesize)value;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4040, (int)value);
		}
	}

	public bool PredictionDisabled
	{
		get
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4043, out var value);
			return value != 0;
		}
		set
		{
			NativeOpus.opus_encoder_ctl(NativeHandle, 4042, value ? 1 : 0);
		}
	}

	internal NativeOpusEncoder()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		NativeOpus.opus_encoder_destroy(handle);
		return true;
	}

	public static NativeOpusEncoder Create(int sampleRate, int channelCount, OpusApplication application)
	{
		int error;
		NativeOpusEncoder nativeOpusEncoder = NativeOpus.opus_encoder_create(sampleRate, channelCount, (int)application, out error);
		if (error != 0)
		{
			nativeOpusEncoder.Dispose();
			throw new Exception($"Failed to create opus encoder: error {error}");
		}
		nativeOpusEncoder._sampleRate = sampleRate;
		nativeOpusEncoder._numChannels = channelCount;
		return nativeOpusEncoder;
	}

	public unsafe int Encode(ReadOnlySpan<short> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes)
	{
		fixed (short* pcm = in_pcm)
		{
			fixed (byte* data = out_data)
			{
				return NativeOpus.opus_encode(NativeHandle, pcm, frame_size, data, max_data_bytes);
			}
		}
	}

	public unsafe int Encode(ReadOnlySpan<float> in_pcm, int frame_size, Span<byte> out_data, int max_data_bytes)
	{
		fixed (float* pcm = in_pcm)
		{
			fixed (byte* data = out_data)
			{
				return NativeOpus.opus_encode_float(NativeHandle, pcm, frame_size, data, max_data_bytes);
			}
		}
	}

	public void ResetState()
	{
		NativeOpus.opus_encoder_ctl(NativeHandle, 4028, 0);
	}

	public string GetVersionString()
	{
		return Marshal.PtrToStringAnsi(NativeOpus.opus_get_version_string());
	}
}

}