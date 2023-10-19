using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using Object = Il2CppSystem.Object;

namespace CustomAlbums.Patches;

internal static class WebApiPatch
{
    private static readonly Logger Logger = new(nameof(WebApiPatch));

    [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.SendToUrl))]
    internal static class SendToUrlPatch
    {
        private static bool Prefix(string url, string method, Il2CppSystem.Collections.Generic.Dictionary<string, Object> datas)
        {
            Logger.Msg($"[SendToUrlPatch] url:{url} method:{method}");

            switch (url)
            {
                case "statistics/pc-play-statistics-feedback":
                    if (datas["music_uid"].ToString().StartsWith($"{AlbumManager.Uid}"))
                    {
                        Logger.Msg("[SendToUrlPatch] Blocked play feedback upload:" + datas["music_uid"].ToString());
                        return false;
                    }

                    break;
                case "musedash/v2/pcleaderboard/high-score":
                    if (GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}"))
                    {
                        Logger.Msg("[SendToUrlPatch] Blocked high score upload:" + GlobalDataBase.dbBattleStage.musicUid);
                        return false;
                    }

                    break;
            }

            return true;
        }
    }
}