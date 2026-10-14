using System;
using Concentus.Celt;
using Concentus.Celt.Structs;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Enums;

namespace Concentus.Structs {
public class OpusMSEncoder : IOpusMultiStreamEncoder, IDisposable
{
	internal delegate void opus_copy_channel_in_func<T>(Span<short> dst, int dst_ptr, int dst_stride, ReadOnlySpan<T> src, int src_stride, int src_channel, int frame_size);

	internal readonly ChannelLayout layout = new ChannelLayout();

	internal int lfe_stream;

	internal OpusApplication application = OpusApplication.OPUS_APPLICATION_AUDIO;

	internal OpusFramesize variable_duration;

	internal int surround;

	internal int bitrate_bps;

	internal readonly float[] subframe_mem = new float[3];

	internal readonly OpusEncoder[] encoders;

	internal readonly int[] window_mem;

	internal readonly int[] preemph_mem;

	private static readonly int[] diff_table = new int[17]
	{
		512, 300, 165, 87, 45, 23, 11, 6, 3, 0,
		0, 0, 0, 0, 0, 0, 0
	};

	private const int MS_FRAME_TMP = 3832;

	public int Bitrate
	{
		get
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < layout.nb_streams; i++)
			{
				OpusEncoder opusEncoder = encoders[num2++];
				num += opusEncoder.Bitrate;
			}
			return num;
		}
		set
		{
			if (value < 0 && value != -1000 && value != -1)
			{
				throw new ArgumentException("Invalid bitrate");
			}
			bitrate_bps = value;
		}
	}

	public OpusApplication Application
	{
		get
		{
			return encoders[0].Application;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].Application = value;
			}
		}
	}

	public int ForceChannels
	{
		get
		{
			return encoders[0].ForceChannels;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].ForceChannels = value;
			}
		}
	}

	public int NumChannels => layout.nb_channels;

	public OpusBandwidth MaxBandwidth
	{
		get
		{
			return encoders[0].MaxBandwidth;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].MaxBandwidth = value;
			}
		}
	}

	public OpusBandwidth Bandwidth
	{
		get
		{
			return encoders[0].Bandwidth;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].Bandwidth = value;
			}
		}
	}

	public bool UseDTX
	{
		get
		{
			return encoders[0].UseDTX;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].UseDTX = value;
			}
		}
	}

	public int Complexity
	{
		get
		{
			return encoders[0].Complexity;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].Complexity = value;
			}
		}
	}

	public OpusMode ForceMode
	{
		get
		{
			return encoders[0].ForceMode;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].ForceMode = value;
			}
		}
	}

	public bool UseInbandFEC
	{
		get
		{
			return encoders[0].UseInbandFEC;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].UseInbandFEC = value;
			}
		}
	}

	public int PacketLossPercent
	{
		get
		{
			return encoders[0].PacketLossPercent;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].PacketLossPercent = value;
			}
		}
	}

	public bool UseVBR
	{
		get
		{
			return encoders[0].UseVBR;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].UseVBR = value;
			}
		}
	}

	public bool UseConstrainedVBR
	{
		get
		{
			return encoders[0].UseConstrainedVBR;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].UseConstrainedVBR = value;
			}
		}
	}

	public OpusSignal SignalType
	{
		get
		{
			return encoders[0].SignalType;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].SignalType = value;
			}
		}
	}

	public int Lookahead => encoders[0].Lookahead;

	public int SampleRate => encoders[0].SampleRate;

	public uint FinalRange
	{
		get
		{
			uint num = 0u;
			int num2 = 0;
			for (int i = 0; i < layout.nb_streams; i++)
			{
				num ^= encoders[num2++].FinalRange;
			}
			return num;
		}
	}

	public int LSBDepth
	{
		get
		{
			return encoders[0].LSBDepth;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].LSBDepth = value;
			}
		}
	}

	public bool PredictionDisabled
	{
		get
		{
			return encoders[0].PredictionDisabled;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				encoders[i].PredictionDisabled = value;
			}
		}
	}

	public OpusFramesize ExpertFrameDuration
	{
		get
		{
			return variable_duration;
		}
		set
		{
			variable_duration = value;
		}
	}

	private OpusMSEncoder(int nb_streams, int nb_coupled_streams)
	{
		if (nb_streams < 1 || nb_coupled_streams > nb_streams || nb_coupled_streams < 0)
		{
			throw new ArgumentException("Invalid channel count in MS encoder");
		}
		encoders = new OpusEncoder[nb_streams];
		for (int i = 0; i < nb_streams; i++)
		{
			encoders[i] = new OpusEncoder();
		}
		int num = nb_coupled_streams * 2 + (nb_streams - nb_coupled_streams);
		window_mem = new int[num * 120];
		preemph_mem = new int[num];
	}

	public void ResetState()
	{
		subframe_mem[0] = (subframe_mem[1] = (subframe_mem[2] = 0f));
		if (surround != 0)
		{
			Arrays.MemSetInt(preemph_mem, 0, layout.nb_channels);
			Arrays.MemSetInt(window_mem, 0, layout.nb_channels * 120);
		}
		int num = 0;
		for (int i = 0; i < layout.nb_streams; i++)
		{
			encoders[num++].ResetState();
		}
	}

	internal static int validate_encoder_layout(ChannelLayout layout)
	{
		for (int i = 0; i < layout.nb_streams; i++)
		{
			if (i < layout.nb_coupled_streams)
			{
				if (OpusMultistream.get_left_channel(layout, i, -1) == -1)
				{
					return 0;
				}
				if (OpusMultistream.get_right_channel(layout, i, -1) == -1)
				{
					return 0;
				}
			}
			else if (OpusMultistream.get_mono_channel(layout, i, -1) == -1)
			{
				return 0;
			}
		}
		return 1;
	}

	internal static void channel_pos(int channels, int[] pos)
	{
		switch (channels)
		{
		case 4:
			pos[0] = 1;
			pos[1] = 3;
			pos[2] = 1;
			pos[3] = 3;
			break;
		case 3:
		case 5:
		case 6:
			pos[0] = 1;
			pos[1] = 2;
			pos[2] = 3;
			pos[3] = 1;
			pos[4] = 3;
			pos[5] = 0;
			break;
		case 7:
			pos[0] = 1;
			pos[1] = 2;
			pos[2] = 3;
			pos[3] = 1;
			pos[4] = 3;
			pos[5] = 2;
			pos[6] = 0;
			break;
		case 8:
			pos[0] = 1;
			pos[1] = 2;
			pos[2] = 3;
			pos[3] = 1;
			pos[4] = 3;
			pos[5] = 1;
			pos[6] = 3;
			pos[7] = 0;
			break;
		}
	}

	internal static int logSum(int a, int b)
	{
		int num;
		int num2;
		if (a > b)
		{
			num = a;
			num2 = Inlines.SUB32(Inlines.EXTEND32(a), Inlines.EXTEND32(b));
		}
		else
		{
			num = b;
			num2 = Inlines.SUB32(Inlines.EXTEND32(b), Inlines.EXTEND32(a));
		}
		if (num2 >= 8192)
		{
			return num;
		}
		int num3 = Inlines.SHR32(num2, 9);
		int a2 = Inlines.SHL16(num2 - Inlines.SHL16(num3, 9), 6);
		return num + diff_table[num3] + Inlines.MULT16_16_Q15(a2, Inlines.SUB16(diff_table[num3 + 1], diff_table[num3]));
	}

	internal static void surround_analysis<T>(CeltMode celt_mode, ReadOnlySpan<T> pcm, int[] bandLogE, int[] mem, int[] preemph_mem, int len, int overlap, int channels, int rate, opus_copy_channel_in_func<T> copy_channel_in)
	{
		int[] array = new int[8];
		int[][] array2 = Arrays.InitTwoDimensionalArray<int>(1, 21);
		int[][] array3 = Arrays.InitTwoDimensionalArray<int>(3, 21);
		int num = CeltCommon.resampling_factor(rate);
		int num2 = len * num;
		int i;
		for (i = 0; i < celt_mode.maxLM && celt_mode.shortMdctSize << i != num2; i++)
		{
		}
		int[] array4 = new int[num2 + overlap];
		short[] array5 = new short[len];
		int[][] array6 = Arrays.InitTwoDimensionalArray<int>(1, num2);
		channel_pos(channels, array);
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 21; k++)
			{
				array3[j][k] = -28672;
			}
		}
		for (int j = 0; j < channels; j++)
		{
			Arrays.MemCopy(mem, j * overlap, array4, 0, overlap);
			copy_channel_in(array5, 0, 1, pcm, channels, j, len);
			BoxedValueInt boxedValueInt = new BoxedValueInt(preemph_mem[j]);
			CeltCommon.celt_preemphasis(array5, array4, overlap, num2, 1, num, celt_mode.preemph, boxedValueInt, 0);
			preemph_mem[j] = boxedValueInt.Val;
			MDCT.clt_mdct_forward(celt_mode.mdct, array4, 0, array6[0], 0, celt_mode.window, overlap, celt_mode.maxLM - i, 1);
			if (num != 1)
			{
				int k;
				for (k = 0; k < len; k++)
				{
					array6[0][k] *= num;
				}
				for (; k < num2; k++)
				{
					array6[0][k] = 0;
				}
			}
			Bands.compute_band_energies(celt_mode, array6, array2, 21, 1, i);
			QuantizeBands.amp2Log2(celt_mode, 21, 21, array2[0], bandLogE, 21 * j, 1);
			for (int k = 1; k < 21; k++)
			{
				bandLogE[21 * j + k] = Inlines.MAX16(bandLogE[21 * j + k], bandLogE[21 * j + k - 1] - 1024);
			}
			for (int k = 19; k >= 0; k--)
			{
				bandLogE[21 * j + k] = Inlines.MAX16(bandLogE[21 * j + k], bandLogE[21 * j + k + 1] - 2048);
			}
			if (array[j] == 1)
			{
				for (int k = 0; k < 21; k++)
				{
					array3[0][k] = logSum(array3[0][k], bandLogE[21 * j + k]);
				}
			}
			else if (array[j] == 3)
			{
				for (int k = 0; k < 21; k++)
				{
					array3[2][k] = logSum(array3[2][k], bandLogE[21 * j + k]);
				}
			}
			else if (array[j] == 2)
			{
				for (int k = 0; k < 21; k++)
				{
					array3[0][k] = logSum(array3[0][k], bandLogE[21 * j + k] - 512);
					array3[2][k] = logSum(array3[2][k], bandLogE[21 * j + k] - 512);
				}
			}
			Arrays.MemCopy(array4, num2, mem, j * overlap, overlap);
		}
		for (int k = 0; k < 21; k++)
		{
			array3[1][k] = Inlines.MIN32(array3[0][k], array3[2][k]);
		}
		int num3 = Inlines.HALF16(Inlines.celt_log2(32768 / (channels - 1)));
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 21; k++)
			{
				array3[j][k] += num3;
			}
		}
		for (int j = 0; j < channels; j++)
		{
			if (array[j] != 0)
			{
				int[] array7 = array3[array[j] - 1];
				for (int k = 0; k < 21; k++)
				{
					bandLogE[21 * j + k] -= array7[k];
				}
			}
			else
			{
				for (int k = 0; k < 21; k++)
				{
					bandLogE[21 * j + k] = 0;
				}
			}
		}
	}

	internal int opus_multistream_encoder_init(int Fs, int channels, int streams, int coupled_streams, byte[] mapping, OpusApplication application, int surround)
	{
		if (channels > 255 || channels < 1 || coupled_streams > streams || streams < 1 || coupled_streams < 0 || streams > 255 - coupled_streams)
		{
			return -1;
		}
		layout.nb_channels = channels;
		layout.nb_streams = streams;
		layout.nb_coupled_streams = coupled_streams;
		subframe_mem[0] = (subframe_mem[1] = (subframe_mem[2] = 0f));
		if (surround == 0)
		{
			lfe_stream = -1;
		}
		bitrate_bps = -1000;
		this.application = application;
		variable_duration = OpusFramesize.OPUS_FRAMESIZE_ARG;
		int i;
		for (i = 0; i < layout.nb_channels; i++)
		{
			layout.mapping[i] = mapping[i];
		}
		if (OpusMultistream.validate_layout(layout) == 0 || validate_encoder_layout(layout) == 0)
		{
			return -1;
		}
		int num = 0;
		for (i = 0; i < layout.nb_coupled_streams; i++)
		{
			int num2 = encoders[num].opus_init_encoder(Fs, 2, application);
			if (num2 != 0)
			{
				return num2;
			}
			if (i == lfe_stream)
			{
				encoders[num].IsLFE = true;
			}
			num++;
		}
		for (; i < layout.nb_streams; i++)
		{
			int num2 = encoders[num].opus_init_encoder(Fs, 1, application);
			if (i == lfe_stream)
			{
				encoders[num].IsLFE = true;
			}
			if (num2 != 0)
			{
				return num2;
			}
			num++;
		}
		if (surround != 0)
		{
			Arrays.MemSetInt(preemph_mem, 0, channels);
			Arrays.MemSetInt(window_mem, 0, channels * 120);
		}
		this.surround = surround;
		return 0;
	}

	internal int opus_multistream_surround_encoder_init(int Fs, int channels, int mapping_family, out int streams, out int coupled_streams, byte[] mapping, OpusApplication application)
	{
		streams = 0;
		coupled_streams = 0;
		if (channels > 255 || channels < 1)
		{
			return -1;
		}
		lfe_stream = -1;
		if (mapping_family == 0)
		{
			switch (channels)
			{
			case 1:
				streams = 1;
				coupled_streams = 0;
				mapping[0] = 0;
				break;
			case 2:
				streams = 1;
				coupled_streams = 1;
				mapping[0] = 0;
				mapping[1] = 1;
				break;
			default:
				return -5;
			}
		}
		else if (mapping_family == 1 && channels <= 8 && channels >= 1)
		{
			streams = VorbisLayout.vorbis_mappings[channels - 1].nb_streams;
			coupled_streams = VorbisLayout.vorbis_mappings[channels - 1].nb_coupled_streams;
			for (int i = 0; i < channels; i++)
			{
				mapping[i] = VorbisLayout.vorbis_mappings[channels - 1].mapping[i];
			}
			if (channels >= 6)
			{
				lfe_stream = streams - 1;
			}
		}
		else
		{
			if (mapping_family != 255)
			{
				return -5;
			}
			streams = channels;
			coupled_streams = 0;
			for (byte b = 0; b < channels; b++)
			{
				mapping[b] = b;
			}
		}
		return opus_multistream_encoder_init(Fs, channels, streams, coupled_streams, mapping, application, (channels > 2 && mapping_family == 1) ? 1 : 0);
	}

	[Obsolete("Use OpusCodecFactory methods which can give you native code if supported by your platform")]
	public static OpusMSEncoder Create(int Fs, int channels, int streams, int coupled_streams, byte[] mapping, OpusApplication application)
	{
		if (channels > 255 || channels < 1 || coupled_streams > streams || streams < 1 || coupled_streams < 0 || streams > 255 - coupled_streams)
		{
			throw new ArgumentException("Invalid channel / stream configuration");
		}
		OpusMSEncoder opusMSEncoder = new OpusMSEncoder(streams, coupled_streams);
		int num = opusMSEncoder.opus_multistream_encoder_init(Fs, channels, streams, coupled_streams, mapping, application, 0);
		return num switch
		{
			-1 => throw new ArgumentException("OPUS_BAD_ARG when creating MS encoder"), 
			0 => opusMSEncoder, 
			_ => throw new OpusException("Could not create MS encoder: " + CodecHelpers.opus_strerror(num), num), 
		};
	}

	internal static void GetStreamCount(int channels, int mapping_family, BoxedValueInt nb_streams, BoxedValueInt nb_coupled_streams)
	{
		switch (mapping_family)
		{
		case 0:
			switch (channels)
			{
			case 1:
				nb_streams.Val = 1;
				nb_coupled_streams.Val = 0;
				break;
			case 2:
				nb_streams.Val = 1;
				nb_coupled_streams.Val = 1;
				break;
			default:
				throw new ArgumentException("More than 2 channels requires custom mappings");
			}
			return;
		case 1:
			if (channels <= 8 && channels >= 1)
			{
				nb_streams.Val = VorbisLayout.vorbis_mappings[channels - 1].nb_streams;
				nb_coupled_streams.Val = VorbisLayout.vorbis_mappings[channels - 1].nb_coupled_streams;
				return;
			}
			break;
		}
		if (mapping_family == 255)
		{
			nb_streams.Val = channels;
			nb_coupled_streams.Val = 0;
			return;
		}
		throw new ArgumentException("Invalid mapping family");
	}

	[Obsolete("Use OpusCodecFactory methods which can give you native code if supported by your platform")]
	public static OpusMSEncoder CreateSurround(int Fs, int channels, int mapping_family, out int streams, out int coupled_streams, byte[] mapping, OpusApplication application)
	{
		if (channels > 255 || channels < 1 || application == OpusApplication.OPUS_APPLICATION_UNIMPLEMENTED)
		{
			throw new ArgumentException("Invalid channel count or application");
		}
		BoxedValueInt boxedValueInt = new BoxedValueInt();
		BoxedValueInt boxedValueInt2 = new BoxedValueInt();
		GetStreamCount(channels, mapping_family, boxedValueInt, boxedValueInt2);
		OpusMSEncoder opusMSEncoder = new OpusMSEncoder(boxedValueInt.Val, boxedValueInt2.Val);
		int num = opusMSEncoder.opus_multistream_surround_encoder_init(Fs, channels, mapping_family, out streams, out coupled_streams, mapping, application);
		return num switch
		{
			-1 => throw new ArgumentException("Bad argument passed to CreateSurround"), 
			0 => opusMSEncoder, 
			_ => throw new OpusException("Could not create multistream encoder: " + CodecHelpers.opus_strerror(num), num), 
		};
	}

	internal int surround_rate_allocation(int[] out_rates, int frame_size)
	{
		int num = 0;
		int sampleRate = encoders[0].SampleRate;
		int num2 = ((bitrate_bps <= layout.nb_channels * 40000) ? (bitrate_bps / layout.nb_channels / 2) : 20000);
		num2 += 60 * (sampleRate / frame_size - 50);
		int num3 = 3500 + 60 * (sampleRate / frame_size - 50);
		int num4 = 512;
		int num5 = 32;
		int num6;
		if (bitrate_bps == -1000)
		{
			num6 = sampleRate + 60 * sampleRate / frame_size;
		}
		else if (bitrate_bps == -1)
		{
			num6 = 300000;
		}
		else
		{
			int num7 = ((lfe_stream != -1) ? 1 : 0);
			int nb_coupled_streams = layout.nb_coupled_streams;
			int num8 = layout.nb_streams - nb_coupled_streams - num7;
			int num9 = (num8 << 8) + num4 * nb_coupled_streams + num7 * num5;
			num6 = 256 * (bitrate_bps - num3 * num7 - num2 * (nb_coupled_streams + num8)) / num9;
		}
		for (int i = 0; i < layout.nb_streams; i++)
		{
			if (i < layout.nb_coupled_streams)
			{
				out_rates[i] = num2 + (num6 * num4 >> 8);
			}
			else if (i != lfe_stream)
			{
				out_rates[i] = num2 + num6;
			}
			else
			{
				out_rates[i] = num3 + (num6 * num5 >> 8);
			}
			out_rates[i] = Inlines.IMAX(out_rates[i], 500);
			num += out_rates[i];
		}
		return num;
	}

	internal int opus_multistream_encode_native<T>(opus_copy_channel_in_func<T> copy_channel_in, ReadOnlySpan<T> pcm, int analysis_frame_size, Span<byte> data, int max_data_bytes, int lsb_depth, Downmix.downmix_func<T> downmix, int float_api)
	{
		byte[] array = new byte[3832];
		OpusRepacketizer opusRepacketizer = new OpusRepacketizer();
		int[] array2 = new int[256];
		int[] array3 = new int[42];
		int[] mem = null;
		int[] array4 = null;
		int num = 0;
		if (surround != 0)
		{
			array4 = preemph_mem;
			mem = window_mem;
		}
		int num2 = 0;
		int sampleRate = encoders[num2].SampleRate;
		int num3 = (encoders[num2].UseVBR ? 1 : 0);
		CeltMode celtMode = encoders[num2].GetCeltMode();
		int c = layout.nb_streams + layout.nb_coupled_streams;
		int lookahead = encoders[num2].Lookahead;
		lookahead -= sampleRate / 400;
		int num4 = CodecHelpers.compute_frame_size(pcm, analysis_frame_size, variable_duration, c, sampleRate, bitrate_bps, lookahead, downmix, subframe_mem, encoders[num2].analysis.enabled);
		if (400 * num4 < sampleRate)
		{
			return -1;
		}
		if (400 * num4 != sampleRate && 200 * num4 != sampleRate && 100 * num4 != sampleRate && 50 * num4 != sampleRate && 25 * num4 != sampleRate && 50 * num4 != 3 * sampleRate)
		{
			return -1;
		}
		int num5 = layout.nb_streams * 2 - 1;
		if (max_data_bytes < num5)
		{
			return -2;
		}
		short[] array5 = new short[2 * num4];
		int[] array6 = new int[21 * layout.nb_channels];
		if (surround != 0)
		{
			surround_analysis(celtMode, pcm, array6, mem, array4, num4, 120, layout.nb_channels, sampleRate, copy_channel_in);
		}
		int num6 = surround_rate_allocation(array2, num4);
		if (num3 == 0)
		{
			if (bitrate_bps == -1000)
			{
				max_data_bytes = Inlines.IMIN(max_data_bytes, 3 * num6 / (24 * sampleRate / num4));
			}
			else if (bitrate_bps != -1)
			{
				max_data_bytes = Inlines.IMIN(max_data_bytes, Inlines.IMAX(num5, 3 * bitrate_bps / (24 * sampleRate / num4)));
			}
		}
		for (int i = 0; i < layout.nb_streams; i++)
		{
			OpusEncoder opusEncoder = encoders[num2];
			num2++;
			opusEncoder.Bitrate = array2[i];
			if (surround != 0)
			{
				int num7 = bitrate_bps;
				if (num4 * 50 < sampleRate)
				{
					num7 -= 60 * (sampleRate / num4 - 50) * layout.nb_channels;
				}
				if (num7 > 10000 * layout.nb_channels)
				{
					opusEncoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
				}
				else if (num7 > 7000 * layout.nb_channels)
				{
					opusEncoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND;
				}
				else if (num7 > 5000 * layout.nb_channels)
				{
					opusEncoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
				}
				else
				{
					opusEncoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
				}
				if (i < layout.nb_coupled_streams)
				{
					opusEncoder.ForceMode = OpusMode.MODE_CELT_ONLY;
					opusEncoder.ForceChannels = 2;
				}
			}
		}
		num2 = 0;
		int num8 = 0;
		for (int i = 0; i < layout.nb_streams; i++)
		{
			opusRepacketizer.Reset();
			OpusEncoder opusEncoder2 = encoders[num2];
			int c2;
			int c3;
			if (i < layout.nb_coupled_streams)
			{
				int num9 = OpusMultistream.get_left_channel(layout, i, -1);
				int num10 = OpusMultistream.get_right_channel(layout, i, -1);
				copy_channel_in(array5, 0, 2, pcm, layout.nb_channels, num9, num4);
				copy_channel_in(array5, 1, 2, pcm, layout.nb_channels, num10, num4);
				num2++;
				if (surround != 0)
				{
					for (int j = 0; j < 21; j++)
					{
						array3[j] = array6[21 * num9 + j];
						array3[21 + j] = array6[21 * num10 + j];
					}
				}
				c2 = num9;
				c3 = num10;
			}
			else
			{
				int num11 = OpusMultistream.get_mono_channel(layout, i, -1);
				copy_channel_in(array5, 0, 1, pcm, layout.nb_channels, num11, num4);
				num2++;
				if (surround != 0)
				{
					for (int k = 0; k < 21; k++)
					{
						array3[k] = array6[21 * num11 + k];
					}
				}
				c2 = num11;
				c3 = -1;
			}
			if (surround != 0)
			{
				opusEncoder2.SetEnergyMask(array3);
			}
			int num12 = max_data_bytes - num8;
			num12 -= Inlines.IMAX(0, 2 * (layout.nb_streams - i - 1) - 1);
			num12 = Inlines.IMIN(num12, 3832);
			if (i != layout.nb_streams - 1)
			{
				num12 -= ((num12 <= 253) ? 1 : 2);
			}
			if (num3 == 0 && i == layout.nb_streams - 1)
			{
				opusEncoder2.Bitrate = num12 * (8 * sampleRate / num4);
			}
			int num13 = opusEncoder2.opus_encode_native(array5, 0, num4, array, 0, num12, lsb_depth, pcm, analysis_frame_size, c2, c3, layout.nb_channels, downmix, float_api);
			if (num13 < 0)
			{
				return num13;
			}
			opusRepacketizer.AddPacket(array, 0, num13);
			num13 = opusRepacketizer.opus_repacketizer_out_range_impl(0, opusRepacketizer.GetNumFrames(), data, num, max_data_bytes - num8, (i != layout.nb_streams - 1) ? 1 : 0, (num3 == 0 && i == layout.nb_streams - 1) ? 1 : 0);
			num += num13;
			num8 += num13;
		}
		return num8;
	}

	internal static void opus_copy_channel_in_float(Span<short> dst, int dst_offset, int dst_stride, ReadOnlySpan<float> src, int src_stride, int src_channel, int frame_size)
	{
		for (int i = 0; i < frame_size; i++)
		{
			dst[i * dst_stride + dst_offset] = Inlines.FLOAT2INT16(src[i * src_stride + src_channel]);
		}
	}

	internal static void opus_copy_channel_in_short(Span<short> dst, int dst_offset, int dst_stride, ReadOnlySpan<short> src, int src_stride, int src_channel, int frame_size)
	{
		for (int i = 0; i < frame_size; i++)
		{
			dst[i * dst_stride + dst_offset] = src[i * src_stride + src_channel];
		}
	}

	[Obsolete("Use Span<> overrides if possible")]
	public int EncodeMultistream(short[] pcm, int pcm_offset, int frame_size, byte[] outputBuffer, int outputBuffer_offset, int max_data_bytes)
	{
		return EncodeMultistream(MemoryExtensions.AsSpan(pcm, pcm_offset), frame_size, MemoryExtensions.AsSpan(outputBuffer, outputBuffer_offset), max_data_bytes);
	}

	public int EncodeMultistream(ReadOnlySpan<short> pcm, int frame_size, Span<byte> outputBuffer, int max_data_bytes)
	{
		int num = opus_multistream_encode_native(opus_copy_channel_in_short, pcm, frame_size, outputBuffer, max_data_bytes, 16, Downmix.downmix_int, 0);
		if (num < 0)
		{
			if (num == -1)
			{
				throw new ArgumentException("OPUS_BAD_ARG while encoding");
			}
			throw new OpusException("An error occurred during encoding: " + CodecHelpers.opus_strerror(num), num);
		}
		return num;
	}

	[Obsolete("Use Span<> overrides if possible")]
	public int EncodeMultistream(float[] pcm, int pcm_offset, int frame_size, byte[] outputBuffer, int outputBuffer_offset, int max_data_bytes)
	{
		return EncodeMultistream(MemoryExtensions.AsSpan(pcm, pcm_offset), frame_size, MemoryExtensions.AsSpan(outputBuffer, outputBuffer_offset), max_data_bytes);
	}

	public int EncodeMultistream(ReadOnlySpan<float> pcm, int frame_size, Span<byte> outputBuffer, int max_data_bytes)
	{
		int num = opus_multistream_encode_native(opus_copy_channel_in_float, pcm, frame_size, outputBuffer, max_data_bytes, 16, Downmix.downmix_float, 1);
		if (num < 0)
		{
			if (num == -1)
			{
				throw new ArgumentException("OPUS_BAD_ARG while encoding");
			}
			throw new OpusException("An error occurred during encoding: " + CodecHelpers.opus_strerror(num), num);
		}
		return num;
	}

	public OpusEncoder GetMultistreamEncoderState(int streamId)
	{
		if (streamId >= layout.nb_streams)
		{
			throw new ArgumentException("Requested stream doesn't exist");
		}
		return encoders[streamId];
	}

	public string GetVersionString()
	{
		return CodecHelpers.GetVersionString();
	}

	public void Dispose()
	{
	}
}

}