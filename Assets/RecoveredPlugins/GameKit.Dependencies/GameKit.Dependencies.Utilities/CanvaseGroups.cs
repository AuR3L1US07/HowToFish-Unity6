using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class CanvaseGroups
{
	public static void SetBlockingType(this CanvasGroup group, CanvasGroupBlockingType blockingType)
	{
		if (blockingType != CanvasGroupBlockingType.Unchanged)
		{
			bool interactable = (group.blocksRaycasts = blockingType == CanvasGroupBlockingType.Block);
			group.interactable = interactable;
		}
	}

	public static void SetActive(this CanvasGroup group, CanvasGroupBlockingType blockingType, float alpha)
	{
		group.SetBlockingType(blockingType);
		group.alpha = alpha;
	}

	public static void SetActive(this CanvasGroup group, float alpha)
	{
		group.SetActive(active: true, setAlpha: false);
		group.alpha = alpha;
	}

	public static void SetInactive(this CanvasGroup group, float alpha)
	{
		group.SetActive(active: false, setAlpha: false);
		group.alpha = alpha;
	}

	public static void SetActive(this CanvasGroup group, bool active, bool setAlpha)
	{
		if (group == null)
		{
			return;
		}
		if (setAlpha)
		{
			if (active)
			{
				group.alpha = 1f;
			}
			else
			{
				group.alpha = 0f;
			}
		}
		group.interactable = active;
		group.blocksRaycasts = active;
	}

	public static void SetActive(this CanvasGroup group, bool active, float alpha)
	{
		if (!(group == null))
		{
			group.alpha = alpha;
			group.interactable = active;
			group.blocksRaycasts = active;
		}
	}
}

}