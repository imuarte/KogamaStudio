using HarmonyLib;
using Il2Cpp;

// ty KogamaTools.Native
namespace KogamaStudio.Tools.Build;

[HarmonyPatch(typeof(MVMaterial), nameof(MVMaterial.IsAvailable), MethodType.Getter)]
internal static class DestructiblesUnlock
{
    internal static bool Unlocked = false;

    [HarmonyPrefix]
    private static bool IsAvailable(ref bool __result)
    {
        if (Unlocked)
        {
            __result = true;
            return false;
        }
        return true;
    }
}