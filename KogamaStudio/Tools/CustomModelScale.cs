using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace KogamaStudio.Tools;

[HarmonyPatch]
internal static class CustomModelScale
{
    internal static bool Enabled = false;
    internal static float Scale = 4;

    [HarmonyPatch(typeof(EditorWorldObjectCreation), "OnAddNewPrototype")]
    [HarmonyPrefix]
    private static void OnAddNewPrototype(ref float scale)
    {
        if (Enabled)
        {
            scale = Scale;
        }
    }
}