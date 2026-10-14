using System;
using System.Text;
using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Strings
{
	private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static byte[] Buffer = new byte[1024];

	public static string MemberToPascalCase(this string txt)
	{
		if (txt.Length < 2)
		{
			Debug.LogError("Text '" + txt + "' is too short.");
			return string.Empty;
		}
		if (txt[0] != '_')
		{
			Debug.LogError("Text '" + txt + "' has the incorrect member prefix.");
			return string.Empty;
		}
		string text = txt[1].ToString().ToUpper();
		string text2 = ((txt.Length > 2) ? txt.Substring(2) : string.Empty);
		return text + text2;
	}

	public static string PascalCaseToMember(this string txt)
	{
		if (txt.Length < 1)
		{
			Debug.LogError("Text '" + txt + "' is too short.");
			return string.Empty;
		}
		string text = txt[0].ToString().ToLower();
		string text2 = ((txt.Length > 1) ? txt.Substring(1) : string.Empty);
		return "_" + text + text2;
	}

	public static string ReturnModifySuffix(string text, string suffix, bool addExtension)
	{
		if (text.Length > suffix.Length + 1)
		{
			if (addExtension)
			{
				if (!text.Substring(text.Length - suffix.Length).Contains(suffix, StringComparison.CurrentCultureIgnoreCase))
				{
					return text + suffix;
				}
				return text;
			}
			if (text.Substring(text.Length - suffix.Length).Contains(suffix, StringComparison.CurrentCultureIgnoreCase))
			{
				return text.Substring(0, text.Length - suffix.Length);
			}
			return text;
		}
		return text;
	}

	public static int ToBytes(this string value, ref byte[] buffer)
	{
		int length = value.Length;
		int maxByteCount = _encoding.GetMaxByteCount(length);
		if (buffer.Length < maxByteCount)
		{
			Array.Resize(ref buffer, maxByteCount * 2);
		}
		return _encoding.GetBytes(value, 0, length, buffer, 0);
	}

	public static byte[] ToBytesAllocated(this string value)
	{
		return Encoding.Unicode.GetBytes(value);
	}
}

}