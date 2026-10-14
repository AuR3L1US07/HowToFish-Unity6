using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public class SmoothCameraTarget : MonoBehaviour
{
	private static DDOL _instance;

	public static DDOL GetDDOL()
	{
		if (_instance == null)
		{
			DDOL dDOL = new GameObject
			{
				name = "FirstGearGames DDOL"
			}.AddComponent<DDOL>();
			Object.DontDestroyOnLoad(dDOL);
			_instance = dDOL;
			return dDOL;
		}
		return _instance;
	}
}

}