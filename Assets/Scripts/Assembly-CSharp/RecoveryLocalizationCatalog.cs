using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

// Keep recovered translations available without the original Addressables build catalog.
public class RecoveryLocalizationCatalog : ScriptableObject
{
    public LocalizationSettings settings;
    public Locale[] locales;
    public StringTable[] tables;

    [System.Serializable]
    sealed class LocalLocales : ILocalesProvider
    {
        [SerializeField] List<Locale> localLocales = new List<Locale>();
        public List<Locale> Locales => localLocales;
        public Locale GetLocale(LocaleIdentifier id) => Locales.Find(l => l.Identifier == id);
        public void AddLocale(Locale locale) { if (locale && GetLocale(locale.Identifier) == null) Locales.Add(locale); }
        public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
    }
    [System.Serializable]
    sealed class LocalStringDatabase : LocalizedStringDatabase
    {
        [SerializeField] StringTable[] localTables = System.Array.Empty<StringTable>();
        public LocalStringDatabase() { }
        public LocalStringDatabase(StringTable[] tables) { localTables = tables; }
        public override AsyncOperationHandle<StringTable> GetTableAsync(TableReference reference, Locale locale = null)
        {
            if (!locale) locale = LocalizationSettings.SelectedLocale;
            StringTable english = null;
            foreach (var table in localTables)
            {
                bool matches = reference.ReferenceType == TableReference.Type.Guid
                    ? table.SharedData.TableCollectionNameGuid == reference.TableCollectionNameGuid
                    : table.TableCollectionName == reference.TableCollectionName;
                if (!matches) continue;
                if (locale && table.LocaleIdentifier == locale.Identifier)
                    return Addressables.ResourceManager.CreateCompletedOperation(table, null);
                if (table.LocaleIdentifier.Code == "en") english = table;
            }
            return Addressables.ResourceManager.CreateCompletedOperation(english, english ? null : "Recovered translation table not found: " + reference);
        }
    }

    public void Configure(LocalizationSettings target)
    {
        LocalizationSettings.Instance = target;
        var provider = new LocalLocales();
        foreach (var locale in locales) provider.AddLocale(locale);
        LocalizationSettings.AvailableLocales = provider;
        LocalizationSettings.PreloadBehavior = PreloadBehavior.NoPreloading;
        LocalizationSettings.StringDatabase = new LocalStringDatabase(tables);
        LocalizationSettings.SelectedLocale = provider.GetLocale(new LocaleIdentifier("en")) ?? provider.Locales[0];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var catalog = Resources.Load<RecoveryLocalizationCatalog>("RecoveryLocalizationCatalog");
        if (!catalog) return;
        catalog.Configure(catalog.settings);
        Debug.Log("RECOVERY_LOCALIZATION_READY locales=" + catalog.locales.Length + " tables=" + catalog.tables.Length);
    }
}
