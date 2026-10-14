using UnityEngine;

namespace Sirenix.OdinInspector {
public class ShowIfAttribute : PropertyAttribute
{
	public enum DisablingType
	{
		ReadOnly = 2,
		DontDraw
	}

	public string comparedPropertyName { get; private set; }

	public object comparedValue { get; private set; }

	public DisablingType disablingType { get; private set; }

	public ShowIfAttribute(string comparedPropertyName, object comparedValue, DisablingType disablingType = DisablingType.DontDraw)
	{
		this.comparedPropertyName = comparedPropertyName;
		this.comparedValue = comparedValue;
		this.disablingType = disablingType;
	}
}

}