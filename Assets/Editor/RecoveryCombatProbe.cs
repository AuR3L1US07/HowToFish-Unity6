using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RecoveryCombatProbe
{
    [Serializable] public class Report
    {
        public bool completed, creatureSpawned, damageApplied, damageSynced;
        public int healthBefore, authorityAfter, clientAfter;
        public string detail, error;
    }
    static int stage;
    static float due;
    static Creature creature;
    static Report report;
    static RecoveryCombatProbe() { EditorApplication.update+=Tick; }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { stage=0; due=0; creature=null; report=null; return; }
        if(stage>2 || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false) || Time.time<36 || Time.time<due) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            if(stage == 0)
            {
                report=new Report();
                if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal))
                    throw new InvalidOperationException("Dedicated validation save required.");
                Creature prefab=null;
                foreach(var candidate in Resources.LoadAll<GameObject>("items/creatures"))
                {
                    var current=candidate.GetComponent<Creature>();
                    if(current && current.BossType == BossType.None && current.MaxHp>2) { prefab=current; break; }
                }
                if(!prefab) throw new InvalidOperationException("No non-boss creature with enough HP found.");
                creature=UnityEngine.Object.Instantiate(prefab,player.Transform.position+player.Transform.forward*3+Vector3.up*2,Quaternion.identity);
                Server.Instance.Spawn(creature.gameObject); stage=1; due=Time.time+0.7f;
            }
            else if(stage == 1)
            {
                report.creatureSpawned=creature.IsServerInitialized && creature.IsClientInitialized;
                report.healthBefore=creature._hp.Value;
                if(report.healthBefore<=1) throw new InvalidOperationException("Creature cannot receive nonlethal test damage.");
                creature.LocalHit(creature.transform,creature.transform.position,Vector3.forward,player,1,false);
                stage=2; due=Time.time+0.7f;
            }
            else
            {
                report.authorityAfter=creature._hp.Value; report.clientAfter=creature.Hp;
                report.damageApplied=report.authorityAfter == report.healthBefore-1;
                report.damageSynced=report.clientAfter == report.authorityAfter;
                report.completed=true;
                report.detail="Actual Creature.LocalHit to server HitCreature RPC, nonlethal damage only. Weapon input, kill rewards and bosses remain unverified.";
                Finish();
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
        stage=3;
        if(creature) Server.Instance.Despawn(creature.gameObject);
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/combat-probe.json",JsonUtility.ToJson(report,true));
    }
}
