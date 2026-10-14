using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RecoveryVisualBehaviorValidation
{
    [Serializable] private class Result
    {
        public string editorVersion;
        public int vfxEventsPassed, paletteChannelsPassed;
        public bool overlappingBurstsPreserved, shaderErrors;
        public string failure;
        public string limitation = "Functional reconstruction validation; original visual equivalence is not established.";
    }

    public static void Run()
    {
        var result = new Result { editorVersion = Application.unityVersion };
        var owner = new GameObject("VFX validation owner");
        RecoveryVfxParticles particles = null;
        try
        {
            owner.transform.position = new Vector3(200,300,400);
            owner.transform.rotation = Quaternion.Euler(30,40,50);
            particles = new RecoveryVfxParticles(owner.transform);
            string[] events = { "WaterSplash","WaterExplosion","LavaExplosion","VolcanoEruption",
                "LavaBarf","Poison","Angry","BossDeath","SmallBloodExplosion","DustExplosion","PufferfishDeath" };
            int blood = PlayerPrefs.GetInt("Blood",1);
            // The test should not change the saved preference.
            foreach (var name in events)
            {
                if (name == "SmallBloodExplosion" && blood == 0) continue;
                var position = new Vector3(11,12,13);
                particles.Play(name,position,Vector3.up);
                var child = owner.transform.Find("Recovered VFX - " + name);
                if (child == null) throw new Exception("Missing particle event: " + name);
                var system = child.GetComponent<ParticleSystem>();
                var buffer = new ParticleSystem.Particle[system.main.maxParticles];
                int count = system.GetParticles(buffer);
                if (count == 0) throw new Exception("Event emitted no particles: " + name);
                for (int i = 0; i < count; i++)
                    if (Vector3.Distance(buffer[i].position,position) > .001f)
                        throw new Exception("Particle event did not preserve world position: " + name);
                particles.Play(name,new Vector3(31,32,33),Vector3.up);
                int secondCount = system.GetParticles(buffer);
                if (secondCount <= count) throw new Exception("Second burst replaced the first: " + name);
                result.vfxEventsPassed++;
            }
            result.overlappingBurstsPreserved = true;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Shader Graphs_CharacterShader.shader");
            var particleShader = Resources.Load<Shader>("RecoveryVfxParticles");
            var maskShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Custom_DepthMaskShader.shader");
            result.shaderErrors = shader == null || particleShader == null || maskShader == null ||
                ShaderUtil.ShaderHasError(shader) || ShaderUtil.ShaderHasError(particleShader) || ShaderUtil.ShaderHasError(maskShader);
            string[] additional = { "WaterShader", "GlassShader", "FlagShader", "BettingBoxShader", "Grass", "LaserDotDecal", "LaserSight", "LavaballShader", "LavaShader", "LineShader", "URPDecal", "WindyDecorations", "WindyDefault" };
            foreach (var name in additional)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Shader Graphs_" + name + ".shader");
                result.shaderErrors |= asset == null || ShaderUtil.ShaderHasError(asset);
            }
            var blur = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Unify_Internal_Blur.shader");
            var voxel = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Hidden_VoxelizeShader.shader");
            result.shaderErrors |= blur == null || voxel == null || ShaderUtil.ShaderHasError(blur) || ShaderUtil.ShaderHasError(voxel);
            if (result.shaderErrors)
            {
                var messages = new System.Text.StringBuilder();
                foreach (var path in new[] { "Assets/Shader/Shader Graphs_CharacterShader.shader", "Assets/Resources/RecoveryVfxParticles.shader", "Assets/Shader/Custom_DepthMaskShader.shader" })
                {
                    var checkedShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (!checkedShader) { messages.AppendLine("Missing: " + path); continue; }
                    foreach (var message in ShaderUtil.GetShaderMessages(checkedShader)) messages.AppendLine(path + ": " + message.message);
                }
                foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shader", "Assets/Resources" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var checkedShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (checkedShader && ShaderUtil.ShaderHasError(checkedShader))
                        foreach (var message in ShaderUtil.GetShaderMessages(checkedShader)) messages.AppendLine(path + ": " + message.message);
                }
                throw new Exception("Recovered shader missing or has compiler errors. " + messages);
            }
            string[] properties = { "_SkinColor","_PrimaryColor","_SecondColor","_ThirdColor" };
            for (int i = 0; i < properties.Length; i++)
            {
                Color first = RenderPalette(shader, properties[i], .2f + i * .2f,new Vector4(.015625f,.703125f,1,0));
                Color second = RenderPalette(shader, properties[i], .2f + i * .2f,new Vector4(.484375f,.703125f,1,0));
                if (Mathf.Abs(first.r-second.r)+Mathf.Abs(first.g-second.g)+Mathf.Abs(first.b-second.b) < .02f)
                    throw new Exception("Palette channel produced identical colors: " + properties[i]);
                result.paletteChannelsPassed++;
            }
        }
        catch (Exception exception) { result.failure = exception.ToString(); Debug.LogException(exception); }
        finally
        {
            if (particles != null) particles.Dispose();
            UnityEngine.Object.DestroyImmediate(owner);
            Directory.CreateDirectory("CompilationChecks");
            File.WriteAllText("CompilationChecks/visual-behavior-validation.json",JsonUtility.ToJson(result,true));
        }
        EditorApplication.Exit(result.failure == null ? 0 : 1);
    }

    private static Color RenderPalette(Shader shader, string property, float mask, Vector4 colorUV)
    {
        var material = new Material(shader);
        var mesh = new Mesh();
        var target = RenderTexture.GetTemporary(32,32,0,RenderTextureFormat.ARGB32);
        var readback = new Texture2D(32,32,TextureFormat.RGBA32,false);
        var commands = new CommandBuffer();
        var previous = RenderTexture.active;
        try
        {
            material.SetTexture("_Colors",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Texture2D/Colors.png"));
            material.SetVector(property,colorUV);
            mesh.vertices = new[] { new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0) };
            mesh.normals = new[] { Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward };
            mesh.uv = new[] { Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero };
            mesh.uv3 = new[] { new Vector2(mask,0),new Vector2(mask,0),new Vector2(mask,0),new Vector2(mask,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3 };
            commands.SetRenderTarget(target);
            commands.ClearRenderTarget(true,true,Color.black);
            commands.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);
            commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0);
            Graphics.ExecuteCommandBuffer(commands);
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0,0,32,32),0,0);
            readback.Apply();
            return readback.GetPixel(16,16);
        }
        finally
        {
            RenderTexture.active = previous;
            commands.Release();
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(readback);
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
