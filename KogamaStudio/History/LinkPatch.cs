using HarmonyLib;
using Il2Cpp;
using MV.WorldObject;

namespace KogamaStudio.History;

[HarmonyPatch]
internal static class LinkPatch
{
    internal static bool SuppressNext = false;
    internal static bool SuppressNextObjectLink = false;

    // --- Regular links ---

    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), nameof(MVNetworkGame.OperationRequests.AddLink))]
    [HarmonyPrefix]
    private static void AddLinkPrefix(Link link)
    {
        if (link == null || SuppressNext) { SuppressNext = false; return; }
        PipeClient.SendCommand($"history_link_added|{link.outputWOID}|{link.inputWOID}");
    }

    [HarmonyPatch(typeof(Links), nameof(Links.RemoveLink))]
    [HarmonyPrefix]
    private static void RemoveLinkPrefix(int linkID, MVWorldObjectClient outputWo, MVWorldObjectClient inputWo)
    {
        if (SuppressNext) { SuppressNext = false; return; }
        if (outputWo == null || inputWo == null) return;
        PipeClient.SendCommand($"history_link_removed|{outputWo.Id}|{inputWo.Id}");
    }

    // --- Object links ---

    [HarmonyPatch(typeof(MVNetworkGame.OperationRequests), nameof(MVNetworkGame.OperationRequests.AddObjectLink))]
    [HarmonyPrefix]
    private static void AddObjectLinkPrefix(ObjectLink link)
    {
        if (link == null || SuppressNextObjectLink) { SuppressNextObjectLink = false; return; }
        PipeClient.SendCommand($"history_object_link_added|{link.objectConnectorWOID}|{link.objectWOID}");
    }

    [HarmonyPatch(typeof(ObjectLinks), nameof(ObjectLinks.RemoveObjectLink))]
    [HarmonyPrefix]
    private static void RemoveObjectLinkPrefix(ObjectLink link, MVWorldObjectClient objectConnectorWo, MVWorldObjectClient objectWo)
    {
        if (SuppressNextObjectLink) { SuppressNextObjectLink = false; return; }
        if (objectConnectorWo == null || objectWo == null) return;
        PipeClient.SendCommand($"history_object_link_removed|{objectConnectorWo.Id}|{objectWo.Id}");
    }
}
