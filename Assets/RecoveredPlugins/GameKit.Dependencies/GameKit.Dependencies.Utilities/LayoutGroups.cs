using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Dependencies.Utilities {
public static class LayoutGroups
{
	public static int EntriesPerWidth(this GridLayoutGroup lg)
	{
		return Mathf.CeilToInt(lg.GetComponent<RectTransform>().rect.width / lg.cellSize.x);
	}
}

}