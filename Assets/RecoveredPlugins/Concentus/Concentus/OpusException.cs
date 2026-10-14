using System;

namespace Concentus {
public class OpusException : Exception
{
	public int OpusErrorCode { get; private set; }

	internal OpusException()
		: base("Unknown error")
	{
		OpusErrorCode = -100;
	}

	internal OpusException(string message)
		: base(message)
	{
		OpusErrorCode = -100;
	}

	internal OpusException(int opusError)
		: base(CodecHelpers.opus_strerror(opusError))
	{
		OpusErrorCode = opusError;
	}

	internal OpusException(string message, int opusError)
		: base(message)
	{
		OpusErrorCode = opusError;
	}
}

}