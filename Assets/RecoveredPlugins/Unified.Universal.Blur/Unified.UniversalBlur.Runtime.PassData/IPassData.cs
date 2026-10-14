using UnityEngine;

namespace Unified.UniversalBlur.Runtime.PassData {
public interface IPassData
{
	BlurConfig GetBlurConfig();

	MaterialPropertyBlock GetMaterialPropertyBlock();

	Texture GetColorSource();

	Texture GetSource();

	Texture GetDestination();
}

}