using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;

namespace CustomAlbums.Patches;

public static class SavePatch
{
    private static readonly Logger Logger = new(nameof(SavePatch));

    /// <summary>
    ///     Remove custom data before saving, and restore custom data after saving.
    /// </summary>
    [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.SaveLocal))]
    internal class SaveLocalPatch
    {
        private static void Prefix()
        {
            try
            {
                if (ModSettings.SavingEnabled) SaveManager.Save();
                SaveManager.RemoveCustomData();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        private static void Postfix()
        {
            try
            {
                SaveManager.UpdateCustomData();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }

    /// <summary>
    ///     Add custom data after loading save file.
    /// </summary>
    [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.LoadLocal))]
    internal class LoadLocalPatch
    {
        private static void Postfix()
        {
            try
            {
                Backup.BackupSaves();
                SaveManager.UpdateCustomData();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }

    /// <summary>
    ///     Refresh data after cloud sync
    /// </summary>
    [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.RefreshDatas))]
    internal class RefreshDatasPatch
    {
        private static void Prefix()
        {
            try
            {
                Backup.BackupSaves();

                if (ModSettings.SavingEnabled) SaveManager.Save();
                SaveManager.RemoveCustomData();
                SaveManager.UpdateCustomData();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }

    /// <summary>
    ///     Before a callback, remove custom data
    ///     After a callback, re-add custom data
    /// </summary>
    [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.OnSaveSelectCallback))]
    internal class OnSaveSelectCallbackPatch
    {
        private static void Prefix(ref bool isLocal)
        {
            if (!isLocal) return;
            if (ModSettings.SavingEnabled) SaveManager.Save();
            SaveManager.RemoveCustomData();
        }

        private static void Postfix(ref bool isLocal)
        {
            if (isLocal) SaveManager.UpdateCustomData();
        }
    }
}