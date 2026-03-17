using HarmonyLib;

using MV.Common;
using System.Diagnostics;

namespace KogamaStudio.AntiBan;

[HarmonyPatch]
internal static class AntiBan
{
    // tysm beckowl

    [HarmonyPatch(typeof(CheatHandling), "Init")]
    [HarmonyPatch(typeof(CheatHandling), "ExecuteBan")]
    [HarmonyPatch(typeof(CheatHandling), "CheatSoftwareRunningDetected")]
    [HarmonyPatch(typeof(CheatHandling), "TextureHackDetected")]
    [HarmonyPatch(typeof(CheatHandling), "MachineBanDetected")]
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "Ban", [typeof(int), typeof(MVPlayer), typeof(string)])]
    //[HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "Ban", [typeof(CheatType)])]
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "Expel")]
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "Kick")]
    [HarmonyPrefix]
    private static bool NoBan()
    {
        return false;
    }

}