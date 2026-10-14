using System.Runtime.CompilerServices;
using Unified.UniversalBlur.Runtime.CommandBuffer;
using Unified.UniversalBlur.Runtime.PassData;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unified.UniversalBlur.Runtime {
public static class BlurPasses
{
	public static void KawaseExecutePass<TPass, T>(TPass data, T cmd) where TPass : IPassData where T : IWrappedCommandBuffer
	{
		MaterialPropertyBlock materialPropertyBlock = data.GetMaterialPropertyBlock();
		materialPropertyBlock.Clear();
		materialPropertyBlock.SetVector(Constants.BlitScaleBiasId, Constants.DefaultBlitBias);
		BlurConfig blurConfig = data.GetBlurConfig();
		Texture colorSource = data.GetColorSource();
		Texture texture = data.GetSource();
		Texture texture2 = data.GetDestination();
		if (blurConfig.Iterations % 2 == 1)
		{
			Texture texture3 = texture2;
			Texture texture4 = texture;
			texture = texture3;
			texture2 = texture4;
		}
		BlitTexture(cmd, colorSource, texture, blurConfig, materialPropertyBlock, CalculateOffset(blurConfig, 0), 0);
		for (int i = 1; i < blurConfig.Iterations; i++)
		{
			float offset = CalculateOffset(blurConfig, i);
			BlitTexture(cmd, texture, texture2, blurConfig, materialPropertyBlock, offset, i - 1);
			Texture texture5 = texture2;
			Texture texture4 = texture;
			texture = texture5;
			texture2 = texture4;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float CalculateOffset(BlurConfig blurConfig, int iteration)
	{
		return (blurConfig.Offset + (float)iteration * blurConfig.Scale) / blurConfig.Downsample;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void BlitTexture<T>(T cmd, Texture sourceHandle, Texture destinationHandle, BlurConfig blurConfig, MaterialPropertyBlock mpb, float offset, int iteration) where T : IWrappedCommandBuffer
	{
		mpb.SetInt(Constants.IterationId, iteration);
		mpb.SetVector(Constants.BlurParamsId, new Vector4(blurConfig.Intensity, blurConfig.Scale, blurConfig.Downsample, blurConfig.Offset));
		mpb.SetTexture(Constants.BlitTextureId, sourceHandle);
		mpb.SetFloat(Constants.BlitMipLevelId, blurConfig.EnableMipMaps ? Mathf.Log(offset, 2f) : 0f);
		RenderTargetIdentifier rt = destinationHandle;
		cmd.SetRenderTarget(rt, 0, CubemapFace.Unknown, 0);
		Matrix4x4 identity = Matrix4x4.identity;
		Material material = blurConfig.Material;
		cmd.DrawProcedural(identity, material, 0, MeshTopology.Quads, 4, 1, mpb);
	}
}

}