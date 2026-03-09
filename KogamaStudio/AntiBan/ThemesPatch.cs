//13031414  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using KogamaModFramework.Operations;

namespace KogamaStudio.AntiBan
{
    [HarmonyPatch]

    internal class ThemesPatch
    {
        [HarmonyPatch(typeof(Theme), "Initialize", [typeof(int)])]
        [HarmonyPrefix]
        private static bool ThemeInitializeIntPrefix(int woid)
        {
            var wo = WorldObjectOperations.GetObject(woid) as MVWorldObjectClient;
            if (wo != null) return false;

            wo.Destroy();
            return false;
        }
    }
}