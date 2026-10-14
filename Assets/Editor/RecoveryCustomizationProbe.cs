using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RecoveryCustomizationProbe
{
    [Serializable] public class Check { public string name; public bool passed; public string detail; }
    [Serializable] public class Report { public bool completed; public List<Check> checks=new List<Check>(); }
    static bool ran;
    static RecoveryCustomizationProbe() { EditorApplication.update+=Tick; }
    static void Tick()
    {
        if (SessionState.GetBool("RecoveryBossProgression.Requested", false)) return;
        if(!EditorApplication.isPlaying) { ran=false; return; }
        if(ran || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false) || Time.time<39) return;
        if(!Player.LocalPlayer || MainMenuManager.IsInMenu) return;
        ran=true; Validate();
    }
    [MenuItem("Recovery/Validate Isolated Customization")]
    public static void Validate()
    {
        var report=new Report();
        Action<string,bool,string> check=(name,passed,detail)=>report.checks.Add(new Check{name=name,passed=passed,detail=detail});
        try
        {
            if(!EditorApplication.isPlaying || !Player.LocalPlayer || SaveManager.CurServerSave == null ||
                !SaveManager.CurServerSave.Name.StartsWith("RecoveryLocalValidation",StringComparison.Ordinal))
                throw new InvalidOperationException("Dedicated RecoveryLocalValidation session required.");
            var inventory=Player.LocalPlayer.Inventory;
            byte previous=inventory.WishboneIndex;
            try
            {
                var next=inventory.GetNextWishboneUpgrade();
                if(next == null) check("upgrade-available",false,"No further upgrade exists in this save.");
                else
                {
                    inventory.UpgradeWishbone();
                    check("wishbone-upgrade",inventory.WishboneIndex == previous+1,"Invoked upgrade method; shop payment remains unverified.");
                    inventory.SaveInventory();
                    var player=SaveManager.GetSavedPlayer(Player.LocalPlayer.SteamID);
                    check("wishbone-save",player != null && player.WishboneIndex == inventory.WishboneIndex,"Inventory save must capture upgrade.");
                }
            }
            finally { inventory._syncedWishboneIndex.Value=previous; inventory.SaveInventory(); }
            var boat=UnityEngine.Object.FindFirstObjectByType<Boat>();
            check("boat-present",boat != null,"Loaded world must have a boat.");
            if(boat != null)
            {
                bool unlocked=boat.BoatUnlocked, radar=boat.BoatRadarUnlocked;
                try
                {
                    boat.UnlockBoat(); boat.UnlockBoatRadar();
                    check("boat-unlock",boat.BoatUnlocked && boat.BoatRadarUnlocked,"Actual unlock methods invoked; driving and paid upgrades remain unverified.");
                }
                finally { boat._boatUnlocked.Value=unlocked; boat._boatRadarUnlocked.Value=radar; }
            }
            var fields=new Dictionary<FieldInfo,object>();
            var oldColor=LocalSkin.LocalSkinColor;
            try
            {
                // Redirect all local save files before changing customization. Existing local
                // save and its backup are never overwritten by this round-trip assertion.
                string path=Path.GetFullPath("CompilationChecks/isolated-customization/local.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                foreach(var pair in new Dictionary<string,string>{
                    {"_fullLocalSavePath",path},{"_localBackupPath",path+".backup"},{"_localTempPath",path+".tmp"}})
                {
                    var field=typeof(SaveSystem).GetField(pair.Key,BindingFlags.Static|BindingFlags.NonPublic);
                    if(field == null) throw new MissingFieldException(pair.Key);
                    fields[field]=field.GetValue(null); field.SetValue(null,pair.Value);
                }
                var testColor=new Vector3(0.31f,0.47f,0.63f);
                LocalSkin.SetLocalColor(testColor,PlayerSkinType.Skin,false,false);
                check("skin-color",LocalSkin.LocalSkinColor == testColor,"SetLocalColor applies actual customization state.");
                SaveManager.SaveLocal();
                var loaded=JsonUtility.FromJson<SaveManager.LocalSaveObject>(SaveSystem.LoadLocal());
                check("skin-color-save",loaded != null && loaded.LocalSkinColor == testColor,"Round-trip uses isolated local save path.");
                check("outfit-save",loaded != null && loaded.LocalOutfitIndex == LocalSkin.LocalOutfitIndex && loaded.LocalHatIndex == LocalSkin.LocalHatIndex,
                    "Existing outfit and hat selection serialized; switching meshes remains unverified.");
            }
            finally
            {
                LocalSkin.SetLocalColor(oldColor,PlayerSkinType.Skin,false,false);
                foreach(var entry in fields) entry.Key.SetValue(null,entry.Value);
            }
            report.completed=true;
        }
        catch(Exception error) { check("exception",false,error.ToString()); }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/customization-probe.json",JsonUtility.ToJson(report,true));
    }
}
