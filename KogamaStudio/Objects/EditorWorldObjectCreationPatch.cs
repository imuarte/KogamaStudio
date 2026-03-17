using HarmonyLib;
using UnityEngine;



namespace KogamaStudio.Objects
{
    [HarmonyPatch]
    internal class EditorWorldObjectCreationPatch
    {
        public static EditorWorldObjectCreation savedEditorWOC;

        [HarmonyPatch(typeof(EditorWorldObjectCreation), "Initialize")]
        [HarmonyPostfix]
        private static void SendPackagePrefix(EditorWorldObjectCreation __instance)
        {
            savedEditorWOC = __instance;
            KogamaStudio.Log.LogInfo("EditorWorldObjectCreation captured on Initialize!");
        }
    }
}
