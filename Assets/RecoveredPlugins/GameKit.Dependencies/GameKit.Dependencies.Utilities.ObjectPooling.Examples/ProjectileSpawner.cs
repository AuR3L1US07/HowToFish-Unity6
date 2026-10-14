using UnityEngine;

namespace GameKit.Dependencies.Utilities.ObjectPooling.Examples {
public class ProjectileSpawner : MonoBehaviour
{
	public GameObject Prefab;

	public bool UsePool = true;

	public float _instantiateDelay = 0.075f;

	private float _nextInstantiate;

	private void Update()
	{
		if (!(Time.unscaledTime < _nextInstantiate))
		{
			_nextInstantiate = Time.unscaledTime + _instantiateDelay;
			if (UsePool)
			{
				ObjectPool.Retrieve(Prefab, base.transform.position, Quaternion.identity);
			}
			else
			{
				Object.Instantiate(Prefab, base.transform.position, Quaternion.identity);
			}
		}
	}
}

}