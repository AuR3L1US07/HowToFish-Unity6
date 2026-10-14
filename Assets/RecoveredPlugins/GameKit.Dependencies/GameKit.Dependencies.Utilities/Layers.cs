using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Layers
{
	private static Dictionary<int, int> _interactablesLayers;

	private static void TryInitializeInteractableLayers()
	{
		if (_interactablesLayers != null)
		{
			return;
		}
		_interactablesLayers = new Dictionary<int, int>();
		for (int i = 0; i < 32; i++)
		{
			int num = 0;
			for (int j = 0; j < 32; j++)
			{
				if (!Physics.GetIgnoreLayerCollision(i, j))
				{
					num |= 1 << j;
				}
			}
			_interactablesLayers[i] = num;
		}
	}

	public static int GetInteractableLayersValue(int layer)
	{
		TryInitializeInteractableLayers();
		return _interactablesLayers[layer];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LayerMask GetInteractableLayersMask(int layer)
	{
		return GetInteractableLayersValue(layer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetInteractableLayersValue(GameObject go)
	{
		return GetInteractableLayersValue(go.layer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LayerMask GetInteractableLayersMask(GameObject go)
	{
		return GetInteractableLayersValue(go.layer);
	}

	public static int LayerMaskToLayerNumber(LayerMask mask)
	{
		return LayerValueToLayerNumber(mask.value);
	}

	public static int LayerValueToLayerNumber(int bitmask)
	{
		int num = ((bitmask <= 0) ? 31 : 0);
		while (bitmask > 1)
		{
			bitmask >>= 1;
			num++;
		}
		return num;
	}

	public static bool ContainsLayer(LayerMask layerMask, int layer)
	{
		return (int)layerMask == ((int)layerMask | (1 << layer));
	}
}

}