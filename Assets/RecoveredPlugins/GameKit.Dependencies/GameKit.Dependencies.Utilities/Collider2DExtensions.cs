using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Collider2DExtensions
{
	public static void GetBox2DOverlapParams(this BoxCollider2D boxCollider, out Vector3 center, out Vector3 halfExtents)
	{
		Transform transform = boxCollider.transform;
		center = transform.TransformPoint(boxCollider.offset);
		Vector3 lossyScale = transform.lossyScale;
		Vector3 vector = boxCollider.size;
		float x = vector.x * 0.5f * lossyScale.x;
		float y = vector.y * 0.5f * lossyScale.y;
		float z = vector.z * 0.5f * lossyScale.z;
		halfExtents = new Vector3(x, y, z);
	}

	public static void GetCircleOverlapParams(this CircleCollider2D circleCollider, out Vector3 center, out float radius)
	{
		Transform transform = circleCollider.transform;
		Vector3 position = new Vector3(circleCollider.offset.x, circleCollider.offset.y, circleCollider.transform.position.z);
		center = transform.TransformPoint(position);
		Vector3 lossyScale = transform.lossyScale;
		float val = Math.Abs(lossyScale.x);
		float val2 = Math.Abs(lossyScale.y);
		float val3 = Math.Abs(lossyScale.z);
		radius = circleCollider.radius * Math.Max(Math.Max(val, val2), val3);
	}
}

}