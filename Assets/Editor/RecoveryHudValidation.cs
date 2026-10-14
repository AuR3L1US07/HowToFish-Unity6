using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RecoveryHudValidation
{
    static bool checkedHud;
    static RecoveryHudValidation() {EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying) {checkedHud=false;return;}
        if(checkedHud || !SessionState.GetBool("RecoveryPlayValidation.LocalGame",false) || Time.time<25 || !Player.LocalPlayer) return;
        var hud=Object.FindAnyObjectByType<VitalsUI>();if(!hud)return;
        checkedHud=true;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var health=(Image)typeof(VitalsUI).GetField("_healthImage",flags).GetValue(hud);
        var fullness=(Image)typeof(VitalsUI).GetField("_fullnessImage",flags).GetValue(hud);
        var fullnessMaterial=(Material)typeof(VitalsUI).GetField("_fullnessMat",flags).GetValue(hud);
        hud.SetPlayerFullness(.4f);
        Canvas.ForceUpdateCanvases();
        bool matched=fullness.material==fullnessMaterial;
        File.WriteAllText("CompilationChecks/hud-validation.txt","healthIcon="+health.rectTransform.rect.size+"\nfullnessIcon="+fullness.rectTransform.rect.size+"\nmaterialBinding="+matched+"\nrotationMultiplier="+fullnessMaterial.GetFloat("_Rotation_Multiplier")+"\nshader="+fullnessMaterial.shader.name);
        if(!matched) Debug.LogError("HUD fullness material binding is incorrect.");
    }
}
