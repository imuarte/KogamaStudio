using HarmonyLib;


namespace KogamaStudio.Inventory;

[HarmonyPatch]
internal class InventoryManager
{
    internal static bool AddingToInventory = false;

    internal static void RemoveItem(int itemId)
    {
        MVGameControllerBase.OperationRequests.RemoveItemFromInventory(itemId);
    }

    internal static void AddItem(int worldObjectID)
    {
        var game = MVGameControllerBase.Game.operationRequests;
        if (game == null) { KogamaStudio.Log.LogWarning("[Inventory] MVNetworkGame not found"); return; }
        AddingToInventory = true;
        game.AddWorldObjectToInventory(worldObjectID);
        AddingToInventory = false;
    }

    [HarmonyPatch(typeof(MaterialLoader), nameof(MaterialLoader.CheckAtlasIntegrity))]
    [HarmonyPrefix]
    private static bool CheckAtlasIntegrity_Prefix(ref bool __result)
    {
        if (!AddingToInventory) return true;
        __result = true;
        return false;
    }
}
