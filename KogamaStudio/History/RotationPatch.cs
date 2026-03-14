using HarmonyLib;
using Il2Cpp;
using Il2CppUGUI.Desktop.Scripts.EditMode.Gizmo;
using KogamaStudio.Objects;
using UnityEngine;

namespace KogamaStudio.History;

[HarmonyPatch]
internal class RotationPatch
{
    private static MVWorldObjectClient _targetRecord;
    private static Quaternion _oldRotRecord;

    [HarmonyPatch(typeof(RotationHelper), "RotateStep")]
    [HarmonyPatch(typeof(RotationHelper), "ResetRotation")]
    [HarmonyPrefix]
    private static void RotatePrefix(RotationHelper __instance)
    {
        var e = __instance.editorStateMachine;
        if (e?.SingleSelectedWO == null) return;
        _targetRecord = e.SingleSelectedWO;
        _oldRotRecord = e.SingleSelectedWO.transform.rotation;
    }

    [HarmonyPatch(typeof(RotationHelper), "RotateStep")]
    [HarmonyPatch(typeof(RotationHelper), "ResetRotation")]
    [HarmonyPostfix]
    private static void RotatePostfix()
    {
        if (_targetRecord == null) return;
        var newRot = _targetRecord.transform.rotation;
        _targetRecord = null;

        if (Quaternion.Angle(_oldRotRecord, newRot) < 0.1f) return;

        var id = WOIdGetter.LastSelectedWOId ?? -1;
        var o = _oldRotRecord;
        var cmd = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "history_rotation|{0}|{1:F5}|{2:F5}|{3:F5}|{4:F5}|{5:F5}|{6:F5}|{7:F5}|{8:F5}",
            id, o.x, o.y, o.z, o.w, newRot.x, newRot.y, newRot.z, newRot.w);
        PipeClient.SendCommand(cmd);
    }
}
