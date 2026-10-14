using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// An opt-in acceptance runner; ordinary launches do not create test saves.
public class RecoveryStandaloneValidation : MonoBehaviour
{
    [Serializable] private class Result
    {
        public bool completed;
        public bool localPlayerSpawned;
        public bool localServerStarted;
        public bool localClientStarted;
        public bool saveReadBack;
        public bool loadedExistingWorld;
        public bool loadedStateMatches;
        public List<string> errors = new List<string>();
    }
    private readonly Result result = new Result();
    private string reportPath;
    private bool requested;
    private float elapsed;
    private int expectedMoney, expectedEuro, expectedIsland;

    public static string Argument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++)
            if (arguments[i] == name) return arguments[i + 1];
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Application.isEditor) return;
        string path = Argument("-recovery-validation-report");
        string savePath = Argument("-recovery-save-root");
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(savePath)) return;
        var runner = new GameObject("Recovery Standalone Validation").AddComponent<RecoveryStandaloneValidation>();
        runner.reportPath = Path.GetFullPath(path);
        DontDestroyOnLoad(runner.gameObject);
    }

    private void OnEnable() { Application.logMessageReceived += Log; }
    private void OnDisable() { Application.logMessageReceived -= Log; }
    private void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !result.errors.Contains(message))
            result.errors.Add(message + "\n" + trace);
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (!requested && elapsed > 7 && ConnectionManager.Instance && SaveManager.HasLocalSave)
        {
            requested = true;
            if (Argument("-recovery-load-existing") == "1")
            {
                SaveManager.SelectServer("RecoveryStandaloneValidation");
                result.loadedExistingWorld = SaveManager.CurServerSave != null;
                if (!result.loadedExistingWorld) throw new InvalidOperationException("The validation world could not be loaded.");
                expectedMoney = SaveManager.CurServerSave.Money;
                expectedEuro = SaveManager.CurServerSave.Euro;
                expectedIsland = SaveManager.CurServerSave.MaxIsland;
            }
            else SaveManager.CreateServer("RecoveryStandaloneValidation", false, false, Difficulty.Default);
            SaveManager.OnServerLoaded();
            ConnectionManager.Instance.CreateOfflineLobby();
        }
        if (elapsed < 45) return;
        result.localPlayerSpawned = Player.LocalPlayer;
        var network = FishNet.InstanceFinder.NetworkManager;
        result.localServerStarted = network && network.IsServerStarted;
        result.localClientStarted = network && network.IsClientStarted;
        if (result.localPlayerSpawned)
        {
            result.loadedStateMatches = !result.loadedExistingWorld ||
                (MoneyManager.Money == expectedMoney && MoneyManager.Euro == expectedEuro && SaveManager.CurServerSave.MaxIsland == expectedIsland);
            SaveManager.SaveServer(true);
            result.saveReadBack = SaveSystem.LoadAllServers().Exists(json =>
            {
                var saved = JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                return saved != null && saved.Name == "RecoveryStandaloneValidation";
            });
        }
        result.completed = true;
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
        Application.Quit(result.localPlayerSpawned && result.localServerStarted && result.localClientStarted && result.saveReadBack && result.loadedStateMatches && result.errors.Count == 0 ? 0 : 1);
        enabled = false;
    }
}
