using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RecoveryBundleShaderAudit
{
    [Serializable] public class Entry { public string bundle; public string[] shaders; }
    [Serializable] public class Report { public List<Entry> entries = new List<Entry>(); }
    public static void Run()
    {
        var report = new Report();
        string root = Path.Combine(Application.dataPath, "../ReferenceOnly/original-artifacts/How to Fish_Data/StreamingAssets/aa/StandaloneWindows64");
        foreach (var file in Directory.GetFiles(root, "*.bundle"))
        {
            var bundle = AssetBundle.LoadFromFile(file);
            if (bundle == null) continue;
            var names = new List<string>();
            foreach (var shader in bundle.LoadAllAssets<Shader>()) names.Add(shader.name);
            report.entries.Add(new Entry { bundle = Path.GetFileName(file), shaders = names.ToArray() });
            bundle.Unload(true);
        }
        File.WriteAllText("CompilationChecks/original-bundle-shaders.json", JsonUtility.ToJson(report,true));
    }
}
