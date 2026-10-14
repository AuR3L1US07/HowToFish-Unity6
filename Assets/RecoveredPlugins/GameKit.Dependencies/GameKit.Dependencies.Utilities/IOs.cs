using System.Collections.Generic;
using System.IO;

namespace GameKit.Dependencies.Utilities {
public static class IOs
{
	public static string[] GetDirectoryFiles(string startingPath, HashSet<string> excludedPaths, bool recursive, string extension)
	{
		if (excludedPaths.Count == 0)
		{
			return Directory.GetFiles(startingPath, extension, SearchOption.AllDirectories);
		}
		if (excludedPaths.Contains(startingPath))
		{
			return new string[0];
		}
		List<string> list = new List<string> { startingPath };
		if (recursive)
		{
			for (int i = 0; i < list.Count; i++)
			{
				string[] directories = Directory.GetDirectories(list[i], "*", SearchOption.TopDirectoryOnly);
				foreach (string item in directories)
				{
					if (!excludedPaths.Contains(item))
					{
						list.Add(item);
					}
				}
			}
		}
		List<string> list2 = new List<string>();
		int count = list.Count;
		for (int k = 0; k < count; k++)
		{
			string[] files = Directory.GetFiles(list[k], extension, SearchOption.TopDirectoryOnly);
			list2.AddRange(files);
		}
		return list2.ToArray();
	}
}

}