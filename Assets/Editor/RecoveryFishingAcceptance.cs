using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class RecoveryFishingAcceptance
{
    [Serializable] public class Report
    {
        public bool completed, equipped, cast, naturallyHooked, reelInput, lineShortened, retrievedNearRod, killedBeforePickup, pickedUp, stored, saved;
        public float lineBefore, lineAfter, hookDistance, retrievalDistance;
        public int freeInventorySlotsBeforeStore;
        public string caughtItem, error;
        public List<string> interventions=new List<string>();
    }
    static int stage;
    static float due, deadline;
    static FishingRodCast rod;
    static Item caught;
    static BaitInfo testBait;
    static Mouse mouse;
    static Report report;
    static Vector3 waterPoint;
    static RecoveryFishingAcceptance() { EditorApplication.update+=Tick; }
    public static void Run()
    {
        SessionState.SetBool("RecoveryFishingAcceptance.Active",true);
        SessionState.SetBool("RecoveryFishingAcceptance.Requested",true);
        RecoveryPlayValidation.RunLocalGame();
    }
    static T Read<T>(string field)
    {
        for(Type type=rod.GetType();type != null;type=type.BaseType)
        {
            var info=type.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);
            if(info != null) return (T)info.GetValue(rod);
        }
        throw new MissingFieldException(field);
    }
    static void Button(MouseButton? button)
    {
        var state=new MouseState(); if(button.HasValue) state=state.WithButton(button.Value);
        InputSystem.QueueStateEvent(mouse,state);
    }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { stage=0; due=0; rod=null; caught=null; report=null; return; }
        if(stage>9 || !SessionState.GetBool("RecoveryFishingAcceptance.Active",false) || Time.time<75 || Time.time<due) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            switch(stage)
            {
                case 0:
                    report=new Report();
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal))
                        throw new InvalidOperationException("Dedicated validation world required.");
                    if(player.Holding.HeldItem) throw new InvalidOperationException("Empty hands required.");
                    var prefab=Resources.Load<GameObject>("items/purchasable/Fishing Rod");
                    rod=UnityEngine.Object.Instantiate(prefab,player.Transform.position+Vector3.up,Quaternion.identity).GetComponent<FishingRodCast>();
                    Server.Instance.Spawn(rod.gameObject);
                    mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(mouse);
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    stage=1; due=Time.time+1; break;
                case 1:
                    rod.PickUp(player,true,true); stage=2; due=Time.time+1; break;
                case 2:
                    report.equipped=player.Holding.HeldItem == rod && rod.SyncedHolder == player;
                    if(!report.equipped) throw new InvalidOperationException("Rod equip failed.");
                    testBait=UnityEngine.Object.Instantiate(rod.Bait.Info);
                    var weights=new List<ItemInfoWeight>();
                    foreach(var weight in testBait.ItemWeights)
                        if(weight.Fishable && weight.Fishable.ItemToSpawn && weight.Fishable.ItemToSpawn.GetComponent<Fish>() && weight.Fishable.ItemToSpawn.GetComponent<Creature>().BossType == BossType.None && weight.Weight>0) weights.Add(weight);
                    if(weights.Count == 0) throw new InvalidOperationException("Current bait has no non-boss fish candidates.");
                    typeof(BaitInfo).GetField("_itemWeights",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(testBait,weights);
                    typeof(BaitInfo).GetField("_catchTimeMinMax",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(testBait,new Vector2(1,2));
                    typeof(BaitInfo).GetField("_requireReelingToCatch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(testBait,false);
                    rod.Bait.SetBaitInfo(testBait);
                    report.interventions.Add("Runtime-only clone of existing bait: retain original non-boss fish weights, shorten wait to 1–2 seconds, remove movement requirement. Original asset untouched.");
                    Button(MouseButton.Right); stage=3; due=Time.time+1; break;
                case 3:
                    Button(null); stage=4; due=Time.time+1.5f; deadline=Time.time+12;
                    waterPoint=player.Transform.position+player.Transform.forward*10;
                    waterPoint.y=Mathf.Min(WaterManager.WaterHeight-4,-4);
                    report.interventions.Add("After real mouse cast, hold bait at underwater test point until native CreatureManager hook: "+waterPoint);
                    break;
                case 4:
                    report.cast=Read<bool>("_isReelingOut");
                    if(rod.Bait.ItemOnBait)
                    {
                        caught=rod.Bait.ItemOnBait;
                        report.naturallyHooked=caught.AttachedRod == rod && rod.Bait.ServerItemOnBait == caught;
                        report.caughtItem=caught.name;
                        report.hookDistance=Vector3.Distance(caught.transform.position,rod.transform.position);
                        report.lineBefore=rod.Bait.LineLength;
                        // No placement after the hook: physical joints and original reel logic
                        // must retrieve the fish and shorten the line.
                        Button(MouseButton.Left); stage=5; deadline=Time.time+25;
                    }
                    else
                    {
                        rod.Bait.Rig.position=waterPoint; rod.Bait.Rig.linearVelocity=Vector3.zero;
                        if(Time.time>deadline) throw new TimeoutException("Native bait timer did not produce a hooked fish.");
                    }
                    break;
                case 5:
                    report.reelInput=Read<bool>("_isReelingIn") && !Read<bool>("_isReelingOut");
                    report.lineAfter=rod.Bait.LineLength;
                    report.lineShortened=report.lineAfter<report.lineBefore;
                    report.retrievalDistance=caught ? Vector3.Distance(caught.transform.position,rod.transform.position) : float.PositiveInfinity;
                    report.retrievedNearRod=report.retrievalDistance<4 && report.retrievalDistance<report.hookDistance;
                    if(report.retrievedNearRod)
                    {
                        Button(null);
                        // Creature.CanPickUp becomes true on death; original live-fish pickup must not be bypassed.
                        if(caught.Creature && !caught.Creature.IsDead)
                            caught.LocalHit(caught.transform,caught.transform.position,Vector3.forward,player,caught.Creature.MaxHp+100,false);
                        stage=9; due=Time.time+1;
                        report.interventions.Add("After physical mouse retrieval to < 4 m, original LocalHit dispatch kills catch before pickup, respecting Creature.CanPickUp death requirement; pickup aiming input remains a separate test.");
                    }
                    else if(Time.time>deadline) throw new TimeoutException("Mouse reeling did not retrieve the fish within 25 seconds.");
                    break;
                case 9:
                    report.killedBeforePickup=caught && caught.Creature && caught.Creature.IsDead && caught.CanPickUp;
                    if(!report.killedBeforePickup) throw new InvalidOperationException("Retrieved catch must be dead and legitimately pickable before pickup.");
                    caught.PickUp(player,true,true); stage=6; due=Time.time+1; break;
                case 6:
                    report.pickedUp=caught && player.Holding.HeldItem == caught && caught.SyncedHolder == player && !caught.AttachedRod;
                    if(!report.pickedUp) throw new InvalidOperationException("Retrieved fish pickup failed or live fish escaped.");
                    int slotCount=(int)typeof(PlayerInventory).GetMethod("GetTotalSlots",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player.Inventory,null);
                    for(byte slot=0;slot<slotCount;slot++) if(!player.Inventory._items[slot]) report.freeInventorySlotsBeforeStore++;
                    if(report.freeInventorySlotsBeforeStore == 0) throw new InvalidOperationException("Validation inventory full before storing caught fish; no gameplay rule should be changed to force storage.");
                    player.Inventory.LocalTrySelectSlot(-1); stage=7; due=Time.time+1; break;
                case 7:
                    report.stored=caught && player.Inventory.HasItemInInventory(caught);
                    if(!report.stored) throw new InvalidOperationException("Caught fish could not be stored through slot selection.");
                    player.Inventory.SaveInventory();
                    var saved=SaveManager.GetSavedPlayer(player.SteamID);
                    report.saved=saved != null && saved.InventoryItems != null && saved.InventoryItems.Exists(i=>i.Exists && i.ItemID == caught.ID);
                    report.completed=true; Finish(); break;
            }
        }
        catch(Exception error) { if(report == null) report=new Report(); report.error=error.ToString(); Finish(); }
    }
    static void Finish()
    {
        stage=10;
        SessionState.SetBool("RecoveryFishingAcceptance.Active",false);
        if(mouse != null) Button(null);
        if(caught && !caught.IsDeinitializing && !caught.IsDestroying)
        {
            if(caught.Holder) caught.Drop(true);
            if(Player.LocalPlayer) Player.LocalPlayer.Inventory.RemoveItem(caught);
            Server.Instance.Despawn(caught.gameObject);
        }
        if(rod && !rod.IsDeinitializing)
        {
            if(rod.Holder) rod.Drop(true);
            if(Player.LocalPlayer) Player.LocalPlayer.Inventory.RemoveItem(rod);
            Server.Instance.Despawn(rod.gameObject);
        }
        if(testBait) UnityEngine.Object.Destroy(testBait);
        if(Player.LocalPlayer) Player.LocalPlayer.Inventory.SaveInventory();
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/fishing-acceptance.json",JsonUtility.ToJson(report,true));
    }
}
