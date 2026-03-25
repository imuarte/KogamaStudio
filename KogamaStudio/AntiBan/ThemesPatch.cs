using System;
using HarmonyLib;

namespace KogamaStudio.AntiBan;

[HarmonyPatch(typeof(Theme), "Initialize", new Type[] { })]
internal static class BlockThemeInit
{
    [HarmonyPrefix]
    private static bool Prefix() => false;
}

[HarmonyPatch(typeof(Theme), "Initialize", new Type[] { typeof(int) })]
internal static class BlockThemeInitInt
{
    [HarmonyPrefix]
    private static bool Prefix() => false;
}

[HarmonyPatch(typeof(ThemeSkybox), "Activate")]
internal static class BlockSkyboxActivate
{
    [HarmonyPrefix]
    private static bool Prefix() => false;
}
