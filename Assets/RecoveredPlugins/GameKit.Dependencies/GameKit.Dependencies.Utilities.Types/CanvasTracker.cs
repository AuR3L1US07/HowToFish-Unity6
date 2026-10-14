using System.Collections.Generic;

namespace GameKit.Dependencies.Utilities.Types {
public static class CanvasTracker
{
	private static List<object> _inputBlockingCanvases = new List<object>();

	private static List<object> _openCanvases = new List<object>();

	public static IReadOnlyList<object> InputBlockingCanvases => _inputBlockingCanvases;

	public static IReadOnlyList<object> OpenCanvases => _openCanvases;

	public static bool IsInputBlockingCanvasOpen => _inputBlockingCanvases.Count > 0;

	public static bool IsLastOpenCanvas(object canvas)
	{
		return IsEmptyCollectionOrLastEntry(canvas, _openCanvases);
	}

	public static bool IsLastInputBlockingCanvas(object canvas)
	{
		return IsEmptyCollectionOrLastEntry(canvas, _inputBlockingCanvases);
	}

	private static bool IsEmptyCollectionOrLastEntry(object canvas, List<object> collection)
	{
		int count = collection.Count;
		if (count == 0)
		{
			return true;
		}
		return collection[count - 1] == canvas;
	}

	public static void ClearCollections()
	{
		_openCanvases.Clear();
		_inputBlockingCanvases.Clear();
	}

	public static void RemoveNullReferences()
	{
		RemoveNullEntries(_openCanvases);
		RemoveNullEntries(_inputBlockingCanvases);
		static void RemoveNullEntries(List<object> collection)
		{
			for (int i = 0; i < collection.Count; i++)
			{
				if (collection[i] == null)
				{
					collection.RemoveAt(i);
					i--;
				}
			}
		}
	}

	public static bool IsOpenCanvas(object canvas)
	{
		return _openCanvases.Contains(canvas);
	}

	public static bool IsInputBlockingCanvas(object canvas)
	{
		return _inputBlockingCanvases.Contains(canvas);
	}

	public static bool AddOpenCanvas(object canvas, bool addToBlocking)
	{
		bool num = _openCanvases.AddUnique(canvas);
		if (num && addToBlocking)
		{
			_inputBlockingCanvases.Add(canvas);
		}
		return num;
	}

	public static bool RemoveOpenCanvas(object canvas)
	{
		_inputBlockingCanvases.Remove(canvas);
		return _openCanvases.Remove(canvas);
	}
}

}