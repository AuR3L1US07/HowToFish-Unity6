using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using System.Linq;

[InitializeOnLoad]
public static class RecoveryLocalizationSettings
{
    static RecoveryLocalizationSettings()
    {
        // Play-mode domain reload can enable localized labels before runtime callbacks.
        // Avoid loading assets during the initial editor import, which initializes editor overlays.
        if(!EditorApplication.isPlayingOrWillChangePlaymode) return;
        var settings=AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if(settings) LocalizationSettings.Instance=settings;
    }
    [InitializeOnLoadMethod]
    static void WatchPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode && state != PlayModeStateChange.ExitingEditMode) return;
        RecoveryLocalizationCatalog.Initialize();
        if (state == PlayModeStateChange.ExitingEditMode) return;
        foreach (var localized in Object.FindObjectsByType<UnityEngine.Localization.Components.LocalizeStringEvent>(FindObjectsSortMode.None))
            localized.RefreshString();
    }
    [InitializeOnLoadMethod]
    public static void Initialize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if (settings == null) return;
        LocalizationSettings.Instance = settings;
        var catalog = AssetDatabase.LoadAssetAtPath<RecoveryLocalizationCatalog>("Assets/Resources/RecoveryLocalizationCatalog.asset");
        if (catalog != null)
        {
            catalog.Configure(settings);
            EditorUtility.SetDirty(settings);
        }
        EditorApplication.delayCall += BuildCatalog;
        EditorApplication.delayCall += RecoveryLocalizationCatalog.Initialize;
        EditorBuildSettings.AddConfigObject("com.unity.localization.settings", settings, true);
        var preloaded = new List<Object>(UnityEditor.PlayerSettings.GetPreloadedAssets());
        if (!preloaded.Contains(settings))
        {
            preloaded.Add(settings);
            UnityEditor.PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }
    }
    public static void BuildCatalog()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string path = "Assets/Resources/RecoveryLocalizationCatalog.asset";
        if (AssetDatabase.LoadAssetAtPath<RecoveryLocalizationCatalog>(path)) return;
        var catalog = ScriptableObject.CreateInstance<RecoveryLocalizationCatalog>();
        catalog.settings = LocalizationSettings.Instance;
        var allLocales = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Localization/Locales" })
            .Select(g => AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(g))).Where(l => l).ToArray();
        string[] order = { "en", "sv", "zh-CN", "zh-TW", "fr", "de", "it", "ja", "ko", "pl", "pt-BR", "ru", "es-MX", "es-ES", "tr", "uk" };
        catalog.locales = order.Select(code => allLocales.FirstOrDefault(l => l.Identifier.Code == code)).Where(l => l).ToArray();
        catalog.tables = AssetDatabase.FindAssets("t:StringTable", new[] { "Assets/Localization/Locales" })
            .Select(g => AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(g))).Where(t => t).ToArray();
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
    }
}
