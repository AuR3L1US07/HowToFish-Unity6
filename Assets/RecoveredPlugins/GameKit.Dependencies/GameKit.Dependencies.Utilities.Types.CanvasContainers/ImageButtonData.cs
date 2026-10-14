using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types.CanvasContainers {
public class ImageButtonData : ButtonData
{
	public Sprite DisplayImage { get; protected set; }

	public void Initialize(Sprite sprite, string text, PressedDelegate callback, string key = "")
	{
		Initialize(text, callback, key);
		DisplayImage = sprite;
	}

	public override void ResetState()
	{
		base.ResetState();
		DisplayImage = null;
	}
}

}