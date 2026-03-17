
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace KogamaStudio.Debugging
{
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "UpdateWorldObjectDataPartial",
        new System.Type[] { typeof(int), typeof(Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>) })]
    class DebugUpdateDictPatch
    {
        static void Prefix(int worldObjectID, Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object> woData)
        {
            var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(worldObjectID);
            if (wo != null)
            {
                KogamaStudio.Log.LogInfo($"[DEBUG] {wo.GetType().Name} (ID:{worldObjectID}):");

                if (woData != null && woData.Count > 0)
                {
                    foreach (var kvp in woData)
                    {
                        string key = kvp.Key?.ToString() ?? "null";
                        KogamaStudio.Log.LogInfo($"  Key: '{key}'");
                    }
                }
            }
        }
    }
}
