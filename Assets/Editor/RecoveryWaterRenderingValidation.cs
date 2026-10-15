using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class RecoveryWaterRenderingValidation
{
    const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
    [Serializable] class Result
    {
        public bool completed, originalShader, originalNormal, originalMesh, renderPassMaterial;
        public string graphicsApi, failure;
        public int checkedPasses;
        public int changedPixels;
        public List<string> passes = new List<string>();
    }
    static RecoveryWaterRenderingValidation() { EditorApplication.update += Tick; }
    public static void Run()
    {
        SessionState.SetBool("RecoveryWaterRenderingValidation.Requested", true);
        RecoveryPlayValidation.RunLocalGame();
    }
    static void Tick()
    {
        bool liveRequest = File.Exists("CompilationChecks/water-live-validation.request");
        if ((!liveRequest && !SessionState.GetBool("RecoveryWaterRenderingValidation.Requested", false)) ||
            !EditorApplication.isPlaying || Time.time < 30 || !Player.LocalPlayer) return;
        SessionState.SetBool("RecoveryWaterRenderingValidation.Requested", false);
        if (liveRequest) File.Delete("CompilationChecks/water-live-validation.request");
        var result = new Result { graphicsApi = SystemInfo.graphicsDeviceType.ToString() };
        try
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<WaterManager>();
            if (!manager) throw new Exception("WaterManager missing.");
            // Allows a specifically requested live check after an editor script
            // reload, where an existing WaterManager may not receive Awake again.
            typeof(WaterManager).GetMethod("BindOriginalWaterRenderPasses", Fields).Invoke(manager, null);
            var material = (Material)typeof(WaterManager).GetField("_waterMat", Fields).GetValue(manager);
            result.originalShader = material.shader == RecoveryOriginalShaders.Get("Shader Graphs/WaterShader");
            result.originalNormal = material.GetTexture("_Normal_Bumps_Map") == RecoveryOriginalShaders.GetAsset<Texture2D>(2001);
            result.originalMesh = (Mesh)typeof(WaterManager).GetField("_waterMesh", Fields).GetValue(manager) == RecoveryOriginalShaders.GetAsset<Mesh>(2000);
            result.renderPassMaterial = true;
            foreach (var feature in Resources.FindObjectsOfTypeAll<RenderObjects>())
            {
                if (feature.settings.overrideMode != RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material) continue;
                var chosen = feature.settings.overrideMaterial;
                if (!chosen || !chosen.shader || chosen.shader.name != "Shader Graphs/WaterShader") continue;
                var pass = typeof(RenderObjects).GetField("renderObjectsPass", Fields).GetValue(feature);
                var passMaterial = (Material)pass.GetType().GetProperty("overrideMaterial", Fields).GetValue(pass);
                bool matches = chosen == material && passMaterial == material;
                result.passes.Add(feature.name + ": settings/cached-pass=" + matches + "; event=" + feature.settings.Event);
                result.renderPassMaterial &= matches;
                result.checkedPasses++;
            }
            if (!result.originalShader || !result.originalNormal || !result.originalMesh ||
                !result.renderPassMaterial || result.checkedPasses == 0)
                throw new Exception("Original water resource / actual cached render pass validation failed.");
            result.changedPixels = CaptureComparison(material);
            result.completed = true;
        }
        catch (Exception error) { result.failure = error.ToString(); Debug.LogException(error); }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/water-rendering-validation.json", JsonUtility.ToJson(result, true));
        if (!result.completed && Application.isBatchMode) EditorApplication.Exit(1);
    }

    static int CaptureComparison(Material restored)
    {
        var owner = new GameObject("Water comparison camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = owner.AddComponent<Camera>();
        camera.CopyFrom(Player.LocalPlayer.CurCam);
        camera.enabled = false;
        var position = Player.LocalPlayer.Transform.position;
        camera.transform.position = position + new Vector3(65, 7, 0);
        camera.transform.LookAt(position + new Vector3(100, -1, 35));
        var additional = camera.GetUniversalAdditionalCameraData();
        additional.renderPostProcessing = true;
        additional.requiresDepthTexture = true;
        var target = new RenderTexture(960, 540, 24);
        var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var features = new List<RenderObjects>();
        foreach (var feature in Resources.FindObjectsOfTypeAll<RenderObjects>())
            if (feature.settings.overrideMaterial == restored) features.Add(feature);
        try
        {
            camera.targetTexture = target;
            Color32[] before = null;
            int changed = 0;
            for (int mode = 0; mode < 2; mode++)
            {
                var material = mode == 0 ? AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Water.mat") : restored;
                foreach (var feature in features) { feature.settings.overrideMaterial = material; feature.Create(); }
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                File.WriteAllBytes("CompilationChecks/water-" + (mode == 0 ? "before" : "restored") + ".png", image.EncodeToPNG());
                var pixels = image.GetPixels32();
                if (mode == 0) before = pixels;
                else for (int i = 0; i < pixels.Length; i++)
                    if (Math.Abs(pixels[i].r - before[i].r) + Math.Abs(pixels[i].g - before[i].g) + Math.Abs(pixels[i].b - before[i].b) > 9) changed++;
            }
            return changed;
        }
        finally
        {
            foreach (var feature in features) { feature.settings.overrideMaterial = restored; feature.Create(); }
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
