using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Transforms
{
	public static Vector2 HalfSizeDelta(this RectTransform rectTransform, bool useScale = false)
	{
		return (useScale ? rectTransform.SizeDeltaScaled() : rectTransform.sizeDelta) / 2f;
	}

	public static Vector2 SizeDeltaScaled(this RectTransform rectTransform)
	{
		return rectTransform.sizeDelta * rectTransform.localScale;
	}

	public static Vector3 GetOnScreenPosition(this RectTransform rectTransform, Vector3 desiredPosition, Vector2 padding)
	{
		RectTransform obj = rectTransform.GetComponentInParent<Canvas>().transform as RectTransform;
		Vector2 vector = desiredPosition;
		Vector2 vector2 = obj.localScale;
		Vector2 vector3 = Vector2.one - rectTransform.pivot;
		Vector2 vector4 = rectTransform.sizeDelta * vector2 * rectTransform.pivot + padding;
		Vector2 vector5 = (obj.rect.size - (rectTransform.sizeDelta * vector3 + padding)) * vector2;
		float x = Mathf.Clamp(vector.x, vector4.x, vector5.x);
		float y = Mathf.Clamp(vector.y, vector4.y, vector5.y);
		return new Vector2(x, y);
	}

	public static void SetParentAndKeepTransform(this Transform src, Transform parent)
	{
		Vector3 position = src.position;
		Quaternion rotation = src.rotation;
		Vector3 localScale = src.localScale;
		src.SetParent(parent);
		src.position = position;
		src.rotation = rotation;
		src.localScale = localScale;
	}

	public static void DestroyChildren(this Transform t, bool destroyImmediately = false)
	{
		if (destroyImmediately)
		{
			List<Transform> list = CollectionCaches<Transform>.RetrieveList();
			int childCount = t.childCount;
			for (int i = 0; i < childCount; i++)
			{
				list.Add(t.GetChild(i));
			}
			foreach (Transform item in list)
			{
				Object.DestroyImmediate(item);
			}
			CollectionCaches<Transform>.Store(list);
			return;
		}
		foreach (Transform item2 in t)
		{
			Object.Destroy(item2.gameObject);
		}
	}

	public static void DestroyChildren<T>(this Transform t, bool destroyImmediately = false) where T : MonoBehaviour
	{
		T[] componentsInChildren = t.GetComponentsInChildren<T>();
		foreach (T val in componentsInChildren)
		{
			if (destroyImmediately)
			{
				Object.DestroyImmediate(val.gameObject);
			}
			else
			{
				Object.Destroy(val.gameObject);
			}
		}
	}

	public static void GetComponentsInChildren<T>(this Transform parent, List<T> results, bool includeParent = true, bool includeInactive = false) where T : Component
	{
		if (!includeParent)
		{
			List<T> list = CollectionCaches<T>.RetrieveList();
			for (int i = 0; i < parent.childCount; i++)
			{
				parent.GetChild(i).GetComponentsInChildren(includeInactive, list);
				results.AddRange(list);
			}
			CollectionCaches<T>.Store(list);
		}
		else
		{
			parent.GetComponentsInChildren(includeInactive, results);
		}
	}

	public static Vector3 GetPosition(this Transform t, bool localSpace)
	{
		if (!localSpace)
		{
			return t.position;
		}
		return t.localPosition;
	}

	public static Quaternion GetRotation(this Transform t, bool localSpace)
	{
		if (!localSpace)
		{
			return t.rotation;
		}
		return t.localRotation;
	}

	public static Vector3 GetScale(this Transform t)
	{
		return t.localScale;
	}

	public static void SetPosition(this Transform t, bool localSpace, Vector3 pos)
	{
		if (localSpace)
		{
			t.localPosition = pos;
		}
		else
		{
			t.position = pos;
		}
	}

	public static void SetRotation(this Transform t, bool localSpace, Quaternion rot)
	{
		if (localSpace)
		{
			t.localRotation = rot;
		}
		else
		{
			t.rotation = rot;
		}
	}

	public static void SetScale(this Transform t, Vector3 scale)
	{
		t.localScale = scale;
	}
}

}