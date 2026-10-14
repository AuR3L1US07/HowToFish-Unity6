namespace GameKit.Dependencies.Utilities.Types.CanvasContainers {
public class ButtonData : IResettable
{
	public delegate void PressedDelegate(string key);

	private PressedDelegate _delegate;

	public string Text { get; protected set; } = string.Empty;

	public string Key { get; protected set; } = string.Empty;

	public void Initialize(string text, PressedDelegate callback, string key = "")
	{
		Text = text;
		Key = key;
		_delegate = callback;
	}

	public virtual void OnPressed()
	{
		_delegate?.Invoke(Key);
	}

	public virtual void ResetState()
	{
		Text = string.Empty;
		_delegate = null;
		Key = string.Empty;
	}

	public void InitializeState()
	{
	}
}

}