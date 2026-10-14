using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public class RectTransformResizer : MonoBehaviour
{
	public class ResizeData : IResettable
	{
		public byte Remaining;

		public ResizeDelegate Delegate;

		public ResizeData()
		{
			Remaining = 2;
		}

		public void InitializeState()
		{
		}

		public void ResetState()
		{
			Remaining = 2;
			Delegate = null;
		}
	}

	public delegate void ResizeDelegate(bool complete);

	private List<ResizeData> _resizeDatas = new List<ResizeData>();

	private static RectTransformResizer _instance;

	private void OnDestroy()
	{
		foreach (ResizeData resizeData in _resizeDatas)
		{
			ResettableObjectCaches<ResizeData>.Store(resizeData);
		}
	}

	private void Update()
	{
		Resize();
	}

	private void Resize()
	{
		for (int i = 0; i < _resizeDatas.Count; i++)
		{
			_resizeDatas[i].Remaining--;
			bool flag = _resizeDatas[i].Remaining == 0;
			_resizeDatas[i].Delegate?.Invoke(flag);
			if (flag)
			{
				ResettableObjectCaches<ResizeData>.Store(_resizeDatas[i]);
				_resizeDatas.RemoveAt(i);
				i--;
			}
		}
	}

	public static void Resize(ResizeDelegate del)
	{
		if (_instance == null)
		{
			GameObject obj = new GameObject(typeof(RectTransformResizer).Name);
			_instance = obj.AddComponent<RectTransformResizer>();
			Object.DontDestroyOnLoad(obj);
		}
		_instance.Resize_Internal(del);
	}

	private void Resize_Internal(ResizeDelegate del)
	{
		ResizeData resizeData = ResettableObjectCaches<ResizeData>.Retrieve();
		resizeData.Delegate = del;
		_instance._resizeDatas.Add(resizeData);
	}
}

}