using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using MelonLoader;


namespace KogamaStudio.Objects
{
    [HarmonyPatch]
    internal class TestPatch2
    {
        //[HarmonyPatch(typeof(EditorWorldObjectCreation), "OnChangePrototypeScale")]
        //[HarmonyPrefix]
        //private static bool SendPackagePrefix(int worldId, ref float scale)
        //{
        //    TextCommand.NotifyUser($"{scale}");
        //    scale = 1;
        //    return true;
        //}
    }
}
