using UnityEngine;

namespace Sirenix.OdinInspector {
public class TabGroupAttribute : PropertyAttribute
{
	public string name;

	public bool foldEverything;

	public TabGroupAttribute(string name, bool foldEverything = false)
	{
		this.foldEverything = foldEverything;
		this.name = name;
	}
}

}