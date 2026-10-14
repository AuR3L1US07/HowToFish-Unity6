using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXManager : MonoBehaviour
{
	private static readonly Dictionary<string, VisualEffect> _allVfx = new Dictionary<string, VisualEffect>();
	private static RecoveryVfxParticles _recoveredParticles;

	[SerializeField]
	private Transform _vfxHolder;

	private void Awake()
	{
		_allVfx.Clear();
		_recoveredParticles = new RecoveryVfxParticles(transform);
		for (int i = 0; i < _vfxHolder.childCount; i++)
		{
			_allVfx.Add(_vfxHolder.GetChild(i).name, _vfxHolder.GetChild(i).GetComponent<VisualEffect>());
		}
	}

	private void Start()
	{
		foreach (KeyValuePair<string, VisualEffect> item in _allVfx)
		{
			// Only original, working graphs require GPU warmup. Reconstructed particles
			// are emitted on demand and must not be spawned during this warmup.
			if (item.Value != null && item.Value.enabled && item.Value.visualEffectAsset != null)
				Play(item.Key, Vector3.down * 1000f);
		}
	}

	private void OnDestroy()
	{
		if (_recoveredParticles != null && _recoveredParticles.Owner == transform)
		{
			_recoveredParticles.Dispose();
			_recoveredParticles = null;
			_allVfx.Clear();
		}
	}

	public static void Play(string type, Vector3 position)
	{
		Play(type, position, Vector3.zero);
	}

	public static void Play(string type, Vector3 position, Vector3 direction)
	{
		if (!string.IsNullOrEmpty(type))
		{
			if (!_allVfx.ContainsKey(type))
			{
				if (_recoveredParticles != null && RecoveryVfxParticles.HasProfile(type))
				{
					_recoveredParticles.Play(type, position, direction);
					return;
				}
				Debug.LogWarning($"Could not find {type} in {_allVfx}");
				return;
			}
			var effect = _allVfx[type];
			if (effect == null || !effect.enabled || effect.visualEffectAsset == null)
			{
				_recoveredParticles?.Play(type, position, direction);
				return;
			}
			effect.SetVector3("Position", position);
			effect.SetVector3("Direction", direction);
			effect.SendEvent("Play");
		}
	}
}
