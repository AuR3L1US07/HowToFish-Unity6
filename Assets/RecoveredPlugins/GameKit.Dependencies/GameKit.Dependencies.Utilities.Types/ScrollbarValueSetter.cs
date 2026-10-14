using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Dependencies.Utilities.Types {
public class ScrollbarValueSetter
{
	private Scrollbar _scrollBar;

	private float _value;

	private int _updatedFrame = -1;

	private int _fixFrames;

	public ScrollbarValueSetter(Scrollbar sb, int fixFrames = 2)
	{
		_scrollBar = sb;
		_fixFrames = fixFrames;
	}

	public void SetValue(float value)
	{
		_scrollBar.value = value;
		_value = value;
		_updatedFrame = Time.frameCount;
	}

	public void LateUpdate()
	{
		if (_updatedFrame != -1 && Time.frameCount - _updatedFrame >= _fixFrames)
		{
			_updatedFrame = -1;
			_scrollBar.value = _value;
		}
	}
}

}