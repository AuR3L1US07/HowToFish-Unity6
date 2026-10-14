using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
	private static T _instance;

	public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				T[] array = Resources.FindObjectsOfTypeAll<T>();
				if (array.Length == 0)
				{
					Debug.LogError("SingletonScriptableObject: results length is 0 of " + typeof(T).ToString());
					return null;
				}
				if (array.Length > 1)
				{
					Debug.LogError("SingletonScriptableObject: results length is greater than 1 of " + typeof(T).ToString());
					return null;
				}
				_instance = array[0];
				_instance.hideFlags = HideFlags.DontUnloadUnusedAsset;
			}
			return _instance;
		}
	}
}

}