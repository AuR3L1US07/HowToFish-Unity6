using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Requires a 65-second local-game harness. No progression or reward state is
// assigned: physics delivery and the game's QuestInteractable must produce it.
[InitializeOnLoad]
public static class RecoveryNpcDeliveryProbe
{
    [Serializable] public class Report
    {
        public bool completed, pickedUp, delivered, itemConsumed, rewardTaken, boatUnlocked, rewardSaved;
        public string npc, quest, detail, error;
    }
    static int stage;
    static float due, deadline;
    static Item item;
    static NPC npc;
    static byte questIndex;
    static Collider trigger;
    static Report report;
    static RecoveryNpcDeliveryProbe() { EditorApplication.update+=Tick; }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { stage=0; due=0; item=null; report=null; return; }
        if(stage>5 || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false) || Time.time<41 || Time.time<due) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            switch(stage)
            {
                case 0:
                    report=new Report();
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal))
                        throw new InvalidOperationException("Dedicated validation save required.");
                    if(player.Holding.HeldItem) throw new InvalidOperationException("Empty hands required.");
                    Item prefab=null;
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
                    {
                        for(byte i=0;i<candidate.Quests.Count;i++)
                        {
                            var quest=candidate.Quests[i];
                            if(!quest || quest.Type != QuestType.UnlockBoat || quest.TotalItems != 1 || quest.QuestItems == null) continue;
                            foreach(var required in quest.QuestItems)
                                if(required && !required.Creature) { npc=candidate; questIndex=i; prefab=required; break; }
                            if(prefab) break;
                        }
                        if(prefab) break;
                    }
                    if(!prefab) throw new InvalidOperationException("No loaded single-item boat-unlock quest accepting a non-creature item.");
                    foreach(var collider in npc.GetComponentsInChildren<Collider>())
                        if(collider.isTrigger && collider.enabled) { trigger=collider; break; }
                    if(!trigger) throw new InvalidOperationException("NPC has no enabled delivery trigger.");
                    if(BoatManager.Boat.BoatUnlocked) throw new InvalidOperationException("Boat is already unlocked; reward effect cannot be demonstrated.");
                    report.npc=npc.name; report.quest=npc.Quests[questIndex].name;
                    item=UnityEngine.Object.Instantiate(prefab,player.Transform.position+Vector3.up,Quaternion.identity);
                    Server.Instance.Spawn(item.gameObject); stage=1; due=Time.time+0.7f; break;
                case 1:
                    item.PickUp(player,true,true); stage=2; due=Time.time+0.7f; break;
                case 2:
                    report.pickedUp=player.Holding.HeldItem == item && item.SyncedHolder == player;
                    if(!report.pickedUp) throw new InvalidOperationException("Quest item pickup failed.");
                    item.Drop(true);
                    // Placement is the only test intervention. The original trigger callback
                    // performs CanEat, EatItem, destruction and quest progression naturally.
                    stage=3; due=Time.time+0.5f; deadline=Time.time+12; break;
                case 3:
                    if(item && !item.IsDestroying && !item.IsDeinitializing)
                    {
                        item.Rig.position=trigger.bounds.center;
                        item.Rig.linearVelocity=Vector3.zero;
                        item.Rig.angularVelocity=Vector3.zero;
                    }
                    report.delivered=NPCManager.Instance.NpcIsHoldingItem(npc.ID) && NPCManager.Instance.GetShowingQuest(npc.ID) == npc.Quests[questIndex];
                    report.itemConsumed=!item || item.IsDestroying || item.IsDeinitializing;
                    if(report.delivered && report.itemConsumed)
                    {
                        var reward=typeof(NPC).GetField("questInteractable",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(npc) as QuestInteractable;
                        if(!reward || !reward.gameObject.activeInHierarchy)
                        {
                            if(Time.time>deadline) throw new TimeoutException("Quest state completed but reward interactable never became active before deadline.");
                            // Authority updates the showing quest first. The client SyncDictionary
                            // callback activates the hand-held interactable on a later tick.
                            break;
                        }
                        reward.Interact(player);
                        stage=4; due=Time.time+1;
                    }
                    else if(Time.time>deadline) throw new TimeoutException("Physics delivery did not complete. Check NPC trigger and item acceptance.");
                    break;
                case 4:
                    report.rewardTaken=!NPCManager.Instance.NpcIsHoldingItem(npc.ID);
                    report.boatUnlocked=BoatManager.Boat.BoatUnlocked;
                    SaveManager.SaveServer(true);
                    foreach(var json in SaveSystem.LoadAllServers())
                    {
                        var loaded=JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                        if(loaded != null && loaded.Name == SaveManager.CurServerSave.Name) report.rewardSaved=loaded.UnlockedBoat;
                    }
                    report.completed=true;
                    report.detail="Physical required item pickup/drop, NPC trigger delivery, consumed item, QuestInteractable reward RPC, boat unlock and disk save. Fishing catch is separate.";
                    Finish(); break;
            }
        }
        catch(Exception error)
        {
            if(report == null) report=new Report();
            report.error=error.ToString(); Finish();
        }
    }
    static void Finish()
    {
        stage=6;
        if(item && !item.IsDestroying && !item.IsDeinitializing)
        {
            if(item.Holder) item.Drop(true);
            Server.Instance.Despawn(item.gameObject);
        }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/npc-delivery-probe.json",JsonUtility.ToJson(report,true));
    }
}
