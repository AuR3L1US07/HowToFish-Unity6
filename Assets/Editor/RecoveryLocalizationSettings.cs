using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using System.Linq;

public static class RecoveryLocalizationSettings
{
    [InitializeOnLoadMethod]
    static void WatchPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        RecoveryLocalizationCatalog.Initialize();
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
