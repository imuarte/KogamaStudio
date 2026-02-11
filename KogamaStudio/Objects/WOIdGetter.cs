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

        [HarmonyPatch(typeof(SelectionController), "SelectWO")]
        [HarmonyPostfix]
        internal static void SelectWOPostfix(int id)
        {
            LastSelectedWOId = id;
            var wo = WorldObjectOperations.GetObject(LastSelectedWOId.Value);
            if (wo == null) return;

            CommandHandler.currentEulerAngles = wo.Rotation.eulerAngles;

            PropertiesManager.SendProperties(LastSelectedWOId.Value);
        }
    }
}