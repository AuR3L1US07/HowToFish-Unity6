using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RecoveryScriptInventory
{
    [Serializable] public class Entry { public string guid; public long fileID; public string type; public string assembly; public string path; }
    [Serializable] public class Inventory { public List<Entry> entries = new List<Entry>(); }
    public static void Export()
    {
        var inventory = new Inventory();
        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/Plugins/") || !path.EndsWith(".dll")) continue;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var script = obj as MonoScript;
                if (script == null) continue;
                var type = script.GetClass();
                if (type == null) continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long id))
                    inventory.entries.Add(new Entry { guid = guid, fileID = id, type = type.FullName, assembly = type.Assembly.GetName().Name, path = path });
            }
        }
        Directory.CreateDirectory("CompilationChecks");
        File.WriteAllText("CompilationChecks/plugin-script-inventory.json", JsonUtility.ToJson(inventory, true));
        Debug.Log("RECOVERY_SCRIPT_INVENTORY_OK " + inventory.entries.Count);
    }
}
