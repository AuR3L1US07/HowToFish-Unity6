using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RecoveryRenderValidation
{
    [Serializable] public class Result { public string editorVersion; public bool globalSettingsReady; public bool pipelineCreated; public string scene; public int missingScripts; public string failure; }
    public static void Run()
    {
        var result = new Result { editorVersion = Application.unityVersion };
        try
        {
            RecoveryLocalizationSettings.Initialize();
            var globalType = typeof(UniversalRenderPipeline).Assembly.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings", true);
            var ensure = globalType.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);
            var global = ensure.Invoke(null, new object[] { true });
            result.globalSettingsReady = global != null;
            AssetDatabase.SaveAssets();
            var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) throw new Exception("Default URP asset is missing.");
            var create = typeof(UniversalRenderPipelineAsset).GetMethod("CreatePipeline", BindingFlags.Instance | BindingFlags.NonPublic);
            var pipeline = create.Invoke(asset, null) as RenderPipeline;
            result.pipelineCreated = pipeline != null;
            if (!result.pipelineCreated) throw new Exception("URP returned a null pipeline.");
            foreach (var configuredScene in EditorBuildSettings.scenes)
            {
                if (!configuredScene.enabled || !File.Exists(configuredScene.path)) continue;
                var scene = EditorSceneManager.OpenScene(configuredScene.path, OpenSceneMode.Single);
                result.scene = configuredScene.path;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                        result.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                break;
            }
            Debug.Log("RECOVERY_RENDER_VALIDATION_OK " + JsonUtility.ToJson(result));
        }
        catch(Exception exception)
        {
            result.failure = exception.ToString();
            Debug.LogException(exception);
        }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/render-runtime-validation.json", JsonUtility.ToJson(result,true));
        EditorApplication.Exit(result.failure == null && result.pipelineCreated && result.missingScripts == 0 ? 0 : 1);
    }
}
