using UnityEngine;

namespace GameKit.Dependencies.Utilities.ObjectPooling.Examples {
public class Projectile : MonoBehaviour
{
	[Tooltip("If above 0f projectiles are stored with a delay rather than when off screen.")]
	[Range(0f, 5f)]
	public float DestroyDelay;

	public float MoveRate = 30f;

	private ProjectileSpawner _spawner;

	private MeshRenderer[] _renderers;

	private Vector3 _moveDirection;

	private bool _exitingPlayMode;

	private void Awake()
	{
		for (int i = 0; i < 30; i++)
		{
			_spawner = Object.FindObjectOfType<ProjectileSpawner>();
			_renderers = GetComponentsInChildren<MeshRenderer>();
		}
	}

	private void OnBecameInvisible()
	{
		if (!_exitingPlayMode && DestroyDelay <= 0f)
		{
			if (_spawner.UsePool)
			{
				ObjectPool.Store(base.gameObject);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnEnable()
	{
		_moveDirection = new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized;
		if (_spawner.UsePool && DestroyDelay > 0f)
		{
			ObjectPool.Store(base.gameObject, DestroyDelay);
		}
	}

	private void Update()
	{
		base.transform.position += _moveDirection * MoveRate * Time.deltaTime;
	}
}

}