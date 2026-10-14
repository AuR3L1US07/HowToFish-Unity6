using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types.CanvasContainers {
public class ResizableContainer : FloatingContainer
{
	[Tooltip("Minimum and maximum range for width and height of the RectTransform.")]
	public FloatRange2D SizeLimits = new FloatRange2D
	{
		X = new FloatRange(0f, 999999f),
		Y = new FloatRange(0f, 999999f)
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetSizeAndShow(Vector2 size, bool ignoreSizeLimits = false)
	{
		ResizeAndShow(size, ignoreSizeLimits);
	}

	protected virtual void ResizeAndShow(Vector2 desiredSize, bool ignoreSizeLimits)
	{
		float x = desiredSize.x;
		float y = desiredSize.y;
		x = Mathf.Clamp(x, SizeLimits.X.Minimum, SizeLimits.X.Maximum);
		y = Mathf.Clamp(y, SizeLimits.Y.Minimum, SizeLimits.Y.Maximum);
		RectTransform.sizeDelta = new Vector2(x, y);
		base.Move();
		base.Show();
	}
}

}