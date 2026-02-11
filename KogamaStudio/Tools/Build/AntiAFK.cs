using HarmonyLib;
using Il2Cpp;
using System.Runtime.InteropServices;

namespace KogamaStudio.Tools.Build;

internal static class AntiAFK
{
    public static bool Enabled
    {
        get => !AwayMonitor.IdleKickEnabled;
        set => AwayMonitor.IdleKickEnabled = !value;
    }

}