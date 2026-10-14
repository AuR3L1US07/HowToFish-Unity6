using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Materials
{
	public static Color GetColor(this Material material)
	{
		if (material.HasProperty("_Color"))
		{
			return material.color;
		}
		if (material.HasProperty("_TintColor"))
		{
			return material.GetColor("_TintColor");
		}
		return Color.white;
	}

	public static void SetColor(this Material material, Color color)
	{
		if (material.HasProperty("_Color"))
		{
			material.color = color;
		}
		else if (material.HasProperty("_TintColor"))
		{
			material.SetColor("_TintColor", color);
		}
	}
}

}