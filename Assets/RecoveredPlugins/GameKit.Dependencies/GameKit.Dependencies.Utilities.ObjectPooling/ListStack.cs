using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.ObjectPooling {
public class ListStack<GameObject>
{
	private float _lastAccessedTime;

	public int Count => Entries.Count;

	public List<GameObject> Entries { get; private set; } = new List<GameObject>();

	public List<float> EntriesAddedTimes { get; private set; } = new List<float>();

	public ListStack()
	{
		_lastAccessedTime = Time.time;
	}

	public bool AccessedRecently(float threshold)
	{
		return Time.time - _lastAccessedTime < threshold;
	}

	public List<GameObject> Cull(float threshold)
	{
		List<GameObject> list = new List<GameObject>();
		float time = Time.time;
		for (int i = 0; i < EntriesAddedTimes.Count; i++)
		{
			if (time - EntriesAddedTimes[i] > threshold)
			{
				list.Add(Entries[i]);
			}
		}
		if (list.Count > 0)
		{
			Entries.RemoveRange(0, list.Count);
			EntriesAddedTimes.RemoveRange(0, list.Count);
		}
		return list;
	}

	public void Push(GameObject item)
	{
		_lastAccessedTime = Time.time;
		Entries.Add(item);
		EntriesAddedTimes.Add(_lastAccessedTime);
	}

	public GameObject Pop()
	{
		_lastAccessedTime = Time.time;
		if (Entries.Count > 0)
		{
			int index = Entries.Count - 1;
			GameObject result = Entries[index];
			Entries.RemoveAt(index);
			EntriesAddedTimes.RemoveAt(index);
			return result;
		}
		return default(GameObject);
	}

	public void Remove(int index)
	{
		_lastAccessedTime = Time.time;
		Entries.RemoveAt(index);
		EntriesAddedTimes.RemoveAt(index);
	}

	public bool Remove(GameObject item)
	{
		_lastAccessedTime = Time.time;
		int num = Entries.IndexOf(item);
		if (num == -1)
		{
			return false;
		}
		Entries.RemoveAt(num);
		EntriesAddedTimes.RemoveAt(num);
		return true;
	}

	public void Clear()
	{
		_lastAccessedTime = Time.time;
		Entries.Clear();
		EntriesAddedTimes.Clear();
	}
}

}