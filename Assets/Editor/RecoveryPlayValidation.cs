using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class RecoveryPlayValidation
{
    [Serializable] public class Result { public bool enteredPlay; public bool completed; public bool steamRestartCheck; public bool steamRestartRequired; public bool steamInitialized; public bool localGameRequested; public bool localPlayerSpawned; public bool localServerStarted; public bool localClientStarted; public bool movementInputReceived; public float movementDistance; public string inputDiagnostics; public bool saveReadBack; public List<string> errors = new List<string>(); public string screenshot; }
    static Result result;
    static double started;
    static bool captureRequested;
    static bool gameRequested;
    static double movementStarted;
    static Vector3 movementOrigin;
    static bool movementReleased;
    static RecoveryPlayValidation()
    {
        if (SessionState.GetBool("RecoveryPlayValidation.Active", false))
        {
            result = new Result { enteredPlay = true };
            EditorApplication.update += Tick;
            Application.logMessageReceived += Log;
            started = EditorApplication.timeSinceStartup;
        }
    }
    public static void Run()
    {
        RecoveryLocalizationSettings.Initialize();
        RecoveryLocalizationSettings.BuildCatalog();
        var precheck = new Result { steamRestartCheck = false, steamRestartRequired = false };
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/steam-restart-validation.json", JsonUtility.ToJson(precheck,true));
        if (precheck.steamRestartRequired) { EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        SessionState.SetBool("RecoveryPlayValidation.Active", true);
        EditorApplication.isPlaying = true;
    }
    public static void RunLocalGame()
    {
        SessionState.SetBool("RecoveryPlayValidation.LocalGame", true);
        Run();
    }
    static void Log(string message, string trace, LogType type)
    {
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            if(!result.errors.Contains(message)) result.errors.Add(message);
    }
    static void Tick()
    {
        if(result == null) return;
        if(EditorApplication.isPlaying)
        {
            result.enteredPlay = true;
            bool localGame = SessionState.GetBool("RecoveryPlayValidation.LocalGame", false);
            if(localGame && !captureRequested && Time.time > 3)
            {
                var textReport = new System.Text.StringBuilder();
                foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
                {
                    text.ForceMeshUpdate();
                    textReport.AppendLine(text.name + " | " + text.text + " | visible=" + text.textInfo.characterCount + " | font=" + (text.font ? text.font.name : "NULL") + " | atlasMatches=" + (text.font && text.fontSharedMaterial && text.fontSharedMaterial.mainTexture == text.font.atlasTexture));
                }
                File.WriteAllText("CompilationChecks/local-menu-text.txt", textReport.ToString());
                CaptureMenu("CompilationChecks/local-menu.png"); ScreenCapture.CaptureScreenshot("CompilationChecks/local-menu-gameview.png"); captureRequested=true;
            }
            if(localGame && !gameRequested && Time.time > 7 && ConnectionManager.Instance && SaveManager.HasLocalSave)
            {
                gameRequested = true;
                result.localGameRequested = true;
                SaveManager.CreateServer("RecoveryLocalValidation", false, false, Difficulty.Default);
                SaveManager.OnServerLoaded();
                ConnectionManager.Instance.CreateOfflineLobby();
            }
            if(!captureRequested && Time.time > 12)
            {
                result.screenshot = "CompilationChecks/menu-play.png";
                CaptureMenu(result.screenshot);
                captureRequested = true;
            }
            if(localGame && Player.LocalPlayer != null && !MainMenuManager.IsInMenu && Time.time > 20)
            {
                var keyboard = movementStarted == 0 ? InputSystem.AddDevice<Keyboard>("RecoveryTestKeyboard") : Keyboard.current; InputSystem.EnableDevice(keyboard);
                if(movementStarted == 0)
                {
                    movementStarted = EditorApplication.timeSinceStartup;
                    movementOrigin = Player.LocalPlayer.Transform.position; var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>(); GameInfo.Input.enabled=false; GameInfo.Input.enabled=true; GameInfo.Input.SwitchCurrentControlScheme("Keyboard", keyboard, mouse);
                    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); InputSystem.Update();
                }
                if(!movementReleased)
                {
                    result.movementInputReceived |= Player.LocalPlayer.Movement.Input.sqrMagnitude > 0;
                    if(EditorApplication.timeSinceStartup-movementStarted > 1)
                    {
                        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                        result.movementDistance = Vector3.Distance(movementOrigin,Player.LocalPlayer.Transform.position);
                        movementReleased = true;
                    }
                }
            }
            if(Time.time < (localGame ? 45 : 20)) return;
            result.inputDiagnostics = "menu="+MainMenuManager.IsInMenu+" enabled="+Player.LocalPlayerEnabled+" driving="+SelfDrivingBoat.IsDriving+" paused="+PauseManager.IsPaused+" time="+Time.time+" input="+GameInfo.Input.currentActionMap+" blocked="+(Player.LocalPlayer != null && Player.LocalPlayer.BlockInputs)+" moveEnabled="+GameInfo.Input.actions["PlayerMove"].enabled+" controls="+GameInfo.Input.actions["PlayerMove"].controls.Count+" owner="+Player.LocalPlayer.Movement.Owner.IsLocalClient+" keyboard="+Keyboard.current.wKey.isPressed+" inputUser="+GameInfo.Input.user.valid; result.steamInitialized = HeathenEngineering.SteamworksIntegration.API.App.Initialized;
            result.localPlayerSpawned = Player.LocalPlayer != null;
            if(localGame && result.localPlayerSpawned)
            {
                SaveManager.SaveServer(true);
                result.saveReadBack = SaveSystem.LoadAllServers().Exists(s=>s.Contains("RecoveryLocalValidation"));
            }
            if(FishNet.InstanceFinder.NetworkManager != null)
            {
                result.localServerStarted = FishNet.InstanceFinder.NetworkManager.IsServerStarted;
                result.localClientStarted = FishNet.InstanceFinder.NetworkManager.IsClientStarted;
            }
            if(localGame) { result.screenshot = "CompilationChecks/local-game.png"; CaptureMenu(result.screenshot); }
            SessionState.SetBool("RecoveryPlayValidation.Active", false);
            SessionState.SetBool("RecoveryPlayValidation.LocalGame", false);
            result.completed = true;
            File.WriteAllText(localGame ? "CompilationChecks/local-game-validation.json" : "CompilationChecks/menu-play-validation.json", JsonUtility.ToJson(result,true));
            Debug.Log("RECOVERY_PLAY_VALIDATION_FINISHED errors="+result.errors.Count);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
        else if(EditorApplication.timeSinceStartup-started > 40)
        {
            SessionState.SetBool("RecoveryPlayValidation.Active", false);
            File.WriteAllText("CompilationChecks/menu-play-validation.json", JsonUtility.ToJson(result,true));
            EditorApplication.Exit(1);
        }
    }
    static void CaptureMenu(string filename)
    {
        var camera = Player.LocalPlayer != null ? Player.LocalPlayer.CurCam : MainMenuManager.MenuCam;
        if(camera == null) camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if(camera == null) return;
        var texture = new RenderTexture(1280,720,24);
        var previous = camera.targetTexture;
        int previousMask = camera.cullingMask;
        var active = RenderTexture.active;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var changed = new List<Canvas>(); var modes = new Dictionary<Canvas,RenderMode>(); var cameras = new Dictionary<Canvas,Camera>(); var distances = new Dictionary<Canvas,float>();
        foreach(var canvas in canvases) if(canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
        { changed.Add(canvas); modes[canvas]=canvas.renderMode; cameras[canvas]=canvas.worldCamera; distances[canvas]=canvas.planeDistance; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f; }
        try
        {
            camera.targetTexture = texture;
            camera.cullingMask = -1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = texture;
            var pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);
            pixels.Apply();
            File.WriteAllBytes(filename,pixels.EncodeToPNG());
            UnityEngine.Object.Destroy(pixels);
        }
        finally
        {
            camera.targetTexture = previous; camera.cullingMask = previousMask; RenderTexture.active = active;
            foreach(var canvas in changed) { canvas.renderMode=modes[canvas]; canvas.worldCamera=cameras[canvas]; canvas.planeDistance=distances[canvas]; }
            UnityEngine.Object.Destroy(texture);
        }
    }
}








