using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class ApplicationState
{
	private static bool _isQuitting;

	static ApplicationState()
	{
		_isQuitting = false;
		Application.quitting -= Application_quitting;
		Application.quitting += Application_quitting;
	}

	private static void Application_quitting()
	{
		_isQuitting = true;
	}

	public static bool IsQuitting()
	{
		return _isQuitting;
	}

	public static bool IsPlaying()
	{
		return Application.isPlaying;
	}

	public static void Quit()
	{
		Application.Quit();
	}
}

}