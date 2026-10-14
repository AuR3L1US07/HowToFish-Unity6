using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class RecoveryFontRepair
{
    [MenuItem("Recovery/Repair Font Atlas Pixels")]
    public static void RepairAtlasPixels()
    {
        int count=0;
        foreach(var guid in AssetDatabase.FindAssets("t:TMP_FontAsset",new[] {"Assets"}))
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if(!font || !font.sourceFontFile || font.atlasPopulationMode==AtlasPopulationMode.Static) continue;
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            font.ClearFontAssetData();
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?;:'\"+-*/=()[]%_<>\\",out string missing);
            foreach(var atlas in font.atlasTextures) if(atlas) EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(font);
            count++;
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("CompilationChecks/font-atlas-pixel-repair.txt","Regenerated and saved: "+count);
        Debug.Log("RECOVERY_FONT_PIXELS_REPAIRED "+count);
    }
    public static void Run()
    {
        Directory.CreateDirectory("Assets/RecoveredFontAtlases");
        Directory.CreateDirectory("ReferenceOnly/BeforeFontRepair");
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null || font.sourceFontFile == null || font.atlasPopulationMode == AtlasPopulationMode.Static) continue;
            var backup = "ReferenceOnly/BeforeFontRepair/" + path.Replace('/', '_');
            if (File.Exists(backup)) continue;
            File.Copy(path, backup);
            var oldTextures = font.atlasTextures;
            var fallback = font.fallbackFontAssetTable;
            var material = font.material;
            var replacement = TMP_FontAsset.CreateFontAsset(font.sourceFontFile, (int)font.faceInfo.pointSize,
                font.atlasPadding, font.atlasRenderMode, font.atlasWidth, font.atlasHeight, AtlasPopulationMode.Dynamic, true);
            replacement.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?;:'\"+-*/=()[]%_<>\\", out string missing);
            var atlas = replacement.atlasTexture;
            AssetDatabase.CreateAsset(atlas, "Assets/RecoveredFontAtlases/" + guid + ".asset");
            foreach (var materialGuid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                var matPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || !mat.HasProperty("_MainTex") || System.Array.IndexOf(oldTextures, mat.mainTexture) < 0) continue;
                var matBackup = "ReferenceOnly/BeforeFontRepair/" + matPath.Replace('/', '_');
                if (!File.Exists(matBackup)) File.Copy(matPath, matBackup);
                mat.mainTexture = atlas;
                mat.SetFloat("_TextureWidth", atlas.width);
                mat.SetFloat("_TextureHeight", atlas.height);
                mat.SetFloat("_GradientScale", replacement.material.GetFloat("_GradientScale"));
                EditorUtility.SetDirty(mat);
            }
            var generatedMaterial = replacement.material;
            replacement.material = material;
            replacement.fallbackFontAssetTable = fallback;
            replacement.name = font.name;
            EditorUtility.CopySerialized(replacement, font);
            EditorUtility.SetDirty(font);
            Object.DestroyImmediate(generatedMaterial);
            // Do not destroy the temporary font: TMP OnDestroy clears the atlas shared by
            // the saved font. Let Unity dispose of this object when the editor process exits.
            count++;
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("CompilationChecks/font-repair.txt", "Regenerated dynamic font assets: " + count);
        Debug.Log("RECOVERY_FONT_REPAIR " + count);
    }
}
