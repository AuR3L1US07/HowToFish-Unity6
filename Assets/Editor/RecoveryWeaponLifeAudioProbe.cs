using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class RecoveryWeaponLifeAudioProbe
{
    [Serializable] public class Report
    {
        public bool completed, weaponHeld, mouseFired, projectileEmitted, reloadInput, died, corpseSpawned, mouseRespawned, healthRestored, masterVolumeApplied, fxVolumeApplied, audioClipPlayed;
        public uint projectileIdBefore, projectileIdAfter;
        public int ammoBefore, ammoAfter, ammoReloaded, healthAfterRespawn;
        public float masterDb, fxDb;
        public string error, detail;
    }
    static int stage;
    static float due;
    static Weapon weapon;
    static Mouse mouse;
    static Keyboard keyboard;
    static Report report;
    static AudioMixer mixer;
    static float oldMaster, oldFx, oldBypass;
    static RecoveryWeaponLifeAudioProbe() { EditorApplication.update+=Tick; }
    static void MouseButton(bool pressed) { InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,pressed)); }
    static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { stage=0; due=0; report=null; weapon=null; return; }
        if(stage>8 || Time.time<137 || Time.time<due || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false)) return;
        var player=Player.LocalPlayer;
        if(!player || MainMenuManager.IsInMenu) return;
        try
        {
            switch(stage)
            {
                case 0:
                    report=new Report();
                    if(SaveManager.CurServerSave == null || !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal)) throw new InvalidOperationException("Dedicated validation world required.");
                    if(player.Holding.HeldItem || Boat.IsDrivingLocally) throw new InvalidOperationException("Empty hands and stopped boat required.");
                    mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>(); keyboard=Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                    InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keyboard);
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    GameInfo.Input.SwitchCurrentControlScheme("Keyboard",keyboard,mouse);
                    var prefab=Resources.Load<GameObject>("items/purchasable/Pistol");
                    if(!prefab) throw new InvalidOperationException("Pistol prefab missing.");
                    weapon=UnityEngine.Object.Instantiate(prefab,player.Transform.position+Vector3.up,Quaternion.identity).GetComponent<Weapon>();
                    Server.Instance.Spawn(weapon.gameObject); stage=1; due=Time.time+1; break;
                case 1:
                    weapon.PickUp(player,true,true); stage=2; due=Time.time+1; break;
                case 2:
                    report.weaponHeld=player.Holding.HeldItem == weapon;
                    report.ammoBefore=weapon.Ammo;
                    report.projectileIdBefore=(uint)typeof(ProjectileManager).GetField("_nextId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ProjectileManager.Instance);
                    MouseButton(true); stage=3; due=Time.time+0.2f; break;
                case 3:
                    MouseButton(false); report.ammoAfter=weapon.Ammo;
                    report.mouseFired=report.weaponHeld && report.ammoAfter<report.ammoBefore;
                    report.projectileIdAfter=(uint)typeof(ProjectileManager).GetField("_nextId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ProjectileManager.Instance);
                    report.projectileEmitted=report.projectileIdAfter>report.projectileIdBefore;
                    stage=4; due=Time.time+2; break;
                case 4:
                    Key reload=Key.None;
                    foreach(var binding in GameInfo.Input.actions["WeaponReload"].bindings)
                        if(binding.effectivePath.StartsWith("<Keyboard>/",StringComparison.OrdinalIgnoreCase) && Enum.TryParse(binding.effectivePath.Substring(11),true,out reload)) break;
                    if(reload == Key.None) throw new InvalidOperationException("WeaponReload keyboard binding missing.");
                    Keys(reload); stage=5; due=Time.time+0.2f; break;
                case 5:
                    Keys(); stage=6; due=Time.time+7; break;
                case 6:
                    report.ammoReloaded=weapon.Ammo;
                    report.reloadInput=report.mouseFired && report.ammoReloaded == weapon.Attachments.AmmoPerMag && report.ammoReloaded>report.ammoAfter;
                    var audio=UnityEngine.Object.FindFirstObjectByType<AudioManager>();
                    mixer=(AudioMixer)typeof(AudioManager).GetField("_mainMixer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);
                    if(!mixer || !mixer.GetFloat("Master",out oldMaster) || !mixer.GetFloat("FX",out oldFx) || !mixer.GetFloat("FXBypassMute",out oldBypass)) throw new InvalidOperationException("Exposed mixer parameters missing.");
                    AudioManager.SetVolume(0.5f,VolumeType.Master); AudioManager.SetVolume(0.25f,VolumeType.FX);
                    stage=7; due=Time.time+0.3f; break;
                case 7:
                    report.masterVolumeApplied=mixer.GetFloat("Master",out report.masterDb) && Mathf.Abs(report.masterDb-20*Mathf.Log10(0.5f))<0.05f;
                    report.fxVolumeApplied=mixer.GetFloat("FX",out report.fxDb) && Mathf.Abs(report.fxDb-20*Mathf.Log10(0.25f))<0.05f;
                    AudioManager.PlayGlobalClip("AdsIn",false,1,0);
                    var source=(AudioSource)typeof(AudioManager).GetField("_globalSource",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                    report.audioClipPlayed=AudioManager.GetClip("AdsIn") != null && source && source.isPlaying;
                    mixer.SetFloat("Master",oldMaster); mixer.SetFloat("FX",oldFx); mixer.SetFloat("FXBypassMute",oldBypass);
                    player.Vitals.TakeDamage(10000,player.Transform.position,Vector3.zero,true);
                    stage=8; due=Time.time+1; break;
                case 8:
                    report.died=player.Dying.IsDead && player.Vitals.Health == 0;
                    report.corpseSpawned=player.Dying.DeadPlayer && player.Dying.DeadPlayer.IsServerInitialized;
                    if(!report.died) throw new InvalidOperationException("Lethal damage did not cause death.");
                    MouseButton(true); stage=9; due=Time.time+4; EditorApplication.update+=FinishAfterRespawn; break;
            }
        }
        catch(Exception error) { if(report == null) report=new Report(); report.error=error.ToString(); Finish(); }
    }
    static void FinishAfterRespawn()
    {
        if(!EditorApplication.isPlaying || Time.time<due) return;
        EditorApplication.update-=FinishAfterRespawn;
        var player=Player.LocalPlayer;
        MouseButton(false);
        report.healthAfterRespawn=player ? player.Vitals.Health : 0;
        report.mouseRespawned=player && !player.Dying.IsDead;
        report.healthRestored=report.healthAfterRespawn>0;
        report.completed=true;
        report.detail="Original Pistol spawn/pickup fixture; actual mouse firing and keyboard reload; actual TakeDamage death and held mouse give-up respawn; audio mixer roundtrip plus original clip source playback. Projectile collision/boss defeat and audible sound output remain separate verification.";
        Finish();
    }
    static void Finish()
    {
        stage=10; if(mouse != null) MouseButton(false); if(keyboard != null) Keys();
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/weapon-life-audio-probe.json",JsonUtility.ToJson(report,true));
    }
}
