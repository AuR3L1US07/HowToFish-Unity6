using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RecoveryInputAudit
{
    [Serializable] public class Result {
        public string value, label, font, contentType;
        public bool focused, active, culled, atlasMatches;
        public int characters, visibleCharacters;
        public Vector4 clip;
        public Rect textRect, viewportRect;
        public Color color;
    }
    static double last;
    static RecoveryInputAudit() { EditorApplication.update += Update; }
    static void Update()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup-last < 2) return;
        last=EditorApplication.timeSinceStartup;
        foreach (var field in UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (field.name != "ServerNameInputField") continue;
            var text=field.textComponent;
            var mat=text.materialForRendering;
            int visible=0;
            for(int i=0;i<text.textInfo.characterCount;i++) if(text.textInfo.characterInfo[i].isVisible) visible++;
            var result=new Result {
                value=field.text,label=text.text,font=text.font.name,contentType=field.contentType.ToString(),focused=field.isFocused,
                active=field.gameObject.activeInHierarchy,culled=text.canvasRenderer.cull,atlasMatches=mat.mainTexture==text.font.atlasTexture,
                characters=text.textInfo.characterCount,visibleCharacters=visible,clip=mat.HasProperty("_ClipRect")?mat.GetVector("_ClipRect"):Vector4.zero,
                textRect=text.rectTransform.rect,viewportRect=field.textViewport.rect,color=text.color
            };
            File.WriteAllText("CompilationChecks/input-field-runtime.json",JsonUtility.ToJson(result,true));
        }
    }
}
