using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;

namespace Concentus.Celt.Structs {
internal class CeltDecoder
{
	internal CeltMode mode;

	internal int overlap;

	internal int channels;

	internal int stream_channels;

	internal int downsample;

	internal int start;

	internal int end;

	internal int signalling;

	internal uint rng;

	internal int error;

	internal int last_pitch_index;

	internal int loss_count;

	internal int postfilter_period;

	internal int postfilter_period_old;

	internal int postfilter_gain;

	internal int postfilter_gain_old;

	internal int postfilter_tapset;

	internal int postfilter_tapset_old;

	internal readonly int[] preemph_memD = new int[2];

	internal int[][] decode_mem;

	internal int[][] lpc;

	internal int[] oldEBands;

	internal int[] oldLogE;

	internal int[] oldLogE2;

	internal int[] backgroundLogE;

	private void Reset()
	{
		mode = null;
		overlap = 0;
		channels = 0;
		stream_channels = 0;
		downsample = 0;
		start = 0;
		end = 0;
		signalling = 0;
		PartialReset();
	}

	private void PartialReset()
	{
		rng = 0u;
		error = 0;
		last_pitch_index = 0;
		loss_count = 0;
		postfilter_period = 0;
		postfilter_period_old = 0;
		postfilter_gain = 0;
		postfilter_gain_old = 0;
		postfilter_tapset = 0;
		postfilter_tapset_old = 0;
		Arrays.MemSetInt(preemph_memD, 0, 2);
		decode_mem = null;
		lpc = null;
		oldEBands = null;
		oldLogE = null;
		oldLogE2 = null;
		backgroundLogE = null;
	}

	internal void ResetState()
	{
		PartialReset();
		decode_mem = new int[channels][];
		lpc = new int[channels][];
		for (int i = 0; i < channels; i++)
		{
			decode_mem[i] = new int[2048 + mode.overlap];
			lpc[i] = new int[24];
		}
		oldEBands = new int[2 * mode.nbEBands];
		oldLogE = new int[2 * mode.nbEBands];
		oldLogE2 = new int[2 * mode.nbEBands];
		backgroundLogE = new int[2 * mode.nbEBands];
		for (int j = 0; j < 2 * mode.nbEBands; j++)
		{
			oldLogE[j] = (oldLogE2[j] = -28672);
		}
	}

	internal int celt_decoder_init(int sampling_rate, int channels)
	{
		int num = opus_custom_decoder_init(CeltMode.mode48000_960_120, channels);
		if (num != 0)
		{
			return num;
		}
		downsample = CeltCommon.resampling_factor(sampling_rate);
		if (downsample == 0)
		{
			return -1;
		}
		return 0;
	}

	private int opus_custom_decoder_init(CeltMode mode, int channels)
	{
		if (channels < 0 || channels > 2)
		{
			return -1;
		}
		if (this == null)
		{
			return -7;
		}
		Reset();
		this.mode = mode;
		overlap = mode.overlap;
		stream_channels = (this.channels = channels);
		downsample = 1;
		start = 0;
		end = this.mode.effEBands;
		signalling = 1;
		loss_count = 0;
		ResetState();
		return 0;
	}

