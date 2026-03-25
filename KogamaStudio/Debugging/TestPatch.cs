using HarmonyLib;

namespace KogamaStudio;

[HarmonyPatch]
internal static class SpawnRoleDebug
{
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), nameof(MVNetworkGame.OperationRequests.SetSpawnRoleBody))]
    [HarmonyPrefix]
    private static void SetSpawnRoleBody_Prefix(int avatarCreatorWoId, int avatarBodyDbId)
    {
        KogamaStudio.Log.LogInfo($"[SpawnRole] SetSpawnRoleBody -> creatorWoId: {avatarCreatorWoId}, bodyDbId: {avatarBodyDbId}");
    }

    [HarmonyPatch(typeof(MVAvatarSpawnRoleCreator), nameof(MVAvatarSpawnRoleCreator.UpdateAvatarBody))]
    [HarmonyPrefix]
    private static void UpdateAvatarBody_Prefix(object[] __args)
    {
        try
        {
            if (__args == null || __args.Length < 1) return;
            var data = __args[0] as dynamic;
            if (data == null) return;
            KogamaStudio.Log.LogInfo($"[SpawnRole] UpdateAvatarBody <- deletedBody: {data.deletedBodyWoId}, addedBody: {data.addedBodyWoId}, deletedProto: {data.deletedProtoBodyWoId}, addedProto: {data.addedProtoBodyWoId}, creatorWoId: {data.spawnRoleCreatorWoId}");
        }
        catch (System.Exception ex)
        {
            KogamaStudio.Log.LogWarning($"[SpawnRole] UpdateAvatarBody log failed: {ex.Message}");
        }
    }
}
