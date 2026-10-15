using System;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;

/// <summary>负责存档文件校验、临时文件替换及备份恢复。</summary>
public static class SaveSystem
{
#if UNITY_EDITOR
    private static readonly string _saveRootFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../EditorRuntime/Saves"));
#else
	private static readonly string _saveRootFolder = !string.IsNullOrEmpty(RecoveryStandaloneValidation.Argument("-recovery-save-root"))
		? Path.GetFullPath(RecoveryStandaloneValidation.Argument("-recovery-save-root"))
		: LocalPlayMode.Enabled ? Path.Combine(Application.persistentDataPath, "RecoveryLocal", "Saves") : Path.Combine(Application.persistentDataPath, "Saves");
#endif

    private static readonly string _localFileName = "local";

    private static readonly string _extension = ".txt";

    private static string _saveFolder;

    private static string _fullLocalSavePath;

    private static string _localBackupPath;

    private static string _localTempPath;

    public static void Init()
    {
        string path = LocalPlayMode.UserId.m_SteamID.ToString();
        _saveFolder = Path.Combine(_saveRootFolder, path);
        _fullLocalSavePath = Path.Combine(_saveFolder, _localFileName + _extension);
        _localBackupPath = _fullLocalSavePath + ".backup";
        _localTempPath = _fullLocalSavePath + ".tmp";
        Directory.CreateDirectory(_saveRootFolder);
        bool num = !Directory.Exists(_saveFolder) || !HasPrimaryOrBackupSave(_saveFolder);
        Directory.CreateDirectory(_saveFolder);
        if (!num)
        {
            return;
        }

        string[] files = Directory.GetFiles(_saveRootFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (string text in files)
        {
            if (IsSaveFile(text))
            {
                string text2 = Path.Combine(_saveFolder, Path.GetFileName(text));
                try
                {
                    File.Move(text, text2);
                }
                catch (Exception arg)
                {
                    Debug.LogError($"Could not migrate legacy save '{text}' to '{text2}': {arg}");
                }
            }
        }
    }

    private static bool IsSaveFile(string path)
    {
        string extension = Path.GetExtension(path);
        if (!extension.Equals(_extension, StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(".backup", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private static bool HasPrimaryOrBackupSave(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
        foreach (string saveFile in files)
        {
            if (IsSaveFile(saveFile))
            {
                return true;
            }
        }

        return false;
    }

    public static void SaveServer(string name, string saveString)
    {
        if (!IsValidServerSave(saveString, out var error))
        {
            Debug.LogError("Server save '" + name + "' was not written because it is invalid: " + error);
            return;
        }

        string savePath = Path.Combine(_saveFolder, name + _extension);
        string backupPath = savePath + ".backup";
        string temporaryPath = savePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, saveString);
            if (!IsValidServerSave(File.ReadAllText(temporaryPath), out error))
            {
                throw new InvalidDataException("Temporary server save failed validation: " + error);
            }

            if (File.Exists(savePath))
            {
                // Only replace the backup when the current primary save is valid.
                string destinationBackupFileName = TryReadValidServerSave(savePath, out _) ? backupPath : null;
                File.Replace(temporaryPath, savePath, destinationBackupFileName);
            }
            else
            {
                File.Move(temporaryPath, savePath);
            }
        }
        catch (Exception arg)
        {
            Debug.LogError($"Could not safely write server save '{name}': {arg}");
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not remove temporary server save '" + temporaryPath + "': " + ex.Message);
            }
        }
    }

    public static void DeleteServer(string name)
    {
        string savePath = Path.Combine(_saveFolder, name + _extension);
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        string backupPath = savePath + ".backup";
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        string temporaryPath = savePath + ".tmp";
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }

    public static List<string> LoadAllServers()
    {
        FileInfo[] files = new DirectoryInfo(_saveFolder).GetFiles("*" + _extension);
        List<string> serverSaves = new();
        foreach (FileInfo saveFile in files)
        {
            if (saveFile.Name == _localFileName + _extension)
            {
                continue;
            }

            if (TryReadValidServerSave(saveFile.FullName, out var saveString))
            {
                serverSaves.Add(saveString);
                continue;
            }

            if (!TryReadValidServerSave(saveFile.FullName + ".backup", out saveString))
            {
                Debug.LogError($"Neither server save '{saveFile.FullName}' nor its backup could be loaded.");
                continue;
            }

            Debug.LogWarning($"Server save '{saveFile.FullName}' could not be loaded. Recovering its backup.");
            serverSaves.Add(saveString);
            SaveServer(Path.GetFileNameWithoutExtension(saveFile.Name), saveString);
        }

        return serverSaves;
    }

    public static void SaveLocal(string saveString)
    {
        if (!IsValidLocalSave(saveString, out var error))
        {
            Debug.LogError("Local save was not written because it is invalid: " + error);
            return;
        }

        try
        {
            File.WriteAllText(_localTempPath, saveString);
            if (!IsValidLocalSave(File.ReadAllText(_localTempPath), out error))
            {
                throw new InvalidDataException("Temporary local save failed validation: " + error);
            }

            if (File.Exists(_fullLocalSavePath))
            {
                string destinationBackupFileName = (IsValidLocalSave(File.ReadAllText(_fullLocalSavePath), out var _) ? _localBackupPath : null);
                File.Replace(_localTempPath, _fullLocalSavePath, destinationBackupFileName);
            }
            else
            {
                File.Move(_localTempPath, _fullLocalSavePath);
            }
        }
        catch (Exception arg)
        {
            Debug.LogError($"Could not safely write local save: {arg}");
        }
        finally
        {
            try
            {
                if (File.Exists(_localTempPath))
                {
                    File.Delete(_localTempPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not remove temporary local save: " + ex.Message);
            }
        }
    }

    public static string LoadLocal()
    {
        if (TryReadValidLocalSave(_fullLocalSavePath, out var saveString))
        {
            return saveString;
        }

        if (!File.Exists(_localBackupPath))
        {
            return null;
        }

        Debug.LogWarning("local.txt could not be loaded. Attempting to recover the local save backup.");
        if (TryReadValidLocalSave(_localBackupPath, out saveString))
        {
            SaveLocal(saveString);
            return saveString;
        }

        Debug.LogError("Neither local.txt nor its backup contains a valid local save. Using defaults.");
        return null;
    }

    private static bool TryReadValidLocalSave(string path, out string saveString)
    {
        saveString = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            saveString = File.ReadAllText(path);
            if (IsValidLocalSave(saveString, out var error))
            {
                return true;
            }

            Debug.LogWarning("Local save '" + path + "' is invalid: " + error);
        }
        catch (Exception arg)
        {
            Debug.LogWarning($"Could not read local save '{path}': {arg}");
        }

        saveString = null;
        return false;
    }

    private static bool IsValidLocalSave(string saveString, out string error)
    {
        if (string.IsNullOrWhiteSpace(saveString))
        {
            error = "The file is empty.";
            return false;
        }

        try
        {
            if (JsonUtility.FromJson<SaveManager.LocalSaveObject>(saveString) == null)
            {
                error = "JSON produced a null save object.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryReadValidServerSave(string path, out string saveString)
    {
        saveString = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            saveString = File.ReadAllText(path);
            if (IsValidServerSave(saveString, out var error))
            {
                return true;
            }

            Debug.LogWarning("Server save '" + path + "' is invalid: " + error);
        }
        catch (Exception arg)
        {
            Debug.LogWarning($"Could not read server save '{path}': {arg}");
        }

        saveString = null;
        return false;
    }

    private static bool IsValidServerSave(string saveString, out string error)
    {
        if (string.IsNullOrWhiteSpace(saveString))
        {
            error = "The file is empty.";
            return false;
        }

        try
        {
            SaveManager.ServerSaveObject serverSaveObject = JsonUtility.FromJson<SaveManager.ServerSaveObject>(saveString);
            if (serverSaveObject == null)
            {
                error = "JSON produced a null save object.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(serverSaveObject.Name))
            {
                error = "The server name is missing.";
                return false;
            }

            if (serverSaveObject.LastSave <= 0)
            {
                error = "The last-save timestamp is missing or invalid.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