	internal void celt_decode_lost(int N, int LM)
	{
		int num = channels;
		int[][] array = new int[2][];
		int[] array2 = new int[2];
		CeltMode celtMode = mode;
		int nbEBands = celtMode.nbEBands;
		int num2 = celtMode.overlap;
		short[] eBands = celtMode.eBands;
		int num3 = 0;
		do
		{
			array[num3] = decode_mem[num3];
			array2[num3] = 2048 - N;
		}
		while (++num3 < num);
		if (loss_count >= 5 || ((start != 0) ? true : false))
		{
			int num4 = end;
			int num5 = Inlines.IMAX(start, Inlines.IMIN(num4, celtMode.effEBands));
			int[][] array3 = Arrays.InitTwoDimensionalArray<int>(num, N);
			int num6 = ((loss_count == 0) ? 1536 : 512);
			num3 = 0;
			do
			{
				for (int i = start; i < num4; i++)
				{
					oldEBands[num3 * nbEBands + i] = Inlines.MAX16(backgroundLogE[num3 * nbEBands + i], oldEBands[num3 * nbEBands + i] - num6);
				}
			}
			while (++num3 < num);
			uint num7 = rng;
			for (num3 = 0; num3 < num; num3++)
			{
				for (int i = start; i < num5; i++)
				{
					int num8 = eBands[i] << LM;
					int num9 = eBands[i + 1] - eBands[i] << LM;
					for (int j = 0; j < num9; j++)
					{
						num7 = Bands.celt_lcg_rand(num7);
						array3[num3][num8 + j] = (int)num7 >> 20;
					}
					VQ.renormalise_vector(MemoryExtensions.AsSpan(array3[num3]), num9, 32767);
				}
			}
			rng = num7;
			num3 = 0;
			do
			{
				Arrays.MemMoveInt(decode_mem[num3], N, 0, 2048 - N + (num2 >> 1));
			}
			while (++num3 < num);
			CeltCommon.celt_synthesis(celtMode, array3, array, array2, oldEBands, start, num5, num, num, 0, LM, downsample, 0);
		}
		else
		{
			int a = 32767;
			int num10;
			if (loss_count == 0)
			{
				num10 = (last_pitch_index = CeltCommon.celt_plc_pitch_search(decode_mem, num));
			}
			else
			{
				num10 = last_pitch_index;
				a = 26214;
			}
			int[] array4 = new int[num2];
			int[] array5 = new int[1024];
			Span<int> mem = stackalloc int[24];
			int[] window = celtMode.window;
			num3 = 0;
			do
			{
				int num11 = 0;
				int[] array6 = decode_mem[num3];
				int i;
				for (i = 0; i < 1024; i++)
				{
					array5[i] = Inlines.ROUND16(array6[1024 + i], 12);
				}
				if (loss_count == 0)
				{
					int[] array7 = new int[25];
					Autocorrelation._celt_autocorr(array5, array7, window, num2, 24, 1024);
					array7[0] += Inlines.SHR32(array7[0], 13);
					for (i = 1; i <= 24; i++)
					{
						array7[i] -= Inlines.MULT16_32_Q15(2 * i * i, array7[i]);
					}
					CeltLPC.celt_lpc(lpc[num3], array7, 24);
				}
				int num12 = Inlines.IMIN(2 * num10, 1024);
				for (i = 0; i < 24; i++)
				{
					mem[i] = Inlines.ROUND16(array6[2048 - num12 - 1 - i], 12);
				}
				Span<int> span = MemoryExtensions.AsSpan(array5);
				Span<int> x = span.Slice(1024 - num12);
				Span<int> num13 = MemoryExtensions.AsSpan(lpc[num3]);
				span = MemoryExtensions.AsSpan(array5);
				Kernels.celt_fir(x, num13, span.Slice(1024 - num12), num12, 24, mem);
				int num14 = 1;
				int num15 = 1;
				int shift = Inlines.IMAX(0, 2 * Inlines.celt_zlog2(Inlines.celt_maxabs16(array5, 1024 - num12, num12)) - 20);
				int num16 = num12 >> 1;
				for (i = 0; i < num16; i++)
				{
					int num17 = array5[1024 - num16 + i];
					num14 += Inlines.SHR32(Inlines.MULT16_16(num17, num17), shift);
					num17 = array5[1024 - 2 * num16 + i];
					num15 += Inlines.SHR32(Inlines.MULT16_16(num17, num17), shift);
				}
				num14 = Inlines.MIN32(num14, num15);
				int b = Inlines.celt_sqrt(Inlines.frac_div32(Inlines.SHR32(num14, 1), num15));
				Arrays.MemMoveInt(array6, N, 0, 2048 - N);
				int num18 = 1024 - num10;
				int num19 = N + num2;
				int a2 = Inlines.MULT16_16_Q15(a, b);
				int num20;
				i = (num20 = 0);
				while (i < num19)
				{
					if (num20 >= num10)
					{
						num20 -= num10;
						a2 = Inlines.MULT16_16_Q15(a2, b);
					}
					array6[2048 - N + i] = Inlines.SHL32(Inlines.MULT16_16_Q15(a2, array5[num18 + num20]), 12);
					int num21 = Inlines.ROUND16(array6[1024 - N + num18 + num20], 12);
					num11 += Inlines.SHR32(Inlines.MULT16_16(num21, num21), 8);
					i++;
					num20++;
				}
				for (i = 0; i < 24; i++)
				{
					mem[i] = Inlines.ROUND16(array6[2048 - N - 1 - i], 12);
				}
				span = MemoryExtensions.AsSpan(array6);
				Span<int> x2 = span.Slice(2048 - N);
				int[] den = lpc[num3];
				span = MemoryExtensions.AsSpan(array6);
				CeltLPC.celt_iir(x2, den, span.Slice(2048 - N), num19, 24, mem);
				int num22 = 0;
				for (i = 0; i < num19; i++)
				{
					int num23 = Inlines.ROUND16(array6[2048 - N + i], 12);
					num22 += Inlines.SHR32(Inlines.MULT16_16(num23, num23), 8);
				}
				if (num11 <= Inlines.SHR32(num22, 2))
				{
					for (i = 0; i < num19; i++)
					{
						array6[2048 - N + i] = 0;
					}
				}
				else if (num11 < num22)
				{
					int num24 = Inlines.celt_sqrt(Inlines.frac_div32(Inlines.SHR32(num11, 1) + 1, num22 + 1));
					for (i = 0; i < num2; i++)
					{
						int a3 = 32767 - Inlines.MULT16_16_Q15(window[i], 32767 - num24);
						array6[2048 - N + i] = Inlines.MULT16_32_Q15(a3, array6[2048 - N + i]);
					}
					for (i = num2; i < num19; i++)
					{
						array6[2048 - N + i] = Inlines.MULT16_32_Q15(num24, array6[2048 - N + i]);
					}
				}
				CeltCommon.comb_filter(array4, 0, array6, 2048, postfilter_period, postfilter_period, num2, -postfilter_gain, -postfilter_gain, postfilter_tapset, postfilter_tapset, null, 0);
				for (i = 0; i < num2 / 2; i++)
				{
					array6[2048 + i] = Inlines.MULT16_32_Q15(window[i], array4[num2 - 1 - i]) + Inlines.MULT16_32_Q15(window[num2 - i - 1], array4[i]);
				}
			}
			while (++num3 < num);
		}
		loss_count++;
	}

