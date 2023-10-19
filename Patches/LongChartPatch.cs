using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppDYUnityLib;

namespace CustomAlbums.Patches;

/// <summary>
///     Patches the chart timers to last longer than 4 minutes.
/// </summary>
[HarmonyPatch(typeof(FixUpdateTimer), nameof(FixUpdateTimer.Run))]
internal class LongChartPatch
{
    private static readonly Logger Logger = new(nameof(LongChartPatch));

    private static void Prefix(FixUpdateTimer __instance)
    {
        if (__instance.totalTick >= 24000 && __instance.totalTick < int.MaxValue)
        {
            Logger.Msg("Extending length of timer with length " + __instance.totalTick);
            __instance.totalTick = int.MaxValue;
        }
    }
}