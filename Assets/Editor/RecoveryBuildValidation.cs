using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
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
