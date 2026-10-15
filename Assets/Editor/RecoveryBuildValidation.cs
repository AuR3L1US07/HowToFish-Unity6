using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class RecoveryBuildValidation
{
    [Serializable]
    private class Result
    {
        public string target;
        public string output;
        public string status;
        public int errors;
        public int warnings;
        public string[] scenes;
        public string failure;
        public string[] errorMessages;
    }

    // The Addressables player-build callback injects its regenerated runtime data
    // (catalog.bin/catalog.hash/settings.json and rebuilt script/locale bundles).
    // Stale copies shipped with the original game in StreamingAssets/aa conflict at
    // the Bee build-program level. Back them up instead of deleting so the original
    // files stay recoverable; the callback then copies a set consistent with the
    // recovered assemblies.
    static void MoveAddressablesCallbackConflicts()
    {
        const string aaRoot = "Assets/StreamingAssets/aa";
        const string callbackRoot = "Library/com.unity.addressables/aa/Windows";
        if(!Directory.Exists(callbackRoot) || !Directory.Exists(aaRoot)) return;
        var provided=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var file in Directory.GetFiles(callbackRoot,"*",SearchOption.AllDirectories))
            provided.Add(Path.GetFileName(file));
        var backup=Path.Combine("CompilationChecks","streaming-aa-backup",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
        Directory.CreateDirectory(backup);
        foreach(var file in Directory.GetFiles(aaRoot,"*",SearchOption.AllDirectories))
        {
            var name=Path.GetFileName(file);
            if(name.EndsWith(".meta",StringComparison.OrdinalIgnoreCase))
                name=name.Substring(0,name.Length-5);
            if(!provided.Contains(name)) continue;
            var relative=file.Substring(aaRoot.Length).TrimStart('\\','/');
            var destination=Path.Combine(backup,relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Move(file,destination);
        }
        AssetDatabase.Refresh();
    }

    public static void BuildWindows()
    {
        var result = new Result
        {
            target = "StandaloneWindows64",
            output = "Builds/Offline/HowToFish.exe",
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            status = "NotStarted"
        };
        Directory.CreateDirectory("CompilationChecks");
        try
        {
            RecoveryLocalizationSettings.Initialize();
            RecoveryLocalizationSettings.BuildCatalog();
            AssetDatabase.SaveAssets();
            // On a clean checkout the callback directory does not exist yet.
            // Generate it before resolving conflicts, rather than relying on a
            // previous failed player build to create its file inventory.
            AddressableAssetSettings.BuildPlayerContent(out var contentResult);
            if (contentResult == null || !string.IsNullOrEmpty(contentResult.Error))
                throw new InvalidOperationException("Addressables content build failed: " + contentResult?.Error);
            MoveAddressablesCallbackConflicts();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = result.scenes,
                locationPathName = result.output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            result.status = report.summary.result.ToString();
            result.errors = (int)report.summary.totalErrors;
            result.warnings = (int)report.summary.totalWarnings;
            result.errorMessages = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error || message.type == LogType.Exception)
                .Select(message => message.content).Distinct().ToArray();
        }
        catch (Exception exception)
        {
            result.status = "Exception";
            result.failure = exception.ToString();
            Debug.LogException(exception);
        }
        File.WriteAllText("CompilationChecks/offline-build-validation.json", JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.status == "Succeeded" ? 0 : 1);
    }
}
