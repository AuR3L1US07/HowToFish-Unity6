using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit editor-only check. Uses the source shader asset, never Shader.Find or
// the compiled original-shader bundle, so a binary replacement cannot mask this check.
public static class RecoveryItemSurfaceValidation
{
    [Serializable] private class Result
    {
        public bool completed;
        public int cookingShadersPassed, skinShadersPassed, distinctPatternPairs;
        public string failure;
        public string limitation = "GPU functional reconstruction check; does not prove original lighting or graph-source equivalence.";
    }
    public static void Run()
    {
        var result = new Result();
        try
        {
            foreach (var name in new[] { "DefaultShader", "BoatShader", "CookableGlassShader" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Shader Graphs_" + name + ".shader");
                if (!shader || ShaderUtil.ShaderHasError(shader))
                    throw new Exception("Source shader missing or has errors: " + name);
                var material = new Material(shader);
                var palette = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
                try
                {
                    palette.wrapMode = TextureWrapMode.Repeat;
                    palette.filterMode = FilterMode.Point;
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                        palette.SetPixel(x, y, x < 32 ? new Color(.08f,.65f,.12f) : new Color(.85f,.07f,.55f));
                    palette.Apply();
                    material.SetTexture("_Colors", palette);
                    material.SetFloat("_Normal_Strength", 0);
                    if (name != "CookableGlassShader")
                    {
                        material.SetVector("_Color_Offset", Vector4.zero);
                        material.SetVector("_Color_Offset_2", new Vector4(.5f,0,0,0));
                        material.SetVector("_Skin_Smooth_Step", new Vector4(.3f,.7f,0,0));
                        material.SetVector("_Skin_UV_Scale", new Vector4(1,1,0,0));
                        material.SetFloat("_Skin_Noise_Scale", 5.7f);
                        material.SetFloat("_Use_Skin", 0);
                        var plain = Render(material);
                        material.SetFloat("_Use_Skin", 1);
                        string[] patterns = { "_SKIN_TYPE_GRADIENT_NOISE", "_SKIN_TYPE_CHECKERBOARD", "_SKIN_TYPE_VORONOI" };
                        Color[][] images = new Color[patterns.Length][];
                        for (int i = 0; i < patterns.Length; i++)
                        {
                            foreach (var keyword in patterns) material.DisableKeyword(keyword);
                            material.EnableKeyword(patterns[i]);
                            images[i] = Render(material);
                            RequireDifference(plain, images[i], name + " skin toggle " + patterns[i]);
                        }
                        for (int i = 0; i < images.Length; i++) for (int j = i+1; j < images.Length; j++)
                        {
                            RequireDifference(images[i], images[j], name + " pattern pair " + i + "/" + j);
                            result.distinctPatternPairs++;
                        }
                        result.skinShadersPassed++;
                    }
                    if (name != "BoatShader")
                    {
                        material.SetFloat("_Use_Skin", 0);
                        material.SetColor("_CookColor", new Color(.8f,.2f,.03f));
                        material.SetColor("_BurntColor", new Color(.01f,.01f,.01f));
                        material.SetFloat("_Cookness", 0); var raw = Render(material);
                        material.SetFloat("_Cookness", 1); var cooked = Render(material);
                        material.SetFloat("_Cookness", 2); var burnt = Render(material);
                        RequireDifference(raw, cooked, name + " raw/cooked");
                        RequireDifference(cooked, burnt, name + " cooked/burnt");
                        result.cookingShadersPassed++;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(palette); }
            }
            result.completed = true;
        }
        catch (Exception exception) { result.failure = exception.ToString(); Debug.LogException(exception); }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/item-surface-validation.json", JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.completed ? 0 : 1);
    }
    private static void RequireDifference(Color[] first, Color[] second, string label)
    {
        double difference = 0;
        for (int i = 0; i < first.Length; i++)
            difference += Mathf.Abs(first[i].r-second[i].r) + Mathf.Abs(first[i].g-second[i].g) + Mathf.Abs(first[i].b-second[i].b);
        if (difference / first.Length < .008) throw new Exception("GPU output did not change: " + label);
    }
    private static Color[] Render(Material material)
    {
        var mesh = new Mesh();
        var target = RenderTexture.GetTemporary(96,96,0,RenderTextureFormat.ARGB32);
        var readback = new Texture2D(96,96,TextureFormat.RGBA32,false);
        var commands = new CommandBuffer();
        var previous = RenderTexture.active;
        try
        {
            mesh.vertices = new[] { new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0) };
            mesh.normals = new[] { Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward };
            mesh.tangents = new[] { new Vector4(1,0,0,1),new Vector4(1,0,0,1),new Vector4(1,0,0,1),new Vector4(1,0,0,1) };
            mesh.uv = new[] { new Vector2(.1f,.1f),new Vector2(.1f,.1f),new Vector2(.1f,.1f),new Vector2(.1f,.1f) };
            mesh.uv2 = new[] { new Vector2(.8f,.2f),new Vector2(.8f,.2f),new Vector2(.8f,.2f),new Vector2(.8f,.2f) };
            mesh.uv3 = mesh.uv;
            mesh.uv4 = new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right };
            mesh.triangles = new[] { 0,1,2,0,2,3 };
            commands.SetRenderTarget(target);
            commands.ClearRenderTarget(true,true,Color.black);
            commands.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);
            commands.SetGlobalVector("_WorldSpaceCameraPos",new Vector4(0,0,3,1));
            commands.SetGlobalVector("_MainLightPosition",new Vector4(0,0,1,0));
            commands.SetGlobalVector("_MainLightColor",Vector4.one);
            commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0);
            Graphics.ExecuteCommandBuffer(commands);
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0,0,96,96),0,0); readback.Apply();
            return readback.GetPixels();
        }
        finally
        {
            RenderTexture.active = previous; commands.Release(); RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
