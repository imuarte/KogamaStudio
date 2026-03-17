using HarmonyLib;
using MV.WorldObject;

using UnityEngine;

namespace KogamaStudio.Explorer;

[HarmonyPatch]
internal static class ExplorerPatches
{
    private static bool _refreshPending = false;

    static ExplorerPatches()
    {
        WOReciever.OnWORecieved += (wo, _) => ScheduleRefresh();
    }

    internal static void ScheduleRefresh()
    {
        if (!_refreshPending)
        {
            _refreshPending = true;
            KogamaStudioBehaviour.StartCo(RefreshDebounce());
        }
    }

    private static System.Collections.IEnumerator RefreshDebounce()
    {
        yield return new WaitForSeconds(0.01f);
        _refreshPending = false;
        ExplorerManager.Refresh();
    }

    [HarmonyPatch(typeof(MVWorldObjectClientManagerNetwork), "DestroyWO")]
    [HarmonyPostfix]
    private static void DestroyWO()
    {
        ScheduleRefresh();
    }

}
