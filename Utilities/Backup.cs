using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomAlbums.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace CustomAlbums.Utilities;

public static class Backup
{
    private static readonly Logger Log = new(nameof(Backup));
    private static string BackupPath => Path.Combine(Directory.GetCurrentDirectory(), @"UserData\SavesBackup");
    private static string BackupVanilla => Path.Combine(BackupPath, "vanilla.sav.bak");
    private static string BackupVanillaDebug => Path.Combine(BackupPath, "vanilla-debug.json.bak");
    private static string BackupCustom => Path.Combine(BackupPath, "CustomAlbums.json.bak");
    private static string BackupZip => Path.Combine(BackupPath, "backups.zip");
    private static TimeSpan MaxBackupTime => TimeSpan.FromDays(30);

    public static void BackupSaves()
    {
        Directory.CreateDirectory(BackupPath);

        CompressBackups();
        CreateBackup(BackupVanilla, Singleton<DataManager>.instance.ToBytes());
        CreateBackup(BackupVanillaDebug, JsonSerializer.Serialize(DataToJsonObject(Singleton<DataManager>.instance.datas)));
        if (ModSettings.SavingEnabled) CreateBackup(BackupCustom, JsonSerializer.Serialize(SaveManager.CustomData));
        ClearOldBackups();
    }

    /// <summary>
    ///     Create backup files for the given data
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="data"></param>
    private static void CreateBackup(string filePath, object data)
    {
        try
        {
            if (data == null)
            {
                Log.Warning("Could not create backup of null data!");
                return;
            }

            var wroteFile = false;
            switch (data)
            {
                case string str:
                    File.WriteAllText(filePath, str);
                    wroteFile = true;
                    break;

                case byte[] bytes:
                    File.WriteAllBytes(filePath, bytes);
                    wroteFile = true;
                    break;

                case Il2CppStructArray<byte> ilBytes:
                    File.WriteAllBytes(filePath, ilBytes);
                    wroteFile = true;
                    break;

                default:
                    Log.Warning("Could not create backup for unsupported data type " + data.GetType().FullName);
                    break;
            }

            if (wroteFile) Log.Msg($"Saved backup: {filePath}");
        }
        catch (Exception e)
        {
            Log.Error("Backup failed: " + e);
        }
    }

    /// <summary>
    ///     Remove old backup files
    /// </summary>
    private static void ClearOldBackups()
    {
        try
        {
            var backups = Directory.EnumerateFiles(BackupPath);

            foreach (var oldBackupFile in backups.Where(IsBackupTooOld))
            {
                Log.Msg($"Removing old backup: {oldBackupFile}");
                File.Delete(oldBackupFile);
            }

            if (!File.Exists(BackupZip)) return;
            using var zip = ZipFile.Open(BackupZip, ZipArchiveMode.Update);
            foreach (var entry in zip.Entries)
            {
                if ((DateTime.Now - entry.LastWriteTime).Duration() <= MaxBackupTime.Duration()) continue;
                Log.Msg("Removing compressed old backup: " + entry.Name);
                entry.Delete();
            }
        }
        catch (Exception e)
        {
            Log.Error("Clearing old backups failed: " + e.Message);
        }
    }

    /// <summary>
    ///     Check if the given backup file is too old
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static bool IsBackupTooOld(string filePath)
    {
        var backupDate = File.GetLastWriteTime(filePath);
        return DateTime.Now - backupDate > MaxBackupTime;
    }

    /// <summary>
    ///     Compress all backup files into a single zip file
    /// </summary>
    private static void CompressBackups()
    {
        try
        {
            if (!File.Exists(BackupZip))
            {
                var zipFile = ZipFile.Open(BackupZip, ZipArchiveMode.Create);
                zipFile.Dispose();
            }

            using var zip = ZipFile.Open(BackupZip, ZipArchiveMode.Update);

            var files = Directory.EnumerateFiles(BackupPath).Where(fn => Path.GetExtension(fn) != ".zip");

            foreach (var file in files)
            {
                zip.CreateEntryFromFile(file, File.GetCreationTime(file).ToString("yyyy_MM_dd_H_mm_ss-") + Path.GetFileName(file));
                File.Delete(file);
            }
        }
        catch (Exception e)
        {
            Log.Error("Compressing previous backups failed: " + e.Message);
        }
    }

    /// <summary>
    ///     Converts Il2Cpp IData dict to managed JsonObject dict
    /// </summary>
    /// <param name="datas">Il2Cpp Dictionary of string : IData</param>
    /// <returns>Managed dictionary of string : JsonObject</returns>
    public static Dictionary<string, JsonObject> DataToJsonObject(Il2CppSystem.Collections.Generic.Dictionary<string, IData> datas)
    {
        var dictionary = new Dictionary<string, JsonObject>();
        foreach (var keyValuePair in datas)
        {
            var singletonDataObject = keyValuePair.Value?.TryCast<SingletonDataObject>();
            if (singletonDataObject != null)
                dictionary.Add(keyValuePair.Key, JsonSerializer.Deserialize<JsonObject>(singletonDataObject.ToJson()));
        }

        return dictionary;
    }
}