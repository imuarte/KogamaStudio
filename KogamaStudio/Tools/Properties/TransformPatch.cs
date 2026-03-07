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
    internal static MVNetworkGame SavedNetworkGame;
    internal static int SavedWorldObjectID;
    internal static int SavedOwnerActorNr;

    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), "TransferOwnership")]
    [HarmonyPrefix]
    internal static void TransferOwnershipPrefix(MVNetworkGame __instance, int worldObjectID, int ownerActorNr, Transform t)
    {
        PropertiesManager.SendProperties(worldObjectID);
        SavedNetworkGame = __instance;
        SavedWorldObjectID = worldObjectID;
        SavedOwnerActorNr = ownerActorNr;
    }
}
