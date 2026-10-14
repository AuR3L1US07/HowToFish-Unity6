using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime {
public class UniversalBlurFeature : ScriptableRendererFeature
{
	[Header("Blur Settings")]
	[Range(1f, 12f)]
	[SerializeField]
	private int iterations = 4;

	[Range(1f, 10f)]
	[SerializeField]
	private float downsample = 2f;

	[Tooltip("Enable mipmaps for more efficient blur")]
	[SerializeField]
	private bool enableMipMaps = true;

	[SerializeField]
	private float scale = 1f;

	[SerializeField]
	private float offset = 1f;

	[Space]
	[Header("Advanced Settings")]
	[SerializeField]
	private ScaleBlurWith scaleBlurWith = ScaleBlurWith.ScreenHeight;

	[SerializeField]
	private float scaleReferenceSize = 1080f;

	[Space]
	[SerializeField]
	private BlurType blurType;

	[Tooltip("For Overlay Canvas: AfterRenderingPostProcessing\n\nOther: BeforeRenderingTransparents (will hide transparents)")]
	[SerializeField]
	private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

	private float _intensity = 1f;

	[SerializeField]
	[HideInInspector]
	[Reload("Shaders/Blur.shader", ReloadAttribute.Package.Root)]
	private Shader shader;

	private Material _material;

	private UniversalBlurPass _blurPass;

	private float _renderScale;

	public float Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			_intensity = Mathf.Clamp(value, 0f, 1f);
		}
	}

	public override void Create()
	{
		_blurPass = new UniversalBlurPass();
		_blurPass.renderPassEvent = injectionPoint;
	}

	public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
	{
		base.OnCameraPreCull(renderer, in cameraData);
		_renderScale = cameraData.renderScale;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (!TrySetShadersAndMaterials())
		{
			Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, base.name);
		}
		else if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera)
		{
			_blurPass.DrawDefaultTexture();
		}
		else
		{
			BlurConfig blurConfig = GetBlurConfig(in renderingData);
			_blurPass.Setup(blurConfig);
			renderer.EnqueuePass(_blurPass);
		}
	}

	protected override void Dispose(bool disposing)
	{
		_blurPass?.Dispose();
		CoreUtils.Destroy(_material);
	}

	private bool TrySetShadersAndMaterials()
	{
		if (shader == null)
		{
			shader = Shader.Find("Unify/Internal/Blur");
		}
		if (_material == null && shader != null)
		{
			_material = CoreUtils.CreateEngineMaterial(shader);
		}
		return _material != null;
	}

	private BlurConfig GetBlurConfig(in RenderingData renderingData)
	{
		var (width, height) = GetTargetResolution(in renderingData);
		return new BlurConfig
		{
			Scale = CalculateScale(),
			Width = width,
			Height = height,
			Material = _material,
			Intensity = _intensity,
			Downsample = downsample,
			Offset = offset,
			BlurType = blurType,
			Iterations = iterations,
			EnableMipMaps = enableMipMaps
		};
	}

	private (int width, int height) GetTargetResolution(in RenderingData renderingData)
	{
		RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
		int item = Mathf.RoundToInt((float)cameraTargetDescriptor.width / downsample);
		int item2 = Mathf.RoundToInt((float)cameraTargetDescriptor.height / downsample);
		return (width: item, height: item2);
	}

	private float CalculateScale()
	{
		return scaleBlurWith switch
		{
			ScaleBlurWith.ScreenHeight => scale * ((float)Screen.height / scaleReferenceSize) * _renderScale, 
			ScaleBlurWith.ScreenWidth => scale * ((float)Screen.width / scaleReferenceSize) * _renderScale, 
			_ => scale, 
		};
	}
}

}