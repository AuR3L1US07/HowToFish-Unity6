using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RecoveryBossProgressionProbe
{
    [Serializable] public class BossResult
    {
        public string boss, scene, quest, error;
        public bool spawned, damageApplied, deathObserved, trophySpawned, trophyPickedUp, delivered, itemConsumed, rewardTaken, progressChanged, progressSaved;
        public int hpBefore, hpAfter;
        public byte maxBefore, maxAfter;
    }
    [Serializable] public class Report
    {
        public bool completed, finalBossReward, finishedAuthority, finishedClient, creditsUi, creditsEffects, finishSaved;
        public string error;
        public List<BossResult> bosses=new List<BossResult>();
        public string intervention="Dedicated fresh validation world; original scene teleport for setup, original boss prefab Spawn, one nonlethal LocalHit then high-damage LocalHit fixture (not combat difficulty validation), spawned trophy physical pickup/drop/trigger delivery, original quest reward and end-game interaction. Progress flags are never assigned.";
    }
    static int stage, index;
    static float due, deadline;
    static Creature boss;
    static Item trophyPrefab, trophy;
    static NPC npc;
    static NPCQuest quest;
    static Collider trigger;
    static BossResult current;
    static Report report;
    static readonly string[] BossNames={"Giant Piranha","Pufferfish","Albatross","Mutated Bowhead Whale"};
    static RecoveryBossProgressionProbe() { EditorApplication.update+=Tick; }
    public static void Run()
    {
        SessionState.SetBool("RecoveryBossProgression.Requested",true);
        SessionState.SetBool("RecoveryFishingAcceptance.Active",false);
        SessionState.SetBool("RecoveryFishingAcceptance.Requested",false);
        RecoveryPlayValidation.RunLocalGame();
    }
    static T Field<T>(object target,string name)
    {
        for(var type=target.GetType();type != null;type=type.BaseType)
        {
            var info=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            if(info != null) return (T)info.GetValue(target);
        }
        throw new MissingFieldException(name);
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying) { stage=0; index=0; report=null; return; }
        if(stage>10 || !SessionState.GetBool("RecoveryBossProgression.Requested",false) || Time.time<25 || Time.time<due) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            switch(stage)
            {
                case 0:
                    report=new Report();
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal) || SaveManager.CurServerSave.HasFinishedGame || SaveManager.CurServerSave.MaxIsland != 2) throw new InvalidOperationException("Fresh dedicated default-progress world required.");
                    stage=1; break;
                case 1:
                    current=new BossResult{boss=BossNames[index],scene="Island"+(index+2)}; report.bosses.Add(current);
                    current.maxBefore=OnlineIslandManager.MaxIslandUnlocked;
                    if(current.maxBefore<index+2) throw new InvalidOperationException("Previous genuine reward did not unlock next test island.");
                    OnlineIslandManager.TpToSpecificIsland((byte)(index+1));
                    stage=2; due=Time.time+3; deadline=Time.time+25; break;
                case 2:
                    if(IslandManager.IsLoading || !Island.CurIsland || Island.CurIsland.gameObject.scene.name != current.scene)
                    { if(Time.time>deadline) throw new TimeoutException("Original scene teleport did not load "+current.scene); due=Time.time+0.5f; return; }
                    var prefab=Resources.Load<GameObject>("items/creatures/bosses/"+BossNames[index]).GetComponent<Creature>();
                    trophyPrefab=Field<Item>(prefab,"_spawnOnDeath");
                    if(!trophyPrefab) throw new InvalidOperationException("Original boss trophy missing.");
                    npc=null; quest=null; trigger=null;
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
                    {
                        foreach(var candidateQuest in candidate.Quests)
                            // The original final-whale quest declares TotalItems=0. NPC.OnQuestProgression
                            // increments on delivery then completes when progression >= TotalItems,
                            // so both 0 and 1 require a single physical trophy delivery.
                            if(candidateQuest && (candidateQuest.Type == QuestType.UnlockIsland || candidateQuest.Type == QuestType.FinalBoss) && candidateQuest.TotalItems<=1 && candidateQuest.HasItem(trophyPrefab)) { npc=candidate; quest=candidateQuest; break; }
                        if(npc) break;
                    }
                    if(!npc) throw new InvalidOperationException("No original loaded progression NPC accepting boss trophy.");
                    current.quest=quest.name;
                    foreach(var collider in npc.GetComponentsInChildren<Collider>()) if(collider.isTrigger && collider.enabled) { trigger=collider; break; }
                    if(!trigger) throw new InvalidOperationException("Quest NPC trigger missing.");
                    boss=UnityEngine.Object.Instantiate(prefab,player.Transform.position+Vector3.forward*25+Vector3.up*8,Quaternion.identity);
                    Server.Instance.Spawn(boss.gameObject); stage=3; due=Time.time+1; deadline=Time.time+25; break;
                case 3:
                    current.spawned=boss && boss.IsServerInitialized && boss.IsClientInitialized && BossManager.Boss == boss;
                    if(!current.spawned) throw new InvalidOperationException("Original BossManager initialization failed.");
                    if(BossManager.IsImmortal) { if(Time.time>deadline) throw new TimeoutException("Boss remained immortal before damage."); due=Time.time+0.5f; return; }
                    current.hpBefore=boss._hp.Value;
                    boss.LocalHit(boss.transform,boss.transform.position,Vector3.forward,player,1,false);
                    stage=4; due=Time.time+1; break;
                case 4:
                    current.hpAfter=boss._hp.Value;
                    current.damageApplied=current.hpAfter == current.hpBefore-1 && boss.Hp == current.hpAfter;
                    if(!current.damageApplied) throw new InvalidOperationException("Nonlethal LocalHit did not synchronize boss damage.");
                    if(BossManager.IsImmortal) { due=Time.time+0.5f; return; }
                    boss.LocalHit(boss.transform,boss.transform.position,Vector3.forward,player,current.hpAfter+100000,false);
                    stage=5; due=Time.time+1; deadline=Time.time+25; break;
                case 5:
                    current.deathObserved |= (boss && boss.IsDead) || !BossManager.Boss;
                    foreach(var item in UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
                        if(item.ID == trophyPrefab.ID && item.IsServerInitialized && item.IsClientInitialized && !item.IsDestroying) { trophy=item; break; }
                    current.trophySpawned=trophy;
                    if(!current.deathObserved || !trophy) { if(Time.time>deadline) throw new TimeoutException("Death animation did not produce original trophy."); due=Time.time+0.5f; return; }
                    trophy.PickUp(player,true,true); stage=6; due=Time.time+1; break;
                case 6:
                    current.trophyPickedUp=trophy && trophy.SyncedHolder == player && player.Holding.HeldItem == trophy;
                    if(!current.trophyPickedUp) throw new InvalidOperationException("Spawned original trophy pickup failed.");
                    trophy.Drop(true); stage=7; due=Time.time+0.5f; deadline=Time.time+15; break;
                case 7:
                    if(trophy && !trophy.IsDestroying && !trophy.IsDeinitializing)
                    { trophy.Rig.position=trigger.bounds.center; trophy.Rig.linearVelocity=Vector3.zero; trophy.Rig.angularVelocity=Vector3.zero; }
                    current.delivered=NPCManager.Instance.NpcIsHoldingItem(npc.ID) && NPCManager.Instance.GetShowingQuest(npc.ID) == quest;
                    current.itemConsumed=!trophy || trophy.IsDestroying || trophy.IsDeinitializing;
                    if(current.delivered && current.itemConsumed)
                    {
                        var reward=Field<QuestInteractable>(npc,"questInteractable");
                        if(reward && reward.gameObject.activeInHierarchy) { reward.Interact(player); stage=8; due=Time.time+1; return; }
                    }
                    if(Time.time>deadline) throw new TimeoutException("Original trophy delivery/reward interactable failed.");
                    break;
                case 8:
                    current.rewardTaken=!NPCManager.Instance.NpcIsHoldingItem(npc.ID);
                    current.maxAfter=OnlineIslandManager.MaxIslandUnlocked;
                    current.progressChanged=quest.Type == QuestType.FinalBoss ? NPCManager.Instance.FinalBossKilled : current.maxAfter == quest.IslandToUnlock && current.maxAfter>current.maxBefore;
                    SaveManager.SaveServer(true);
                    foreach(var json in SaveSystem.LoadAllServers())
                    {
                        var saved=JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                        if(saved != null && saved.Name == SaveManager.CurServerSave.Name) current.progressSaved=quest.Type == QuestType.FinalBoss ? saved.FinalBossKilled : saved.MaxIsland == current.maxAfter;
                    }
                    if(!current.rewardTaken || !current.progressChanged || !current.progressSaved) throw new InvalidOperationException("Original quest reward did not change and persist world progress.");
                    index++; trophy=null; boss=null;
                    if(index<BossNames.Length) { stage=1; due=Time.time+1; }
                    else { report.finalBossReward=NPCManager.Instance.FinalBossKilled; stage=9; due=Time.time+1; }
                    break;
                case 9:
                    var end=UnityEngine.Object.FindFirstObjectByType<EndGameInteractable>(FindObjectsInactive.Include);
                    if(!end || !end.gameObject.activeInHierarchy) throw new InvalidOperationException("Original end-game interactable unavailable after final trophy reward.");
                    end.Interact(player); stage=10; due=Time.time+8; break;
                case 10:
                    report.finishedAuthority=EndGameManager.Instance._hasFinishedGame.Value;
                    report.finishedClient=EndGameManager.Instance.HasFinishedGame;
                    report.creditsUi=EndGameUI.IsShowingEndGame;
                    report.creditsEffects=EndGameEffects.IsShowingEndGame;
                    SaveManager.SaveServer(true);
                    foreach(var json in SaveSystem.LoadAllServers())
                    {
                        var saved=JsonUtility.FromJson<SaveManager.ServerSaveObject>(json);
                        if(saved != null && saved.Name == SaveManager.CurServerSave.Name) report.finishSaved=saved.HasFinishedGame && saved.FinalBossKilled && saved.MaxIsland == OnlineIslandManager.MaxIslandUnlocked;
                    }
                    report.completed=true; Finish(); break;
            }
        }
        catch(Exception error) { if(report == null) report=new Report(); report.error=error.ToString(); if(current != null) current.error=error.Message; Finish(); }
    }
    static void Finish()
    {
        stage=11;
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/boss-progression-probe.json",JsonUtility.ToJson(report,true));
    }
}
