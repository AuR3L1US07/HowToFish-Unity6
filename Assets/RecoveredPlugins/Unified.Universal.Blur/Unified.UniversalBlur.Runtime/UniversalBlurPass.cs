using System;
using Unified.UniversalBlur.Runtime.CommandBuffer;
using Unified.UniversalBlur.Runtime.PassData;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime {
internal class UniversalBlurPass : ScriptableRenderPass, IDisposable
{
	private const string k_PassName = "Universal Blur";

	private const string k_BlurTextureSourceName = "Universal Blur - Blur Source";

	private const string k_BlurTextureDestinationName = "Universal Blur - Blur Destination";

	private readonly ProfilingSampler _profilingSampler;

	private readonly MaterialPropertyBlock _propertyBlock;

	private BlurConfig _blurConfig;

	private RTHandle _sourceRT;

	private RTHandle _destinationRT;

	public UniversalBlurPass()
	{
		_profilingSampler = new ProfilingSampler("Universal Blur");
		_propertyBlock = new MaterialPropertyBlock();
	}

	public void Setup(BlurConfig blurConfig)
	{
		_blurConfig = blurConfig;
	}

	public void Dispose()
	{
	}

	public void DrawDefaultTexture()
	{
		Shader.SetGlobalTexture(Constants.GlobalFullScreenBlurTextureId, Texture2D.linearGrayTexture);
	}

	private RenderTextureDescriptor GetDescriptor()
	{
		RenderTextureDescriptor result = new RenderTextureDescriptor(_blurConfig.Width, _blurConfig.Height, GraphicsFormat.B10G11R11_UFloatPack32, 0);
		result.useMipMap = _blurConfig.EnableMipMaps;
		result.autoGenerateMips = _blurConfig.EnableMipMaps;
		return result;
	}

	public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
	{
		UniversalResourceData universalResourceData = frameData.Get<UniversalResourceData>();
		if (universalResourceData.isActiveTargetBackBuffer)
		{
			Debug.LogError("Skipping render pass. UniversalBlurPass requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
			return;
		}
		TextureHandle activeColorTexture = universalResourceData.activeColorTexture;
		TextureDesc desc = new TextureDesc(GetDescriptor());
		desc.name = "Universal Blur - Blur Source";
		TextureHandle source = renderGraph.CreateTexture(in desc);
		desc.name = "Universal Blur - Blur Destination";
		TextureHandle destination = renderGraph.CreateTexture(in desc);
		RenderGraphPassData passData;
		using IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = renderGraph.AddUnsafePass<RenderGraphPassData>("Universal Blur", out passData, _profilingSampler, "C:\\Users\\Mrhot\\Documents\\AAAUnity\\FishGameAgain\\Assets\\Art\\UnifiedBlur-0.7.3\\Runtime\\UniversalBlurPass.cs", 115);
		passData.ColorSource = activeColorTexture;
		passData.Source = source;
		passData.Destination = destination;
		passData.MaterialPropertyBlock = _propertyBlock;
		passData.BlurConfig = _blurConfig;
		unsafeRenderGraphBuilder.AllowPassCulling(value: false);
		unsafeRenderGraphBuilder.UseTexture(in source, AccessFlags.ReadWrite);
		unsafeRenderGraphBuilder.UseTexture(in destination, AccessFlags.ReadWrite);
		unsafeRenderGraphBuilder.SetGlobalTextureAfterPass(in destination, Constants.GlobalFullScreenBlurTextureId);
		unsafeRenderGraphBuilder.SetRenderFunc(delegate(RenderGraphPassData data, UnsafeGraphContext ctx)
		{
			BlurPasses.KawaseExecutePass(data, new WrappedUnsafeCommandBuffer(ctx.cmd));
		});
	}
}

}