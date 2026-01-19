using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;

namespace KogamaStudio.AntiBan
{
    [HarmonyPatch]

    internal class ThemesPatch
    {
        [HarmonyPatch(typeof(Theme), "Activate")]
        [HarmonyPrefix]
        private static bool ThemeActivatePrefix()
        {
            return false;
        }
    }
}
