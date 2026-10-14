using System;
using System.IO;
using Concentus.Common;

namespace Concentus {
public static class ResamplerFactory
{
	public static IResampler CreateResampler(int numChannels, int inRate, int outRate, int quality, TextWriter logger = null)
	{
		return CreateResampler(numChannels, inRate, outRate, inRate, outRate, quality, logger);
	}

	public static IResampler CreateResampler(int numChannels, int ratioNum, int ratioDen, int inRate, int outRate, int quality, TextWriter logger = null)
	{
		if (numChannels <= 0)
		{
			throw new ArgumentOutOfRangeException("numChannels");
		}
		if (ratioNum <= 0)
		{
			throw new ArgumentOutOfRangeException("ratioNum");
		}
		if (ratioDen <= 0)
		{
			throw new ArgumentOutOfRangeException("ratioDen");
		}
		if (inRate <= 0)
		{
			throw new ArgumentOutOfRangeException("inRate");
		}
		if (outRate <= 0)
		{
			throw new ArgumentOutOfRangeException("outRate");
		}
		if (quality < 0 || quality > 10)
		{
			throw new ArgumentOutOfRangeException("quality", "Quality must be between 0 and 10");
		}
		return new SpeexResampler(numChannels, ratioNum, ratioDen, inRate, outRate, quality);
	}
}

}