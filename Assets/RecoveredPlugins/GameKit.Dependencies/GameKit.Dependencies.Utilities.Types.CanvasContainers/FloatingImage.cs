using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Dependencies.Utilities.Types.CanvasContainers {
public class FloatingImage : FloatingContainer
{
	[Tooltip("Renderer to apply sprite on.")]
	[SerializeField]
	[TabGroup("Components", false)]
	protected Image Renderer;

	public virtual void SetSprite(Sprite sprite, Vector3? sizeOverride)
	{
		Renderer.sprite = sprite;
		Vector3 vector = ((!sizeOverride.HasValue) ? (sprite.bounds.size * sprite.pixelsPerUnit) : sizeOverride.Value);
		Renderer.rectTransform.sizeDelta = vector;
	}
}

}