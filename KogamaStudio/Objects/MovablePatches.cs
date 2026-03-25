using HarmonyLib;
using System;

namespace KogamaStudio.Objects;

[HarmonyPatch]
internal static class MovablePatches
{
    [HarmonyPatch(typeof(MoveableController), nameof(MoveableController.UpdateControllerFixedUpdate))]
    [HarmonyFinalizer]
    private static Exception MoveableController_Finalizer(Exception __exception) => null;

    [HarmonyPatch(typeof(MovableVisualization), nameof(MovableVisualization.UpdateControllerFixedUpdate))]
    [HarmonyFinalizer]
    private static Exception MovableVisualization_Finalizer(Exception __exception) => null;
}
