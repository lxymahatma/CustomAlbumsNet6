using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace CustomAlbums.Patches
{
    /// <summary>
    /// Patches the DJMax victory screen to enable the scrolling song title object.
    /// Fixes a vanilla bug where this object does not enable automatically.
    /// </summary>
    [HarmonyPatch(typeof(PnlVictory), nameof(PnlVictory.OnVictory), typeof(Il2CppSystem.Object), typeof(Il2CppSystem.Object), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
    internal static class DjMaxVictoryPatch
    {
        // ReSharper disable once InconsistentNaming
        private static void Postfix(PnlVictory __instance)
        {
            if (__instance.m_CurControls.mainPnl.transform.parent.name != "Djmax") return;
            var titleObj = __instance.m_CurControls.mainPnl.transform.Find("PnlVictory_3D").Find("SongTittle").Find("ImgSongTittleMask");
            var titleNormalTxt = titleObj.Find("TxtSongTittle").gameObject;

            // If the normal title text isn't active, then the scrollable text should be
            if (titleNormalTxt.active) return;
            var titleScrollTxt = titleObj.Find("MaskPos").gameObject;
            titleScrollTxt.SetActive(true);
        }
    }
}