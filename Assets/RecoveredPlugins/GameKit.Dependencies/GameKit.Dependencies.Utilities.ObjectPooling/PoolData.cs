using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.ObjectPooling {
public class PoolData
{
	public readonly GameObject Prefab;

	public ListStack<GameObject> Objects = new ListStack<GameObject>();

	private float _expirationDuration = -1f;

	public PoolData(GameObject prefab, float expirationDuration)
	{
		Prefab = prefab;
		_expirationDuration = expirationDuration;
	}

	public bool PoolExpired()
	{
		if (_expirationDuration == -1f)
		{
			return false;
		}
		return !Objects.AccessedRecently(_expirationDuration);
	}

	public List<GameObject> Cull()
	{
		if (_expirationDuration == -1f)
		{
			return new List<GameObject>();
		}
		return Objects.Cull(_expirationDuration);
	}
}

}