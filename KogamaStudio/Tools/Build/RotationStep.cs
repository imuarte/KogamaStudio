using HarmonyLib;
using Il2CppUGUI.Desktop.Scripts.EditMode.Gizmo;

namespace KogamaStudio.Tools.Build;


//thanks kogamatools
[HarmonyPatch]
internal static class RotationStep
{
    internal static bool Enabled = false;
    internal static float Step = 15f;

    [HarmonyPatch(typeof(RotationHelper), "RotateStep")]
    [HarmonyPrefix]
    private static void ApplyRotation(ref float rotationSpeed)
    {
        if (Enabled)
        {
            rotationSpeed = Step * Math.Sign(rotationSpeed);
        }
    }
}