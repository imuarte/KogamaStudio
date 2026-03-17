using HarmonyLib;
using System.Runtime.InteropServices;

namespace KogamaStudio.Camera;


// ty KogamaTools   
[HarmonyPatch]
internal static class CameraDistanceModifier
{
    internal static float distance = 10f;
    internal static float defaultDistance = -1f;

    internal static void ApplyChanges()
    {
        ThirdPersonCamera[] cameras = UnityEngine.Object.FindObjectsOfType<ThirdPersonCamera>();

        foreach (ThirdPersonCamera camera in cameras)
        {
            if (defaultDistance != -1f)
            {
                defaultDistance = camera.distance;
            }
            camera.distanceToAvatar = distance;
        }
    }
}