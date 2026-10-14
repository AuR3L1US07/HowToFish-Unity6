using System;
using System.IO;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Disks
{
	public static void WriteToFile(string text, string path, bool formatPath = true)
	{
		if (formatPath)
		{
			path = FormatPlatformPath(path);
		}
		if (path == string.Empty)
		{
			Debug.LogError("Path cannot be null.");
			return;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			using FileStream stream = new FileStream(path, FileMode.Create);
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(text);
		}
		catch (Exception ex)
		{
			Debug.LogError("An error occured during a file write. Error: " + ex.Message + " " + Environment.NewLine + " File path: " + path + " " + Environment.NewLine + " Text: " + text);
		}
	}

	public static string FormatPlatformPath(string path)
	{
		if (path == string.Empty)
		{
			Debug.LogError("Path cannot be empty.");
			return string.Empty;
		}
		string text = string.Empty;
		string[] array = path.Split(Path.DirectorySeparatorChar);
		for (int i = 0; i < array.Length; i++)
		{
			if (array.Length == 1)
			{
				text = array[i];
				break;
			}
			text = Path.Combine(text, array[i]);
		}
		return text;
	}
}

}