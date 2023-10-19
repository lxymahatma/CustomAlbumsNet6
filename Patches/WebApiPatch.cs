using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using CustomAlbums.Managers;
using HarmonyLib;
using Il2CppSystem;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    class WebApiPatch
    {
        private static readonly Logger Logger = new(nameof(WebApiPatch));

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.SendToUrl))]
        internal class SendToUrlPatch
        {
            private static bool Prefix(string url, string method, Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> datas)
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
}