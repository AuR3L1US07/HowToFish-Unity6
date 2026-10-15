using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicitly requested acceptance fixture. Never runs during ordinary play.
[InitializeOnLoad]
public static class RecoverySettingsClothingAcceptance
{
    const string Request = "RecoverySettingsClothingAcceptance.Requested";
    const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    [Serializable] public class Check { public string name; public bool passed; public string detail; }
    [Serializable] public class Report { public bool completed; public List<Check> checks = new List<Check>(); }
    static RecoverySettingsClothingAcceptance() { EditorApplication.update += Tick; }
    [MenuItem("Recovery/Validate Settings And Clothing")]
    public static void Run() { SessionState.SetBool(Request, true); RecoveryPlayValidation.RunLocalGame(); }
    static void Tick()
    {
        if (!SessionState.GetBool(Request, false) || !EditorApplication.isPlaying || Time.time < 46 || !Player.LocalPlayer) return;
        SessionState.SetBool(Request, false);
        Validate();
    }
    static void Validate()
    {
        var report = new Report();
        Action<string, bool, string> check = (n, p, d) => report.checks.Add(new Check { name = n, passed = p, detail = d });
        try
        {
            if (SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation", StringComparison.Ordinal))
                throw new InvalidOperationException("Dedicated validation world required.");
            Settings(check);
            Clothing(check);
            report.completed = true;
        }
        catch (Exception e) { check("exception", false, e.ToString()); }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/settings-clothing-acceptance.json", JsonUtility.ToJson(report, true));
    }
    static void Settings(Action<string, bool, string> check)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<ButtonManager>(FindObjectsInactive.Include);
        if (!manager) throw new InvalidOperationException("ButtonManager missing.");
        string[] keys = { "HoldToAttack", "HoldToSprint", "HoldToAim" };
        string[] fields = { "_holdToAttackToggle", "_holdToSprintToggle", "_holdToAimToggle" };
        Func<bool>[] getters = { () => PlayerSettings.HoldToAttack, () => PlayerSettings.HoldToSprint, () => PlayerSettings.HoldToAim };
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            bool existed = PlayerPrefs.HasKey(key), oldRuntime = getters[i]();
            int oldValue = PlayerPrefs.GetInt(key);
            var toggle = (Toggle)typeof(ButtonManager).GetField(fields[i], PrivateInstance).GetValue(manager);
            bool oldUi = toggle.isOn;
            try
            {
                toggle.SetIsOnWithoutNotify(!oldRuntime);
                typeof(ButtonManager).GetMethod(key + "Toggle").Invoke(manager, null);
                PlayerPrefs.Save();
                check(key + "-callback-save", getters[i]() == !oldRuntime && PlayerPrefs.GetInt(key, -1) == (!oldRuntime ? 1 : 0),
                    "Original UI callback changed runtime behavior and persisted preference; separate-process reload is not asserted here.");
            }
            finally
            {
                toggle.SetIsOnWithoutNotify(oldRuntime);
                typeof(ButtonManager).GetMethod(key + "Toggle").Invoke(manager, null);
                toggle.SetIsOnWithoutNotify(oldUi);
                if (existed) PlayerPrefs.SetInt(key, oldValue); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
    static void Clothing(Action<string, bool, string> check)
    {
        var paths = new Dictionary<FieldInfo, object>();
        var state = new Dictionary<FieldInfo, object>();
        var meshes = new Dictionary<SkinnedMeshRenderer, Mesh>();
        var local = UnityEngine.Object.FindFirstObjectByType<LocalSkin>(FindObjectsInactive.Include);
        var save = UnityEngine.Object.FindFirstObjectByType<SaveManager>(FindObjectsInactive.Include);
        if (!local || !save) throw new InvalidOperationException("Customization instances missing.");
        foreach (var f in typeof(LocalSkin).GetFields(PrivateStatic)) state[f] = f.GetValue(null);
        var savedLocal = typeof(SaveManager).GetField("_curLocalSave", PrivateStatic);
        object oldSave = savedLocal.GetValue(null);
        var seenHat = (bool[])SkinManager.SeenHats.Clone();
        var seenOutfit = (bool[])SkinManager.SeenOutfits.Clone();
        var seenAccessory = (bool[])SkinManager.SeenAccesories.Clone();
        foreach (var renderer in local.GetComponentsInChildren<SkinnedMeshRenderer>(true)) meshes[renderer] = renderer.sharedMesh;
        try
        {
            string path = Path.GetFullPath("CompilationChecks/settings-clothing-state/local.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            foreach (string name in new[] { "_fullLocalSavePath", "_localBackupPath", "_localTempPath" })
            {
                var field = typeof(SaveSystem).GetField(name, PrivateStatic);
                paths[field] = field.GetValue(null);
                field.SetValue(null, path + (name == "_fullLocalSavePath" ? "" : name == "_localBackupPath" ? ".backup" : ".tmp"));
            }
            var types = new[] { PlayerSkinType.Hat, PlayerSkinType.Outfit, PlayerSkinType.Accessory };
            var before = new[] { LocalSkin.LocalHatIndex, LocalSkin.LocalOutfitIndex, LocalSkin.LocalAccessoryIndex };
            for (int i = 0; i < types.Length; i++) LocalSkin.ChangeMesh(1, types[i]);
            var selected = new[] { LocalSkin.LocalHatIndex, LocalSkin.LocalOutfitIndex, LocalSkin.LocalAccessoryIndex };
            for (int i = 0; i < types.Length; i++)
                check(types[i] + "-selection-changed", selected[i] != before[i], "Original ChangeMesh cycles currently unlocked meshes; no unlock flags assigned.");
            var colors = (PlayerSkinType[])Enum.GetValues(typeof(PlayerSkinType));
            foreach (var type in colors) LocalSkin.SetLocalColor(new Vector3(.27f + (int)type * .01f, .43f, 1f), type, false, false);
            SaveManager.SaveLocal();
            var disk = JsonUtility.FromJson<SaveManager.LocalSaveObject>(SaveSystem.LoadLocal());
            check("clothing-indices-on-disk", disk.LocalHatIndex == selected[0] && disk.LocalOutfitIndex == selected[1] && disk.LocalAccessoryIndex == selected[2], "Real SaveLocal/LoadLocal file round trip in dedicated directory.");
            // Return to the first selectable entry before exercising the original load path.
            for (int i = 0; i < types.Length; i++)
            {
                int guard = 0;
                while ((i == 0 ? LocalSkin.LocalHatIndex : i == 1 ? LocalSkin.LocalOutfitIndex : LocalSkin.LocalAccessoryIndex) != 0 && guard++ < 255)
                    LocalSkin.ChangeMesh(1, types[i]);
            }
            foreach (var type in colors) LocalSkin.SetLocalColor(Vector3.zero, type, false, false);
            typeof(SaveManager).GetMethod("LoadLocal", PrivateInstance).Invoke(save, null);
            check("clothing-load-selection", LocalSkin.LocalHatIndex == selected[0] && LocalSkin.LocalOutfitIndex == selected[1] && LocalSkin.LocalAccessoryIndex == selected[2], "Original private LoadLocal invoked after clearing selection; verifies actual restoration, not JSON fields only.");
            string[] rendererFields = { "_hatRenderer", "_outfitRenderer", "_accessoryRenderer" };
            Mesh[] expectedMeshes = { SkinManager.GetHat(selected[0]), SkinManager.GetOutfit(selected[1]), SkinManager.GetAccessory(selected[2]) };
            for (int i = 0; i < rendererFields.Length; i++)
            {
                var renderer = (SkinnedMeshRenderer)typeof(LocalSkin).GetField(rendererFields[i], PrivateInstance).GetValue(local);
                check(types[i] + "-loaded-renderer-mesh", renderer && renderer.sharedMesh == expectedMeshes[i], "Selected original clothing mesh must be assigned to preview renderer.");
            }
            foreach (var type in colors)
                check(type + "-load-color", LocalSkin.GetUV(type) == new Vector3(.27f + (int)type * .01f, .43f, 1f), "Color restored by original local-save loader.");
            TraverseAllUnlocked(local, save, check);
        }
        finally
        {
            savedLocal.SetValue(null, oldSave);
            foreach (var pair in state) if (!pair.Key.IsInitOnly) pair.Key.SetValue(null, pair.Value);
            foreach (var pair in meshes) if (pair.Key) pair.Key.sharedMesh = pair.Value;
            SkinManager.SetSeenClothes(seenHat, seenOutfit, seenAccessory);
            foreach (var pair in paths) pair.Key.SetValue(null, pair.Value);
            typeof(LocalSkin).GetMethod("UpdatePreview", PrivateInstance).Invoke(local, null);
        }
    }
    static byte Index(PlayerSkinType type)
    {
        return type == PlayerSkinType.Hat ? LocalSkin.LocalHatIndex : type == PlayerSkinType.Outfit ? LocalSkin.LocalOutfitIndex : LocalSkin.LocalAccessoryIndex;
    }
    static void TraverseAllUnlocked(LocalSkin local, SaveManager save, Action<string, bool, string> check)
    {
        var load = typeof(SaveManager).GetMethod("LoadLocal", PrivateInstance);
        foreach (var type in new[] { PlayerSkinType.Hat, PlayerSkinType.Outfit, PlayerSkinType.Accessory })
        {
            var indices = new List<byte>();
            for (int guard = 0; guard < 256; guard++)
            {
                byte current = Index(type);
                if (indices.Contains(current)) break;
                indices.Add(current);
                LocalSkin.ChangeMesh(1, type);
            }
            check(type + "-unlocked-cycle", indices.Count > 1, "Discovered actual selectable indices by original ChangeMesh: " + string.Join(",", indices));
            string field = type == PlayerSkinType.Hat ? "_hatRenderer" : type == PlayerSkinType.Outfit ? "_outfitRenderer" : "_accessoryRenderer";
            var renderer = (SkinnedMeshRenderer)typeof(LocalSkin).GetField(field, PrivateInstance).GetValue(local);
            foreach (byte target in indices)
            {
                int guard = 0;
                while (Index(type) != target && guard++ < 256) LocalSkin.ChangeMesh(1, type);
                if (Index(type) != target) throw new InvalidOperationException("Could not return to unlocked " + type + " " + target);
                Mesh expected = renderer.sharedMesh;
                SaveManager.SaveLocal();
                LocalSkin.ChangeMesh(1, type);
                byte changedFrom = Index(type);
                load.Invoke(save, null);
                check(type + "-restore-index-" + target, Index(type) == target && renderer.sharedMesh == expected,
                    "Saved absolute index=" + target + "; changed selection=" + changedFrom + "; loaded=" + Index(type) + "; original saved mesh match=" + (renderer.sharedMesh == expected));
                byte beforeReload = Index(type);
                load.Invoke(save, null);
                check(type + "-idempotent-index-" + target, Index(type) == target && renderer.sharedMesh == expected,
                    "Repeated original LoadLocal without modifying file; before=" + beforeReload + "; after=" + Index(type) + "; expected saved=" + target);
            }
        }
    }
}
