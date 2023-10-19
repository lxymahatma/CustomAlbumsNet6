using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;

namespace CustomAlbums.Patches;

/// <summary>
///     Stops the game from thinking there are 1000 album JSONs in consecutive order.
///     Makes a huge difference for systems without absurd disk read speeds.
///     Also stops the music index and search bar from crashing on use.
/// </summary>
[HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitDatas))]
internal static class AlbumCountPatch
{
    private static void Postfix()
    {
        var config = Singleton<ConfigManager>.instance;
        var albums = config.GetConfigObject<DBConfigAlbums>();

        // Custom album + 2 "virtual" albums for internal use
        albums.m_MaxAlbumUid = albums.count - 3;
    }
}