	internal int celt_decode_with_ec(ReadOnlySpan<byte> data, int data_ptr, int len, Span<short> pcm, int pcm_ptr, int frame_size, EntropyCoder dec, int accum)
	{
		int[][] array = new int[2][];
		int[] array2 = new int[2];
		int num = channels;
		int intensity = 0;
		int dual_stereo = 0;
		int num2 = 0;
		int num3 = stream_channels;
		CeltMode celtMode = mode;
		int nbEBands = celtMode.nbEBands;
		int num4 = celtMode.overlap;
		short[] eBands = celtMode.eBands;
		int num5 = start;
		int num6 = end;
		frame_size *= downsample;
		int[] array3 = oldEBands;
		int[] array4 = oldLogE;
		int[] array5 = oldLogE2;
		int[] array6 = backgroundLogE;
		int i;
		for (i = 0; i <= celtMode.maxLM && celtMode.shortMdctSize << i != frame_size; i++)
		{
		}
		if (i > celtMode.maxLM)
		{
			return -1;
		}
		int num7 = 1 << i;
		if (len < 0 || len > 1275 || pcm.IsEmpty)
		{
			return -1;
		}
		int num8 = num7 * celtMode.shortMdctSize;
		int num9 = 0;
		do
		{
			array[num9] = decode_mem[num9];
			array2[num9] = 2048 - num8;
		}
		while (++num9 < num);
		int num10 = num6;
		if (num10 > celtMode.effEBands)
		{
			num10 = celtMode.effEBands;
		}
		if (data.IsEmpty || len <= 1)
		{
			celt_decode_lost(num8, i);
			CeltCommon.deemphasis(array, array2, pcm, pcm_ptr, num8, num, downsample, celtMode.preemph, preemph_memD, accum);
			return frame_size / downsample;
		}
		if (dec == null)
		{
			dec = new EntropyCoder();
			dec.dec_init(data.Slice(data_ptr), (uint)len);
		}
		if (num3 == 1)
		{
			for (int j = 0; j < nbEBands; j++)
			{
				array3[j] = Inlines.MAX16(array3[j], array3[nbEBands + j]);
			}
		}
		int num11 = len * 8;
		int num12 = dec.tell();
		int num13 = ((num12 >= num11) ? 1 : ((num12 == 1) ? dec.dec_bit_logp(data.Slice(data_ptr), 15u) : 0));
		if (num13 != 0)
		{
			num12 = len * 8;
			dec.nbits_total += num12 - dec.tell();
		}
		int g = 0;
		int t = 0;
		int tapset = 0;
		if (num5 == 0 && num12 + 16 <= num11)
		{
			if (dec.dec_bit_logp(data.Slice(data_ptr), 1u) != 0)
			{
				int num14 = (int)dec.dec_uint(data.Slice(data_ptr), 6u);
				t = (16 << num14) + (int)dec.dec_bits(data.Slice(data_ptr), (uint)(4 + num14)) - 1;
				int num15 = (int)dec.dec_bits(data.Slice(data_ptr), 3u);
				if (dec.tell() + 2 <= num11)
				{
					tapset = dec.dec_icdf(data.Slice(data_ptr), Tables.tapset_icdf, 2u);
				}
				g = 3072 * (num15 + 1);
			}
			num12 = dec.tell();
		}
		int num16;
		if (i > 0 && num12 + 3 <= num11)
		{
			num16 = dec.dec_bit_logp(data.Slice(data_ptr), 3u);
			num12 = dec.tell();
		}
		else
		{
			num16 = 0;
		}
		int shortBlocks = ((num16 != 0) ? num7 : 0);
		int intra = ((num12 + 3 <= num11) ? dec.dec_bit_logp(data.Slice(data_ptr), 3u) : 0);
		QuantizeBands.unquant_coarse_energy(celtMode, num5, num6, array3, intra, dec, data.Slice(data_ptr), num3, i);
		int[] tf_res = new int[nbEBands];
		CeltCommon.tf_decode(num5, num6, num16, tf_res, i, dec, data.Slice(data_ptr));
		num12 = dec.tell();
		int spread = 2;
		if (num12 + 4 <= num11)
		{
			spread = dec.dec_icdf(data.Slice(data_ptr), Tables.spread_icdf, 5u);
		}
		int[] array7 = new int[nbEBands];
		CeltCommon.init_caps(celtMode, array7, i, num3);
		int[] array8 = new int[nbEBands];
		int num17 = 6;
		num11 <<= 3;
		num12 = (int)dec.tell_frac();
		for (int j = num5; j < num6; j++)
		{
			int num18 = num3 * (eBands[j + 1] - eBands[j]) << i;
			int num19 = Inlines.IMIN(num18 << 3, Inlines.IMAX(48, num18));
			int num20 = num17;
			int num21 = 0;
			while (num12 + (num20 << 3) < num11 && num21 < array7[j])
			{
				int num22 = dec.dec_bit_logp(data.Slice(data_ptr), (uint)num20);
				num12 = (int)dec.tell_frac();
				if (num22 == 0)
				{
					break;
				}
				num21 += num19;
				num11 -= num19;
				num20 = 1;
			}
			array8[j] = num21;
			if (num21 > 0)
			{
				num17 = Inlines.IMAX(2, num17 - 1);
			}
		}
		int[] array9 = new int[nbEBands];
		int alloc_trim = ((num12 + 48 <= num11) ? dec.dec_icdf(data.Slice(data_ptr), Tables.trim_icdf, 7u) : 5);
		int num23 = (len * 8 << 3) - (int)dec.tell_frac() - 1;
		int num24 = ((num16 != 0 && i >= 2 && num23 >= i + 2 << 3) ? 8 : 0);
		num23 -= num24;
		int[] pulses = new int[nbEBands];
		int[] fine_priority = new int[nbEBands];
		int balance;
		int codedBands = Rate.compute_allocation_decode(celtMode, num5, num6, array8, array7, alloc_trim, ref intensity, ref dual_stereo, num23, out balance, pulses, array9, fine_priority, num3, i, dec, data.Slice(data_ptr), 0, 0);
		QuantizeBands.unquant_fine_energy(celtMode, num5, num6, array3, array9, dec, data.Slice(data_ptr), num3);
		num9 = 0;
		do
		{
			Arrays.MemMoveInt(decode_mem[num9], num8, 0, 2048 - num8 + num4 / 2);
		}
		while (++num9 < num);
		byte[] collapse_masks = new byte[num3 * nbEBands];
		int[][] array10 = Arrays.InitTwoDimensionalArray<int>(num3, num8);
		Bands.quant_all_bands_decode(0, celtMode, num5, num6, array10[0], (num3 == 2) ? array10[1] : null, collapse_masks, null, pulses, shortBlocks, spread, dual_stereo, intensity, tf_res, len * 64 - num24, balance, dec, data.Slice(data_ptr), i, codedBands, ref rng);
		if (num24 > 0)
		{
			num2 = (int)dec.dec_bits(data.Slice(data_ptr), 1u);
		}
		QuantizeBands.unquant_energy_finalise(celtMode, num5, num6, array3, array9, fine_priority, len * 8 - dec.tell(), dec, data.Slice(data_ptr), num3);
		if (num2 != 0)
		{
			Bands.anti_collapse(celtMode, array10, collapse_masks, i, num3, num8, num5, num6, array3, array4, array5, pulses, rng);
		}
		if (num13 != 0)
		{
			for (int j = 0; j < num3 * nbEBands; j++)
			{
				array3[j] = -28672;
			}
		}
		CeltCommon.celt_synthesis(celtMode, array10, array, array2, array3, num5, num10, num3, num, num16, i, downsample, num13);
		num9 = 0;
		do
		{
			postfilter_period = Inlines.IMAX(postfilter_period, 15);
			postfilter_period_old = Inlines.IMAX(postfilter_period_old, 15);
			CeltCommon.comb_filter(array[num9], array2[num9], array[num9], array2[num9], postfilter_period_old, postfilter_period, celtMode.shortMdctSize, postfilter_gain_old, postfilter_gain, postfilter_tapset_old, postfilter_tapset, celtMode.window, num4);
			if (i != 0)
			{
				CeltCommon.comb_filter(array[num9], array2[num9] + celtMode.shortMdctSize, array[num9], array2[num9] + celtMode.shortMdctSize, postfilter_period, t, num8 - celtMode.shortMdctSize, postfilter_gain, g, postfilter_tapset, tapset, celtMode.window, num4);
			}
		}
		while (++num9 < num);
		postfilter_period_old = postfilter_period;
		postfilter_gain_old = postfilter_gain;
		postfilter_tapset_old = postfilter_tapset;
		postfilter_period = t;
		postfilter_gain = g;
		postfilter_tapset = tapset;
		if (i != 0)
		{
			postfilter_period_old = postfilter_period;
			postfilter_gain_old = postfilter_gain;
			postfilter_tapset_old = postfilter_tapset;
		}
		if (num3 == 1)
		{
			Arrays.MemCopy(array3, 0, array3, nbEBands, nbEBands);
		}
		if (num16 == 0)
		{
			Arrays.MemCopy(array4, 0, array5, 0, 2 * nbEBands);
			Arrays.MemCopy(array3, 0, array4, 0, 2 * nbEBands);
			int num25 = ((loss_count >= 10) ? 1024 : num7);
			for (int j = 0; j < 2 * nbEBands; j++)
			{
				array6[j] = Inlines.MIN16(array6[j] + num25, array3[j]);
			}
		}
		else
		{
			for (int j = 0; j < 2 * nbEBands; j++)
			{
				array4[j] = Inlines.MIN16(array4[j], array3[j]);
			}
		}
		num9 = 0;
		do
		{
			for (int j = 0; j < num5; j++)
			{
				array3[num9 * nbEBands + j] = 0;
				array4[num9 * nbEBands + j] = (array5[num9 * nbEBands + j] = -28672);
			}
			for (int j = num6; j < nbEBands; j++)
			{
				array3[num9 * nbEBands + j] = 0;
				array4[num9 * nbEBands + j] = (array5[num9 * nbEBands + j] = -28672);
			}
		}
		while (++num9 < 2);
		rng = dec.rng;
		CeltCommon.deemphasis(array, array2, pcm, pcm_ptr, num8, num, downsample, celtMode.preemph, preemph_memD, accum);
		loss_count = 0;
		if (dec.tell() > 8 * len)
		{
			return -3;
		}
		if (dec.get_error() != 0)
		{
			error = 1;
		}
		return frame_size / downsample;
	}

	internal void SetStartBand(int value)
	{
		if (value < 0 || value >= mode.nbEBands)
		{
			throw new ArgumentException("Start band above max number of ebands (or negative)");
		}
		start = value;
	}

	internal void SetEndBand(int value)
	{
		if (value < 1 || value > mode.nbEBands)
		{
			throw new ArgumentException("End band above max number of ebands (or less than 1)");
		}
		end = value;
	}

	internal void SetChannels(int value)
	{
		if (value < 1 || value > 2)
		{
			throw new ArgumentException("Channel count must be 1 or 2");
		}
		stream_channels = value;
	}

	internal int GetAndClearError()
	{
		int result = error;
		error = 0;
		return result;
	}

	internal int GetLookahead()
	{
		return overlap / downsample;
	}

	internal int GetPitch()
	{
		return postfilter_period;
	}

	internal CeltMode GetMode()
	{
		return mode;
	}

	internal void SetSignalling(int value)
	{
		signalling = value;
	}

	internal uint GetFinalRange()
	{
		return rng;
	}
}

}