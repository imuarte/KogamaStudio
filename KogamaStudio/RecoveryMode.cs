using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;


namespace KogamaStudio;

[HarmonyPatch]
internal class RecoveryMode
{
    internal static bool RecoveryModeEnabled = false;
    internal static int TargetItemId = -1;
    internal static LoadingScreenHandler LoadingScreenHandlerInstance;

    internal static void EnableRecoveryMode()
    {
        if (RecoveryModeEnabled) return;
        MelonLogger.Msg("Recovery Mode Enabled!");
        PipeClient.SendCommand("game_initialized");
        PipeClient.SendCommand("key_down|F2");
        PipeClient.SendCommand("recovery_mode");
        RecoveryModeEnabled = true;
    }

    internal static void DisableLoadingScreen()
    {
        LoadingScreenHandlerInstance.OnDestroy();
    }

    [HarmonyPatch(typeof(LoadingScreenHandler), "Start")]
    [HarmonyPrefix]
    internal static void LoadingScreenStartPrefix(LoadingScreenHandler __instance)
    {
        LoadingScreenHandlerInstance = __instance;
    }
}
