using Unity.Collections;
using Unity.Networking.Transport.Error;

namespace FishNet.Transporting.UTP {
public static class ErrorUtilities
{
	private static readonly FixedString128Bytes k_NetworkSuccess = "Success";

	private static readonly FixedString128Bytes k_NetworkIdMismatch = "Invalid connection ID {0}.";

	private static readonly FixedString128Bytes k_NetworkVersionMismatch = "Connection ID is invalid. Likely caused by sending on stale connection {0}.";

	private static readonly FixedString128Bytes k_NetworkStateMismatch = "Connection state is invalid. Likely caused by sending on connection {0} which is stale or still connecting.";

	private static readonly FixedString128Bytes k_NetworkPacketOverflow = "Packet is too large to be allocated by the transport.";

	private static readonly FixedString128Bytes k_NetworkSendQueueFull = "Unable to queue packet in the transport. Likely caused by send queue size ('Max Send Queue Size') being too small.";

	public static string ErrorToString(StatusCode error, ulong connectionId)
	{
		return ErrorToString((int)error, connectionId);
	}

	internal static string ErrorToString(int error, ulong connectionId)
	{
		return ErrorToFixedString(error, connectionId).ToString();
	}

	internal static FixedString128Bytes ErrorToFixedString(int error, ulong connectionId)
	{
		return (StatusCode)error switch
		{
			StatusCode.Success => k_NetworkSuccess, 
			StatusCode.NetworkIdMismatch => FixedString.Format(k_NetworkIdMismatch, (float)connectionId), 
			StatusCode.NetworkVersionMismatch => FixedString.Format(k_NetworkVersionMismatch, (float)connectionId), 
			StatusCode.NetworkStateMismatch => FixedString.Format(k_NetworkStateMismatch, (float)connectionId), 
			StatusCode.NetworkPacketOverflow => k_NetworkPacketOverflow, 
			StatusCode.NetworkSendQueueFull => k_NetworkSendQueueFull, 
			_ => FixedString.Format("Unknown error code {0}.", error), 
		};
	}
}

}