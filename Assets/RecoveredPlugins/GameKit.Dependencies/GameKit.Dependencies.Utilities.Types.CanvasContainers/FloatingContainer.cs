using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types.CanvasContainers {
public class FloatingContainer : CanvasGroupFader
{
	[Tooltip("RectTransform to move.")]
	[SerializeField]
	[TabGroup("Components", false)]
	protected RectTransform RectTransform;

	[Tooltip("True to use edge avoidance.")]
	[SerializeField]
	[TabGroup("Sizing", false)]
	protected bool UseEdgeAvoidance = true;

	[Tooltip("How much to avoid screen edges when being moved.")]
	[SerializeField]
	[TabGroup("Sizing", false)]
	[ShowIf("UseEdgeAvoidance", true, ShowIfAttribute.DisablingType.DontDraw)]
	protected Vector2 EdgeAvoidance;

	private Vector3 _positionGoal;

	private Quaternion _rotationGoal;

	private Vector3 _scaleGoal = Vector3.one;

	private Vector2? _edgeAvoidance;

	public void AttachGameObject(GameObject go)
	{
		if (!(go == null))
		{
			Transform obj = go.transform;
			obj.SetParent(base.transform);
			obj.localPosition = Vector3.zero;
			obj.localRotation = Quaternion.identity;
			obj.localScale = Vector3.one;
		}
	}

	public virtual void Show(Vector3 position, Quaternion rotation, Vector3 scale, Vector2 pivot, Vector2? edgeAvoidanceOverride = null)
	{
		UpdateEdgeAvoidance(edgeAvoidanceOverride, move: false);
		UpdatePivot(pivot, move: false);
		UpdatePositionRotationAndScale(position, rotation, scale);
		base.Show();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void Show(Vector3 position, Vector2? edgeAvoidanceOverride = null)
	{
		Show(position, Quaternion.identity, Vector3.one, RectTransform.pivot);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void Show(Vector3 position, Quaternion rotation, Vector2? edgeAvoidanceOverride = null)
	{
		Show(position, rotation, Vector3.one, RectTransform.pivot);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void Show(Transform startingPoint, Vector2? edgeAvoidanceOverride = null)
	{
		if (startingPoint == null)
		{
			Debug.LogError("A null Transform cannot be used as the starting point.");
		}
		else
		{
			Show(startingPoint.position, startingPoint.rotation, startingPoint.localScale, RectTransform.pivot);
		}
	}

	public virtual void UpdatePivot(Vector2 pivot, bool move = true)
	{
		RectTransform.pivot = pivot;
		if (move)
		{
			Move();
		}
	}

	public virtual void UpdatePosition(Vector3 position, bool move = true)
	{
		_positionGoal = position;
		if (move)
		{
			Move();
		}
	}

	public virtual void UpdateRotation(Quaternion rotation, bool move = true)
	{
		_rotationGoal = rotation;
		if (move)
		{
			Move();
		}
	}

	public virtual void UpdateScale(Vector3 scale, bool move = true)
	{
		_scaleGoal = scale;
		if (move)
		{
			Move();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void UpdatePositionAndRotation(Vector3 position, Quaternion rotation, bool move = true)
	{
		UpdatePosition(position, move: false);
		UpdateRotation(rotation, move: false);
		if (move)
		{
			Move();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void UpdatePositionRotationAndScale(Vector3 position, Quaternion rotation, Vector3 scale, bool move = true)
	{
		UpdatePositionAndRotation(position, rotation, move: false);
		UpdateScale(scale, move: false);
		Move();
	}

	public virtual void UpdateEdgeAvoidance(Vector2? edgeAvoidanceOverride = null, bool move = true)
	{
		_edgeAvoidance = (edgeAvoidanceOverride.HasValue ? edgeAvoidanceOverride.Value : EdgeAvoidance);
		if (move)
		{
			Move();
		}
	}

	protected virtual void Move()
	{
		RectTransform.localScale = _scaleGoal;
		Vector2 vector = _positionGoal;
		if (UseEdgeAvoidance)
		{
			Vector2 padding = (_edgeAvoidance.HasValue ? _edgeAvoidance.Value : EdgeAvoidance);
			vector = RectTransform.GetOnScreenPosition(_positionGoal, padding);
		}
		RectTransform.SetPositionAndRotation(vector, _rotationGoal);
	}
}

}