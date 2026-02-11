using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KogamaStudio.Tools.Properties;

namespace KogamaStudio.Tools.Properties;

[HarmonyPatch]
internal class TransformPatch
{
    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "TransferOwnership")]
    [HarmonyPrefix]
    internal static void TransferOwnershipPrefix(int worldObjectID, int ownerActorNr, Transform t)
    {
        PropertiesManager.SendProperties(worldObjectID);
    }
}
