using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class ColliderExtensions
{
	public static void GetBoxOverlapParams(this BoxCollider boxCollider, out Vector3 center, out Vector3 halfExtents)
	{
		Transform transform = boxCollider.transform;
		center = transform.TransformPoint(boxCollider.center);
		Vector3 lossyScale = transform.lossyScale;
		Vector3 size = boxCollider.size;
		float x = size.x * 0.5f * lossyScale.x;
		float y = size.y * 0.5f * lossyScale.y;
		float z = size.z * 0.5f * lossyScale.z;
		halfExtents = new Vector3(x, y, z);
	}

	public static void GetCapsuleCastParams(this CapsuleCollider capsuleCollider, out Vector3 point1, out Vector3 point2, out float radius)
	{
		Transform transform = capsuleCollider.transform;
		Vector3 lossyScale = transform.lossyScale;
		float num = Math.Abs(lossyScale.x);
		float num2 = Math.Abs(lossyScale.y);
		float num3 = Math.Abs(lossyScale.z);
		float num4;
		Vector3 vector;
		switch (capsuleCollider.direction)
		{
		case 1:
			radius = capsuleCollider.radius * Math.Max(num, num3);
			num4 = capsuleCollider.height * num2;
			vector = Vector3.up;
			break;
		case 2:
			radius = capsuleCollider.radius * Math.Max(num, num2);
			num4 = capsuleCollider.height * num3;
			vector = Vector3.forward;
			break;
		default:
			radius = capsuleCollider.radius * Math.Max(num2, num3);
			num4 = capsuleCollider.height * num;
			vector = Vector3.right;
			break;
		}
		Vector3 vector2 = transform.TransformPoint(capsuleCollider.center);
		Vector3 vector3 = ((num4 < radius * 2f) ? Vector3.zero : transform.TransformDirection(vector * (num4 * 0.5f - radius)));
		float x = vector2.x + vector3.x;
		float y = vector2.y + vector3.y;
		float z = vector2.z + vector3.z;
		float x2 = vector2.x - vector3.x;
		float y2 = vector2.y - vector3.y;
		float z2 = vector2.z - vector3.z;
		point1 = new Vector3(x, y, z);
		point2 = new Vector3(x2, y2, z2);
	}

	public static void GetSphereOverlapParams(this SphereCollider sphereCollider, out Vector3 center, out float radius)
	{
		Transform transform = sphereCollider.transform;
		center = transform.TransformPoint(sphereCollider.center);
		Vector3 lossyScale = transform.lossyScale;
		float val = Math.Abs(lossyScale.x);
		float val2 = Math.Abs(lossyScale.y);
		float val3 = Math.Abs(lossyScale.z);
		radius = sphereCollider.radius * Math.Max(Math.Max(val, val2), val3);
	}
}

}