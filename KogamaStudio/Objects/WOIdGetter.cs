using System;
using HarmonyLib;
using Il2Cpp;
using KogamaModFramework.Operations;
using KogamaStudio.Tools.Properties;
using MelonLoader;

namespace KogamaStudio.Objects
{
    [HarmonyPatch]
    internal class WOIdGetter
    {
        public static int? LastSelectedWOId;

        [HarmonyPatch(typeof(SelectionController), "Select",
    new Type[] { typeof(VoxelHit), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        internal static void SelectPostfix(WorldObjectClientRef __result)
        {
            if (__result == null) return;

            LastSelectedWOId = __result.woId;
            var wo = WorldObjectOperations.GetObject(LastSelectedWOId.Value);
            if (wo == null) return;

            CommandHandler.currentEulerAngles = wo.Rotation.eulerAngles;

            PropertiesManager.SendProperties(LastSelectedWOId.Value);
        }
    }
}