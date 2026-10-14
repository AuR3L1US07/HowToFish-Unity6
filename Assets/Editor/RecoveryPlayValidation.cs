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
        // Allow editor startup callbacks (including creation of the default
        // Search index) to finish before entering Play mode.
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
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
                foreach(var field in UnityEngine.Object.FindObjectsByType<TMPro.TMP_InputField>(FindObjectsInactive.Include))
                    if(field.name=="ServerNameInputField")
                    {
                        var testCanvas=new GameObject("Input validation canvas",typeof(Canvas));
                        testCanvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                        var test=UnityEngine.Object.Instantiate(field,testCanvas.transform);
                        test.onValueChanged=new TMPro.TMP_InputField.OnChangeEvent();
                        test.onEndEdit=new TMPro.TMP_InputField.SubmitEvent();
                        test.gameObject.SetActive(true);test.text="Local123";
                        Canvas.ForceUpdateCanvases();test.ForceLabelUpdate();test.textComponent.ForceMeshUpdate(true,true);
                        var font=test.textComponent.font;
                        File.WriteAllText("CompilationChecks/latin-input-validation.txt", "value="+test.text+" label="+test.textComponent.text+" glyphs="+test.textComponent.textInfo.characterCount+" atlas="+font.atlasTexture.width+"x"+font.atlasTexture.height);
                        UnityEngine.Object.Destroy(testCanvas);
                    }
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
            float duration = SessionState.GetBool("RecoveryBossProgression.Requested", false) ? 240 :
                SessionState.GetBool("RecoveryFishingAcceptance.Requested", false) ? 180 : (localGame ? 65 : 20);
            if(Time.time < duration) return;
            if(!localGame) {
                result.completed=true;
                result.steamInitialized=HeathenEngineering.SteamworksIntegration.API.App.Initialized;
                SessionState.SetBool("RecoveryPlayValidation.Active",false);
                EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=()=>{
                    File.WriteAllText("CompilationChecks/menu-play-validation.json",JsonUtility.ToJson(result,true));
                    EditorApplication.Exit(result.errors.Count==0 ? 0 : 1);
                };
                return;
            }
            if(localGame && !Player.LocalPlayer) {
                result.errors.Add("Local player did not spawn within the validation period.");
                File.WriteAllText("CompilationChecks/local-game-validation.json",JsonUtility.ToJson(result,true));
                EditorApplication.Exit(1);return;
            }
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
            SessionState.SetBool("RecoveryFishingAcceptance.Requested", false);
            SessionState.SetBool("RecoveryBossProgression.Requested", false);
            result.completed = true;
            File.WriteAllText(localGame ? "CompilationChecks/local-game-validation.json" : "CompilationChecks/menu-play-validation.json", JsonUtility.ToJson(result,true));
            Debug.Log("RECOVERY_PLAY_VALIDATION_FINISHED errors="+result.errors.Count);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => {
                File.WriteAllText(localGame ? "CompilationChecks/local-game-validation.json" : "CompilationChecks/menu-play-validation.json", JsonUtility.ToJson(result,true));
                EditorApplication.Exit(result.errors.Count==0 ? 0 : 1);
            };
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
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine("capture=" + camera.name + " position=" + camera.transform.position + " rotation=" + camera.transform.eulerAngles + " fov=" + camera.fieldOfView + " mask=" + camera.cullingMask);
        foreach(var candidate in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            diagnostics.AppendLine("camera=" + candidate.name + " enabled=" + candidate.enabled + " position=" + candidate.transform.position + " rotation=" + candidate.transform.eulerAngles + " depth=" + candidate.depth + " mask=" + candidate.cullingMask);
        if(Player.LocalPlayer)
            foreach(var renderer in Player.LocalPlayer.GetComponentsInChildren<Renderer>())
                diagnostics.AppendLine("renderer=" + renderer.name + " enabled=" + renderer.enabled + " layer=" + renderer.gameObject.layer + " bounds=" + renderer.bounds + " shadow=" + renderer.shadowCastingMode);
        File.WriteAllText(filename + ".diagnostics.txt", diagnostics.ToString());
        var texture = new RenderTexture(1280,720,24);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var changed = new List<Canvas>(); var modes = new Dictionary<Canvas,RenderMode>(); var cameras = new Dictionary<Canvas,Camera>(); var distances = new Dictionary<Canvas,float>();
        foreach(var canvas in canvases) if(canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
        { changed.Add(canvas); modes[canvas]=canvas.renderMode; cameras[canvas]=canvas.worldCamera; distances[canvas]=canvas.planeDistance; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f; }
        try
        {
            camera.targetTexture = texture;
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
            camera.targetTexture = previous; RenderTexture.active = active;
            foreach(var canvas in changed) { canvas.renderMode=modes[canvas]; canvas.worldCamera=cameras[canvas]; canvas.planeDistance=distances[canvas]; }
            UnityEngine.Object.Destroy(texture);
        }
    }
}








