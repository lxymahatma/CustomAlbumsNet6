using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.Database.DataClass;

namespace CustomAlbums.Patches
{
    internal class AlbumTagPatch
    {
        /// <summary>
        /// Adds a tag for Custom Albums on the top row.
        /// </summary>
        [HarmonyPatch(typeof(MusicTagManager), "InitAlbumTagInfo")]
        internal static class TagPatch
        {
            public static void Postfix()
            {
                var info = new AlbumTagInfo
                {
                    name = AlbumManager.Languages["English"],
                    tagUid = "tag-custom-albums",
                    iconName = "IconCustomAlbums"
                };
                var customInfo = new DBConfigCustomTags.CustomTagInfo
                {
                    tag_name = AlbumManager.Languages.ToIl2Cpp(),
                    tag_picture = "https://mdmc.moe/img/melon.png",
                    music_list = new Il2CppSystem.Collections.Generic.List<string>()
                };
                foreach (var uid in AlbumManager.GetAllUid()) customInfo.music_list.Add(uid);

                info.InitCustomTagInfo(customInfo);

                GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Insert(GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Count - 4, AlbumManager.Uid);
                GlobalDataBase.dbMusicTag.AddAlbumTagData(AlbumManager.Uid, info);
            }
        }
    }
}
