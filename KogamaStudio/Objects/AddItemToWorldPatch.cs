using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

using UnityEngine;

namespace KogamaStudio.Objects;

[HarmonyPatch]
internal class AddItemToWorldPatch
{
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "AddItemToWorld")]
    [HarmonyPrefix]
    static bool AddItemToWorldPrefix(int itemId, int groupId, Vector3 position, Quaternion rotation, bool localOwner, bool transferOwnershipToServerOnLeave, bool isPreviewItem)
    {
        return true;
    }
}
