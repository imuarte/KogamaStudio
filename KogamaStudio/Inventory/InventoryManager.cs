using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio.Inventory;

internal class InventoryManager
{
    internal static void RemoveItem(int itemId)
    {
        MVGameControllerBase.OperationRequests.RequestRemoveItemFromMarketPlace(itemId);
    }
}
