using System.Collections.Generic;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameKit.Dependencies.Utilities {
public static class Objects
{
	public static bool IsDestroyed(this GameObject gameObject)
	{
		if (gameObject == null)
		{
			return (object)gameObject != null;
		}
		return false;
	}

	public static List<T> FindAllObjectsOfType<T>(bool activeSceneOnly = true, bool requireSceneLoaded = false, bool includeDDOL = true, bool includeInactive = true)
	{
		List<T> list = new List<T>();
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene sceneAt = SceneManager.GetSceneAt(i);
			if ((!activeSceneOnly || !(SceneManager.GetActiveScene() != sceneAt)) && !(!sceneAt.isLoaded && requireSceneLoaded))
			{
				GameObject[] rootGameObjects = sceneAt.GetRootGameObjects();
				for (int j = 0; j < rootGameObjects.Length; j++)
				{
					list.AddRange(rootGameObjects[j].GetComponentsInChildren<T>(includeInactive));
				}
			}
		}
		if (includeDDOL)
		{
			GameObject gameObject = DDOL.GetDDOL().gameObject;
			list.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive));
		}
		return list;
	}
}

}