using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Companion to the local-game smoke test. These checks deliberately distinguish
// system state from the input-driven fishing / quest completion acceptance tests.
[InitializeOnLoad]
public static class RecoveryOfflineSystemsValidation
{
    [Serializable] public class Check { public string name; public bool passed; public string detail; }
    [Serializable] public class Report
    {
        public bool completed;
        public List<Check> checks = new List<Check>();
        public List<string> pending = new List<string> {
            "Input-driven cast, bite, reel, catch and pickup",
            "Inventory pickup, switching slots, dropping and reloading",
            "NPC delivery, quest reward and island progression",
            "Quit game and load saved world in a new session"
        };
    }
    static bool ran;
    static RecoveryOfflineSystemsValidation() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) { ran = false; return; }
        if (ran || !SessionState.GetBool("RecoveryPlayValidation.LocalGame", false) || Time.time < 30) return;
        if (Player.LocalPlayer == null || MainMenuManager.IsInMenu) return;
        ran = true;
        Validate();
    }
    [MenuItem("Recovery/Validate Current Offline Systems")]
    public static void Validate()
    {
        var report = new Report();
        Action<string, bool, string> check = (name, passed, detail) => report.checks.Add(new Check { name = name, passed = passed, detail = detail });
        try
        {
            var player = Player.LocalPlayer;
            if (!EditorApplication.isPlaying || player == null || SaveManager.CurServerSave == null)
                throw new InvalidOperationException("An initialized local game is required.");
            check("offline-save", !SaveManager.CurServerSave.UseSteam, SaveManager.CurServerSave.Name);
            check("economy-server", MoneyManager.Instance && MoneyManager.Instance.IsServerInitialized, "Authority must be initialized before transactions.");
            if (MoneyManager.Instance && MoneyManager.Instance.IsServerInitialized)
            {
                int original = MoneyManager.Instance._money.Value;
                // Restore authority even when sound/UI code throws. Do not leave test money in a save.
                try
                {
                    MoneyManager.AddMoney(17, player);
                    check("money-credit", MoneyManager.Instance._money.Value == original + 17, "Expected original balance + 17.");
                    MoneyManager.RemoveMoney(17, player);
                    check("money-debit", MoneyManager.Instance._money.Value == original, "Expected original balance after matching debit.");
                }
                finally { MoneyManager.Instance._money.Value = original; }
                check("money-client-sync", MoneyManager.Money == original, "Displayed balance must match authority.");
            }
            int quests = 0;
            foreach (var npc in UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
            {
                if (npc.Quests == null) { check("npc-quests", false, npc.name + " has no quest list."); continue; }
                foreach (var quest in npc.Quests)
                {
                    quests++;
                    check("quest-asset", quest != null, npc.name);
                    if (quest == null) continue;
                    check("quest-lines", quest.Lines != null && quest.Lines.Length > 0, quest.name);
                    if (quest.QuestItems != null)
                        foreach (var item in quest.QuestItems) check("quest-item", item != null, quest.name);
                }
            }
            check("quests-present", quests > 0, "Loaded quest count: " + quests + ". Resource checks do not prove completion.");
            check("fishing-content", GameInfo.AllCreatureCount > 0 && GameInfo.AllBaits != null && GameInfo.AllBaits.Count > 0,
                "Creature count: " + GameInfo.AllCreatureCount + ". This does not prove casting/catching.");
            SaveManager.SaveServer(true);
            var expected = SaveManager.CurServerSave;
            SaveManager.ServerSaveObject loaded = null;
            foreach (var json in SaveSystem.LoadAllServers())
            {
                var candidate = JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                if (candidate != null && candidate.Name == expected.Name) loaded = candidate;
            }
            check("save-deserialized", loaded != null, expected.Name);
            if (loaded != null)
            {
                check("save-world-state", loaded.Money == MoneyManager.Money && loaded.Euro == MoneyManager.Euro &&
                    loaded.MaxIsland == expected.MaxIsland && loaded.SpawnedIsland == expected.SpawnedIsland &&
                    loaded.UnlockedBoat == expected.UnlockedBoat && loaded.UnlockedGrill == expected.UnlockedGrill &&
                    loaded.LastSave == expected.LastSave, "Currency, islands, unlocks and save timestamp compared.");
                var savedPlayer = loaded.Players == null ? null : loaded.Players.Find(p => p.SteamID == player.SteamID);
                check("save-player", savedPlayer != null, "Local player must be serialized.");
                if (savedPlayer != null)
                {
                    int actualItems = 0;
                    foreach (var entry in player.Inventory._items)
                    {
                        if (!entry.Value) continue;
                        actualItems++;
                        var savedItem = savedPlayer.InventoryItems == null ? null : savedPlayer.InventoryItems.Find(i => i.InventorySlot == entry.Key);
                        check("save-inventory-item", savedItem != null && savedItem.Exists && savedItem.ItemID == entry.Value.ID,
                            "Slot " + entry.Key + ", item " + entry.Value.ID);
                    }
                    check("save-inventory-count", savedPlayer.InventoryItems != null && savedPlayer.InventoryItems.Count == actualItems,
                        "Actual item count: " + actualItems + ". Empty inventory is not a pickup test.");
                    check("save-upgrades", savedPlayer.ExtraSlots == player.Inventory.ExtraSlots && savedPlayer.WishboneIndex == player.Inventory.WishboneIndex,
                        "Pocket and wishbone levels compared.");
                    bool baitsMatch = savedPlayer.OwnedBaits != null && savedPlayer.OwnedBaits.Count == player.Inventory._ownedBaits.Count;
                    if (baitsMatch) for (int i = 0; i < savedPlayer.OwnedBaits.Count; i++) baitsMatch &= savedPlayer.OwnedBaits[i] == player.Inventory._ownedBaits[i];
                    check("save-baits", baitsMatch, "Every bait quantity compared.");
                }
            }
            report.completed = true;
        }
        catch (Exception error) { check("exception", false, error.ToString()); }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/offline-systems-validation.json", JsonUtility.ToJson(report, true));
        Debug.Log("RECOVERY_OFFLINE_SYSTEMS_VALIDATION_FINISHED checks=" + report.checks.Count);
    }
}
