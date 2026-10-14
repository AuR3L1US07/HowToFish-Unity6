namespace FishNet.Transporting.UTP {
internal static class LocalConnectionStateExtensions
{
	public static bool IsStartingOrStarted(this LocalConnectionState state)
	{
		if (state != LocalConnectionState.Starting)
		{
			return state == LocalConnectionState.Started;
		}
		return true;
	}

	public static bool IsStoppingOrStopped(this LocalConnectionState state)
	{
		if (state != LocalConnectionState.Stopping)
		{
			return state == LocalConnectionState.Stopped;
		}
		return true;
	}
}

}