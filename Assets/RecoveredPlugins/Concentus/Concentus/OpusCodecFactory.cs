using System;
using System.IO;
using Concentus.Enums;
using Concentus.Native;
using Concentus.Structs;

namespace Concentus {
public static class OpusCodecFactory
{
	private static readonly object _mutex = new object();

	private static bool _nativeLibInitialized = false;

	private static bool _isNativeLibAvailable = false;

	private static bool _userAllowNativeLib = true;

	public static bool AttemptToUseNativeLibrary
	{
		get
		{
			return _userAllowNativeLib;
		}
		set
		{
			_userAllowNativeLib = value;
		}
	}

	public static IOpusEncoder CreateEncoder(int sampleRate, int numChannels, OpusApplication application = OpusApplication.OPUS_APPLICATION_AUDIO, TextWriter messageLogger = null)
	{
		if (_userAllowNativeLib && NativeLibraryAvailable(messageLogger))
		{
			return NativeOpusEncoder.Create(sampleRate, numChannels, application);
		}
		return new OpusEncoder(sampleRate, numChannels, application);
	}

	public static IOpusDecoder CreateDecoder(int sampleRate, int numChannels, TextWriter messageLogger = null)
	{
		if (_userAllowNativeLib && NativeLibraryAvailable(messageLogger))
		{
			return NativeOpusDecoder.Create(sampleRate, numChannels);
		}
		return new OpusDecoder(sampleRate, numChannels);
	}

	public static IOpusMultiStreamEncoder CreateMultiStreamEncoder(int sampleRate, int numChannels, int mappingFamily, out int streams, out int coupledStreams, byte[] mapping, OpusApplication application, TextWriter messageLogger = null)
	{
		if (_userAllowNativeLib && NativeLibraryAvailable(messageLogger))
		{
			return NativeOpusMultistreamEncoder.Create(sampleRate, numChannels, mappingFamily, out streams, out coupledStreams, mapping, application);
		}
		return OpusMSEncoder.CreateSurround(sampleRate, numChannels, mappingFamily, out streams, out coupledStreams, mapping, application);
	}

	public static IOpusMultiStreamDecoder CreateMultiStreamDecoder(int sampleRate, int numChannels, int streams, int coupledStreams, byte[] mapping, TextWriter messageLogger = null)
	{
		if (_userAllowNativeLib && NativeLibraryAvailable(messageLogger))
		{
			return NativeOpusMultistreamDecoder.Create(sampleRate, numChannels, streams, coupledStreams, mapping);
		}
		return new OpusMSDecoder(sampleRate, numChannels, streams, coupledStreams, mapping);
	}

	private static bool NativeLibraryAvailable(TextWriter messageLogger)
	{
		lock (_mutex)
		{
			if (!_nativeLibInitialized)
			{
				try
				{
					_isNativeLibAvailable = NativeOpus.Initialize(messageLogger);
					messageLogger?.WriteLine($"Is native opus available? {_isNativeLibAvailable}");
				}
				catch (Exception ex)
				{
					messageLogger?.WriteLine(ex.ToString());
				}
				_nativeLibInitialized = true;
			}
			return _isNativeLibAvailable;
		}
	}
}

}