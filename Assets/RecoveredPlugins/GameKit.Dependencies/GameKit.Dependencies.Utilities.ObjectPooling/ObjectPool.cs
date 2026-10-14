using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.ObjectPooling {
public class ObjectPool : MonoBehaviour
{
	private struct DelayedStoreData
	{
		public readonly float StoreTime;

		public readonly bool ParentPooler;

		public DelayedStoreData(float storeTime, bool parentPooler)
		{
			StoreTime = storeTime;
			ParentPooler = parentPooler;
		}
	}

	[Tooltip("Time to wait before destroying object pools with no activity as well pool entries which haven't been used recently. Use -1f to disable this feature.")]
	[SerializeField]
	private float _dataExpirationDelay = 60f;

	private static ObjectPool _instance;

	private Transform _collector;

	private List<PoolData> _pools = new List<PoolData>();

	private Dictionary<string, Transform> _categoryChildren = new Dictionary<string, Transform>();

	private Dictionary<GameObject, PoolData> _poolPrefabs = new Dictionary<GameObject, PoolData>();

	private Dictionary<GameObject, PoolData> _activeObjects = new Dictionary<GameObject, PoolData>();

	private Dictionary<GameObject, DelayedStoreData> _delayedStoreObjects = new Dictionary<GameObject, DelayedStoreData>();

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("Multiple ObjectPool scripts found. This script auto loads itself and does not need to be placed in your scenes.");
			}
			Object.Destroy(this);
		}
		else
		{
			_instance = this;
		}
	}

	private void Update()
	{
	}

	private void Start()
	{
		StartCoroutine(__CleanupChecks());
	}

	private IEnumerator __CleanupChecks()
	{
		int poolIndex = 0;
		while (true)
		{
			if (_delayedStoreObjects.Count > 0)
			{
				List<GameObject> list = new List<GameObject>();
				foreach (KeyValuePair<GameObject, DelayedStoreData> delayedStoreObject in _delayedStoreObjects)
				{
					if (Time.time >= delayedStoreObject.Value.StoreTime)
					{
						list.Add(delayedStoreObject.Key);
						Store(delayedStoreObject.Key, delayedStoreObject.Value.ParentPooler);
					}
				}
				for (int i = 0; i < list.Count; i++)
				{
					_delayedStoreObjects.Remove(list[i]);
				}
			}
			if (_dataExpirationDelay > 0f && _pools.Count > 0)
			{
				if (poolIndex >= _pools.Count)
				{
					poolIndex = 0;
				}
				if (_pools[poolIndex].PoolExpired())
				{
					_poolPrefabs.Remove(_pools[poolIndex].Prefab);
					DestroyPool(_pools[poolIndex], removeFromList: false);
					_pools.RemoveAt(poolIndex);
					poolIndex--;
				}
				else
				{
					List<GameObject> list2 = _pools[poolIndex].Cull();
					for (int j = 0; j < list2.Count; j++)
					{
						Object.Destroy(list2[j]);
					}
				}
				poolIndex++;
			}
			yield return null;
		}
	}

	public IEnumerator __Reset(bool destroyActive)
	{
		_poolPrefabs.Clear();
		_categoryChildren.Clear();
		_pools.Clear();
		_delayedStoreObjects.Clear();
		base.transform.DestroyChildren();
		WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
		while (base.transform.childCount > 0)
		{
			yield return endOfFrame;
		}
		if (destroyActive)
		{
			List<GameObject> objects = new List<GameObject>();
			foreach (KeyValuePair<GameObject, PoolData> activeObject in _activeObjects)
			{
				objects.Add(activeObject.Key);
			}
			for (int i = 0; i < objects.Count; i++)
			{
				if (objects[i] != null)
				{
					Object.Destroy(objects[i]);
					while (objects[i] != null)
					{
						yield return endOfFrame;
					}
				}
			}
		}
		_activeObjects.Clear();
	}

	private void DestroyPool(PoolData poolData, bool removeFromList)
	{
		for (int i = 0; i < poolData.Objects.Entries.Count; i++)
		{
			if (poolData.Objects.Entries[i] != null)
			{
				Object.Destroy(poolData.Objects.Entries[i]);
			}
		}
		if (removeFromList)
		{
			_pools.Remove(poolData);
		}
	}

	private PoolData ReturnPoolData(GameObject prefab)
	{
		_poolPrefabs.TryGetValue(prefab, out var value);
		if (value == null)
		{
			return CreatePool(prefab);
		}
		return value;
	}

	private void SetGameObjectPositionRotation(GameObject result, Vector3 position, Quaternion rotation)
	{
		result.transform.position = position;
		result.transform.rotation = rotation;
	}

	public static GameObject Retrieve(GameObject poolObject)
	{
		return _instance.RetrieveInternal(poolObject);
	}

	private GameObject RetrieveInternal(GameObject poolObject)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(poolObject, out pool);
		if (gameObject != null && pool != null)
		{
			SetGameObjectPositionRotation(gameObject, pool.Prefab.transform.position, pool.Prefab.transform.rotation);
			gameObject.transform.SetParent(null);
		}
		return FinalizeRetrieve(gameObject, pool);
	}

	public static GameObject Retrieve(GameObject poolObject, Transform parent, bool instantiateInWorldSpace = true)
	{
		return _instance.RetrieveInternal(poolObject, parent, instantiateInWorldSpace);
	}

	public GameObject RetrieveInternal(GameObject poolObject, Transform parent, bool instantiateInWorldSpace = true)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(poolObject, out pool);
		if (gameObject != null && pool != null)
		{
			SetGameObjectPositionRotation(gameObject, pool.Prefab.transform.position, pool.Prefab.transform.rotation);
			gameObject.transform.SetParent(parent, instantiateInWorldSpace);
		}
		return FinalizeRetrieve(gameObject, pool);
	}

	public static GameObject Retrieve(GameObject poolObject, Vector3 position, Quaternion rotation)
	{
		return _instance.RetrieveInternal(poolObject, position, rotation);
	}

	private GameObject RetrieveInternal(GameObject poolObject, Vector3 position, Quaternion rotation)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(poolObject, out pool);
		if (gameObject != null)
		{
			SetGameObjectPositionRotation(gameObject, position, rotation);
			gameObject.transform.SetParent(null);
		}
		return FinalizeRetrieve(gameObject, pool);
	}

	public GameObject Retrieve(GameObject poolObject, Vector3 position, Quaternion rotation, Transform parent)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(poolObject, out pool);
		if (gameObject != null)
		{
			SetGameObjectPositionRotation(gameObject, position, rotation);
			gameObject.transform.SetParent(parent, worldPositionStays: true);
		}
		return FinalizeRetrieve(gameObject, pool);
	}

	public static T Retrieve<T>(GameObject prefab)
	{
		return _instance.RetrieveInternal<T>(prefab);
	}

	private T RetrieveInternal<T>(GameObject prefab)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(prefab, out pool);
		if (gameObject != null && pool != null)
		{
			SetGameObjectPositionRotation(gameObject, pool.Prefab.transform.position, pool.Prefab.transform.rotation);
			gameObject.transform.SetParent(null);
		}
		return FinalizeRetrieve(gameObject, pool).GetComponent<T>();
	}

	public static T Retrieve<T>(GameObject prefab, Transform parent, bool instantiateInWorldSpace = true)
	{
		return _instance.RetrieveInternal<T>(prefab, parent, instantiateInWorldSpace);
	}

	private T RetrieveInternal<T>(GameObject prefab, Transform parent, bool instantiateInWorldSpace = true)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(prefab, out pool);
		if (gameObject != null && pool != null)
		{
			SetGameObjectPositionRotation(gameObject, pool.Prefab.transform.position, pool.Prefab.transform.rotation);
			gameObject.transform.SetParent(parent, instantiateInWorldSpace);
		}
		return FinalizeRetrieve(gameObject, pool).GetComponent<T>();
	}

	public static T Retrieve<T>(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return _instance.RetrieveInternal<T>(prefab, position, rotation);
	}

	private T RetrieveInternal<T>(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(prefab, out pool);
		if (gameObject != null)
		{
			SetGameObjectPositionRotation(gameObject, position, rotation);
			gameObject.transform.SetParent(null);
		}
		return FinalizeRetrieve(gameObject, pool).GetComponent<T>();
	}

	public static T Retrieve<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
	{
		return _instance.RetrieveInternal<T>(prefab, position, rotation, parent);
	}

	private T RetrieveInternal<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
	{
		PoolData pool;
		GameObject gameObject = ReturnPooledObject(prefab, out pool);
		if (gameObject != null)
		{
			SetGameObjectPositionRotation(gameObject, position, rotation);
			gameObject.transform.SetParent(parent, worldPositionStays: true);
		}
		return FinalizeRetrieve(gameObject, pool).GetComponent<T>();
	}

	private GameObject FinalizeRetrieve(GameObject result, PoolData pool)
	{
		_activeObjects[result] = pool;
		if (pool != null)
		{
			result.SetActive(pool.Prefab.activeSelf);
		}
		return result;
	}

	public static void Store(GameObject instantiatedObject, float delay, bool parentPooler = true)
	{
		_instance.StoreInternal(instantiatedObject, delay, parentPooler);
	}

	private void StoreInternal(GameObject instantiatedObject, float delay, bool parentPooler = true)
	{
		_delayedStoreObjects[instantiatedObject] = new DelayedStoreData(Time.time + delay, parentPooler);
	}

	public static void Store(GameObject instantiatedObject, bool parentPooler = true)
	{
		_instance.StoreInternal(instantiatedObject, parentPooler);
	}

	private void StoreInternal(GameObject instantiatedObject, bool parentPooler = true)
	{
		if (instantiatedObject == null)
		{
			Debug.LogWarning("ObjectPooler -> StoreObject -> poolObject cannot be null.");
			return;
		}
		if (_activeObjects.TryGetValue(instantiatedObject, out var value))
		{
			_activeObjects.Remove(instantiatedObject);
		}
		else
		{
			value = ReturnPoolData(instantiatedObject);
		}
		AddToPool(instantiatedObject, value, parentPooler);
	}

	private void MakeCollector()
	{
		if (_collector == null)
		{
			_categoryChildren.Clear();
			_collector = new GameObject().transform;
			_collector.name = "ObjectPoolerCollector";
		}
	}

	private GameObject ReturnPooledObject(GameObject prefab, out PoolData pool)
	{
		if (prefab == null)
		{
			pool = null;
			Debug.LogError("ObjectPooler -> RetrieveObject -> prefab cannot be null.");
			return null;
		}
		pool = ReturnPoolData(prefab);
		GameObject gameObject = pool.Objects.Pop();
		if (gameObject == null)
		{
			gameObject = Object.Instantiate(prefab);
		}
		return gameObject;
	}

	private PoolData CreatePool(GameObject prefab)
	{
		if (prefab == null)
		{
			Debug.LogError("ObjectPooler -> CreatePool -> prefab cannot be null.");
			return null;
		}
		PoolData poolData = new PoolData(prefab, _dataExpirationDelay);
		if (prefab.scene.name != null)
		{
			AddToPool(prefab, poolData);
		}
		_pools.Add(poolData);
		_poolPrefabs[prefab] = poolData;
		return poolData;
	}

	private void AddToPool(GameObject instantiatedObject, PoolData pool, bool parentPooler = true)
	{
		if (instantiatedObject == null)
		{
			Debug.LogError("ObjectPooler -> AddToPool -> instantiatedObject is null.");
			return;
		}
		if (pool == null)
		{
			Debug.LogError("ObjectPooler -> AddToPool -> pool is null.");
			return;
		}
		instantiatedObject.SetActive(value: false);
		pool.Objects.Push(instantiatedObject);
		if (parentPooler)
		{
			ParentPooler(instantiatedObject, worldPositionStays: true);
		}
	}

	private void ParentPooler(GameObject poolObject, bool worldPositionStays)
	{
		MakeCollector();
		string key = poolObject.tag;
		if (!_categoryChildren.TryGetValue(key, out var value))
		{
			value = new GameObject().transform;
			value.name = key;
			value.SetParent(_collector);
			_categoryChildren[key] = value;
		}
		poolObject.transform.SetParent(value, worldPositionStays);
	}
}

}