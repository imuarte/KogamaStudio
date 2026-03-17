using System;
using HarmonyLib;
using KogamaModFramework.Operations;
using KogamaStudio.Tools.Properties;


namespace KogamaStudio.Objects
{
    [HarmonyPatch]
    internal class WOIdGetter
    {
        public static int? LastSelectedWOId;
        public static SelectionController Instance;

        [HarmonyPatch(typeof(SelectionController), "SelectWO")]
        [HarmonyPostfix]
        internal static void SelectWOPostfix(SelectionController __instance, int id)
        {
            Instance = __instance;
            LastSelectedWOId = id;
            var wo = WorldObjectOperations.GetObject(LastSelectedWOId.Value);
            if (wo == null) return;

            CommandHandler.currentEulerAngles = wo.Rotation.eulerAngles;

            PropertiesManager.SendProperties(LastSelectedWOId.Value);
            PipeClient.SendCommand($"explorer_set_selected|{id}");
        }
    }
}