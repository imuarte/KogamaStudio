using HarmonyLib;
using ExitGames.Client.Photon;

namespace KogamaStudio;

[HarmonyPatch(typeof(PhotonPeer), "SendOperation")]
public class PhotonJoinCapture
{
    static void Prefix(PhotonPeer __instance, object[] __args)
    {
        if (__args == null || __args.Length < 1) return;
        byte opCode = (byte)__args[0];
        if (opCode != 255) return;

        var secret = typeof(PhotonPeer)
            .GetField("PayloadEncryptionSecret",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            ?.GetValue(__instance) as byte[];

        if (secret != null)
            KogamaStudio.Log.LogInfo($"[KogamaScripts] Secret: {System.Text.Encoding.UTF8.GetString(secret)}");
        else
            KogamaStudio.Log.LogInfo($"[KogamaScripts] Secret: null");
    }
}