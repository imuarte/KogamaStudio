using HarmonyLib;

namespace KogamaStudio.Camera;

[HarmonyPatch(typeof(MVCameraController), nameof(MVCameraController.SetCamera), new[] { typeof(CameraType) })]
internal class CameraModePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref CameraType cameraType)
    {
        if (CameraModeModifier.Enabled)
            cameraType = CameraModeModifier.Mode;
    }
}
