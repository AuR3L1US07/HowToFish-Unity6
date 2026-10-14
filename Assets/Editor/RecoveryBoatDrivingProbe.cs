using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RecoveryBoatDrivingProbe
{
    [Serializable] public class Report
    {
        public bool completed, motorPurchased, paymentDebited, purchaseSaved, driverEntered, throttleInput, moved, steeringInput, rotated, reverseInput, driverExited;
        public int price, balanceBefore, balanceAfter;
        public byte motorBefore, motorAfter;
        public float distance, turnDegrees;
        public string error;
        public List<string> setup=new List<string>();
    }
    static int stage;
    static float due;
    static Boat boat;
    static MotorPurchasable shop;
    static Keyboard keyboard;
    static Report report;
    static Vector3 origin;
    static Quaternion turnOrigin;
    static byte targetMotor;
    static AsyncOperation motorSceneLoad;
    static RecoveryBoatDrivingProbe() { EditorApplication.update+=Tick; }
    static T Read<T>(object source,string name)
    {
        for(Type type=source.GetType();type != null;type=type.BaseType)
        {
            var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            if(field != null) return (T)field.GetValue(source);
        }
        throw new MissingFieldException(name);
    }
    static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { stage=0; due=0; report=null; boat=null; shop=null; motorSceneLoad=null; targetMotor=0; return; }
        if(stage>9 || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false) || Time.time<105 || Time.time<due || SessionState.GetBool("RecoveryFishingAcceptance.Active",false)) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            switch(stage)
            {
                case 0:
                    if(report == null) report=new Report();
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal))
                        throw new InvalidOperationException("Dedicated validation world required.");
                    boat=BoatManager.Boat;
                    if(!boat || !boat.BoatUnlocked) throw new InvalidOperationException("NPC boat-key reward must have unlocked the boat first.");
                    if(player.Holding.HeldItem || boat.Driver) throw new InvalidOperationException("Empty hands and vacant driver required.");
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType<MotorPurchasable>(FindObjectsSortMode.None))
                    {
                        byte index=Read<byte>(candidate,"_motorIndex");
                        int price=Read<int>(candidate,"_customCost");
                        if(index>boat.MotorIndex && price>0 && (!shop || index<targetMotor)) { shop=candidate; targetMotor=index; report.price=price; }
                    }
                    if(!shop)
                    {
                        if(motorSceneLoad == null)
                        {
                            if(SceneManager.GetSceneByName("Island2").isLoaded) throw new InvalidOperationException("Island2 already loaded but paid motor upgrade missing.");
                            motorSceneLoad=SceneManager.LoadSceneAsync("Island2",LoadSceneMode.Additive);
                            report.setup.Add("Load original Island2 additively: Island1 has no paid MotorPurchasable; Island2 original upgrade is index 1, price $230.");
                            due=Time.time+1; return;
                        }
                        if(!motorSceneLoad.isDone) { due=Time.time+0.5f; return; }
                        throw new InvalidOperationException("No paid motor upgrade after original Island2 scene loaded.");
                    }
                    report.motorBefore=boat.MotorIndex;
                    int budget=Mathf.Max(0,report.price+1000-MoneyManager.Instance._money.Value);
                    if(budget>0) MoneyManager.AddMoney(budget,player);
                    report.setup.Add("Dedicated test world: add $"+budget+" budget through AddMoney before paid purchase; no euro credit.");
                    keyboard=Keyboard.current ?? InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    GameInfo.Input.SwitchCurrentControlScheme("Keyboard",keyboard,Mouse.current ?? InputSystem.AddDevice<Mouse>());
                    stage=1; due=Time.time+1; break;
                case 1:
                    report.balanceBefore=MoneyManager.Money;
                    shop.Hover(); shop.Interact(player); shop.UnHover();
                    stage=2; due=Time.time+1; break;
                case 2:
                    report.motorAfter=boat.MotorIndex;
                    report.balanceAfter=MoneyManager.Money;
                    report.motorPurchased=report.motorAfter == targetMotor && report.motorAfter>report.motorBefore;
                    report.paymentDebited=report.balanceBefore-report.balanceAfter == report.price;
                    SaveManager.SaveServer(true);
                    foreach(var json in SaveSystem.LoadAllServers())
                    {
                        var saved=JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                        if(saved != null && saved.Name == SaveManager.CurServerSave.Name)
                            report.purchaseSaved=saved.MotorIndex == targetMotor && saved.Money == report.balanceAfter;
                    }
                    var interactable=Read<BoatInteractable>(boat,"_boatInteractable");
                    if(!interactable) throw new InvalidOperationException("Boat driving interactable missing.");
                    // Original interaction sets WantToDrive and sends SetDriver RPC.
                    interactable.Interact(player); stage=3; due=Time.time+3; break;
                case 3:
                    report.driverEntered=boat.Driver == player && Boat.IsDrivingLocally;
                    if(!report.driverEntered) throw new InvalidOperationException("Original BoatInteractable did not enter driving.");
                    origin=boat.HiddenPhysicsRig.position;
                    Keys(Key.W); stage=4; due=Time.time+3; break;
                case 4:
                    report.throttleInput=(float)boat._driverYInput.Value>0.5f;
                    report.distance=Vector3.Distance(origin,boat.HiddenPhysicsRig.position);
                    report.moved=report.distance>0.5f;
                    turnOrigin=boat.HiddenPhysicsRig.rotation;
                    Keys(Key.W,Key.D); stage=5; due=Time.time+3; break;
                case 5:
                    report.steeringInput=Mathf.Abs((float)boat._driverXInput.Value)>0.5f;
                    report.turnDegrees=Quaternion.Angle(turnOrigin,boat.HiddenPhysicsRig.rotation);
                    report.rotated=report.turnDegrees>2;
                    Keys(Key.S); stage=6; due=Time.time+2; break;
                case 6:
                    report.reverseInput=(float)boat._driverYInput.Value < -0.5f;
                    Keys(); stage=7; due=Time.time+0.3f; break;
                case 7:
                    Key stop=Key.None;
                    foreach(var binding in GameInfo.Input.actions["PlayerDrop"].bindings)
                    {
                        if(!binding.effectivePath.StartsWith("<Keyboard>/",StringComparison.OrdinalIgnoreCase)) continue;
                        Enum.TryParse(binding.effectivePath.Substring("<Keyboard>/".Length),true,out stop);
                        if(stop != Key.None) break;
                    }
                    if(stop == Key.None) throw new InvalidOperationException("No simple keyboard binding found for PlayerDrop stop-driving input.");
                    Keys(stop); stage=8; due=Time.time+0.7f; break;
                case 8:
                    Keys(); report.driverExited=!boat.Driver && !Boat.IsDrivingLocally;
                    report.completed=true; Finish(); break;
            }
        }
        catch(Exception error) { if(report == null) report=new Report(); report.error=error.ToString(); Finish(); }
    }
    static void Finish()
    {
        stage=10;
        if(keyboard != null) Keys();
        if(boat && boat.Driver) Server.Instance.SetDriver(null);
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/boat-driving-probe.json",JsonUtility.ToJson(report,true));
    }
}
