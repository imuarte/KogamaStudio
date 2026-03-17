using HarmonyLib;
using KogamaStudio.Objects;
using UnityEngine;

namespace KogamaStudio.History;

[HarmonyPatch]
internal class TransformPatch
{
    private static MVWorldObjectClient _targetRecord;
    private static Vector3 _oldPosRecord;

    [HarmonyPatch(typeof(ESTranslate), "Enter")]
    [HarmonyPrefix]
    private static void EnterPrefix(EditorStateMachine e)
    {
        if (e.SingleSelectedWO == null) return;
        _targetRecord = e.SingleSelectedWO;
        _oldPosRecord = e.SingleSelectedWO.transform.position;
    }

    [HarmonyPatch(typeof(ESTranslate), "Exit")]
    [HarmonyPrefix]
    private static void ExitPrefix(EditorStateMachine e)
    {
        if (_targetRecord == null) return;
        var newPos = _targetRecord.transform.position;
        _targetRecord = null;

        if (Vector3.Distance(_oldPosRecord, newPos) < 0.05f) return;

        var id = WOIdGetter.LastSelectedWOId ?? -1;
        var o = _oldPosRecord;
        var cmd = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "history_transform|{0}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}",
            id, o.x, o.y, o.z, newPos.x, newPos.y, newPos.z);
        PipeClient.SendCommand(cmd);
    }
}
