using HarmonyLib;
using Il2Cpp;

// ty KogamaTools.Native
namespace KogamaStudio.Tools.Build;

[HarmonyPatch]
internal static class BlueModeToggle
{
    internal static bool BlueModeEnabled = true;

    [HarmonyPatch(typeof(MainCameraManager), nameof(MainCameraManager.BlueModeEnabled), MethodType.Setter)]
    [HarmonyPrefix]
    private static void SetBlueModeEnabled(ref bool value)
    {
        value = value && BlueModeEnabled;
    }
}