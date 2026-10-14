using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Only armed by the explicit recovery local-game session, never normal gameplay.
[InitializeOnLoad]
public static class RecoveryFishingProbe
{
    [Serializable] public class Observation { public string name; public bool passed; public string detail; }
    [Serializable] public class Report { public bool completed; public List<Observation> observations = new List<Observation>(); }
    static Report report;
    static int stage;
    static float due, castLength;
    static FishingRodCast rod;
    static Mouse mouse;
    static bool finished;
    static RecoveryFishingProbe() { EditorApplication.update += Tick; }
    static void Observe(string name, bool passed, string detail) { report.observations.Add(new Observation { name=name, passed=passed, detail=detail }); }
    static T Read<T>(string field)
    {
        for (Type type = rod.GetType(); type != null; type=type.BaseType)
        {
            var info=type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            if(info != null) return (T)info.GetValue(rod);
        }
        throw new MissingFieldException(field);
    }
    static void Button(MouseButton? button)
    {
        var state=new MouseState();
        if(button.HasValue) state=state.WithButton(button.Value);
        InputSystem.QueueStateEvent(mouse,state);
    }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { finished=false; stage=0; due=0; report=null; rod=null; return; }
        if(finished || !SessionState.GetBool("RecoveryPlayValidation.LocalGame", false) || Time.time < 24 || Time.time < due) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        if(report == null || stage == 0) report=new Report();
        try
        {
            switch(stage++)
            {
                case 0:
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation", StringComparison.Ordinal))
                        throw new InvalidOperationException("Probe requires the dedicated RecoveryLocalValidation save.");
                    if(player.Holding.HeldItem) throw new InvalidOperationException("Probe requires empty hands to preserve existing held item.");
                    var prefab=Resources.Load<GameObject>("items/purchasable/Fishing Rod");
                    if(!prefab) throw new InvalidOperationException("Fishing rod prefab is missing.");
                    rod=UnityEngine.Object.Instantiate(prefab,player.Transform.position + Vector3.up,Quaternion.identity).GetComponent<FishingRodCast>();
                    if(!rod) throw new InvalidOperationException("Prefab does not contain FishingRodCast.");
                    Server.Instance.Spawn(rod.gameObject);
                    mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
                    InputSystem.EnableDevice(mouse);
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    due=Time.time+1; break;
                case 1:
                    rod.PickUp(player,true,true); due=Time.time+1; break;
                case 2:
                    Observe("equip-rod",player.Holding.HeldItem == rod && rod.SyncedHolder == player,"Pickup uses Item.PickUp and the server holder RPC.");
                    Button(MouseButton.Right); due=Time.time+1; break;
                case 3:
                    Observe("pull-back-input",Read<bool>("_isPullingBack") && Read<float>("_curRodRot") > 0,"Right mouse input must reach PlayerHolding and rod state.");
                    Button(null); due=Time.time+1.5f; break;
                case 4:
                    castLength=rod.Bait.LineLength;
                    Observe("cast-release",Read<bool>("_isReelingOut") && castLength > 0,"Observed line length: "+castLength);
                    Button(MouseButton.Left); due=Time.time+2; break;
                case 5:
                    Observe("reel-input",Read<bool>("_isReelingIn") && !Read<bool>("_isReelingOut"),"Left mouse closes bail and starts reeling.");
                    Observe("reel-length",rod.Bait.LineLength < castLength,"Before: "+castLength+"; after: "+rod.Bait.LineLength);
                    Button(null);
                    player.Inventory.LocalTrySelectSlot(-1); due=Time.time+1; break;
                case 6:
                    Observe("store-rod",player.Inventory.HasItemInInventory(rod) && player.Holding.HeldItem != rod,"Stored through LocalTrySelectSlot and inventory RPC.");
                    player.Inventory.SaveInventory();
                    var saved=SaveManager.GetSavedPlayer(player.SteamID);
                    Observe("stored-rod-save",saved != null && saved.InventoryItems != null && saved.InventoryItems.Exists(i=>i.Exists && i.ItemID == rod.ID),"Stored physical rod must be captured by SaveInventory.");
                    int slot=-1;
                    foreach(var entry in player.Inventory._items) if(entry.Value == rod) slot=entry.Key;
                    if(slot < 0) throw new InvalidOperationException("Rod has no inventory slot.");
                    player.Inventory.LocalTrySelectSlot(slot); due=Time.time+1; break;
                case 7:
                    Observe("select-stored-rod",player.Holding.HeldItem == rod,"Slot selection must restore held item.");
                    rod.Drop(true); due=Time.time+1; break;
                case 8:
                    Observe("drop-rod",!rod.SyncedHolder && player.Holding.HeldItem != rod && !player.Inventory.HasItemInInventory(rod),"Holder and inventory must both release the rod.");
                    if(rod.Currency != Currency.Dollar) throw new InvalidOperationException("Sale probe only permits dollar items to avoid euro achievements.");
                    int before=MoneyManager.Instance._money.Value;
                    int price=rod.TotalWorth;
                    MoneyManager.SellItem(rod);
                    Observe("physical-item-sale",price > 0 && MoneyManager.Instance._money.Value == before+price,"Sold instantiated rod using MoneyManager.SellItem. Price="+price+". NPC hand-in/destruction remains separate.");
                    report.completed=true; Finish(); break;
            }
        }
        catch(Exception error) { Observe("exception",false,error.ToString()); Finish(); }
    }
    static void Finish()
    {
        finished=true;
        if(mouse != null) Button(null);
        if(rod)
        {
            if(rod.Holder) rod.Drop(true);
            if(Player.LocalPlayer) Player.LocalPlayer.Inventory.RemoveItem(rod);
            Server.Instance.Despawn(rod.gameObject);
        }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/fishing-input-probe.json",JsonUtility.ToJson(report,true));
    }
}
