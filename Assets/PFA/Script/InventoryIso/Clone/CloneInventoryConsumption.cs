
using UnityEngine;

public class CloneInventoryConsumption : MonoBehaviour
{
    private InventoryManager inventory;
    private InventoryItem cloneItem;
    private int slotIndex = -1;

    private bool initialized;
    private bool consumed;

    public void Initialize(
        InventoryManager inventoryManager,
        int inventorySlotIndex,
        InventoryItem item)
    {
        inventory = inventoryManager;
        slotIndex = inventorySlotIndex;
        cloneItem = item;

        initialized = true;
    }

    private void OnDestroy()
    {
        if (!initialized || consumed)
            return;

        if (inventory == null || cloneItem == null)
            return;

        InventorySlotData slot = inventory.GetSlot(slotIndex);

        // Ne pas retirer un autre objet si le slot a changé.
        if (slot == null ||
            slot.IsEmpty ||
            slot.item != cloneItem)
        {
            return;
        }

        consumed = inventory.RemoveItem(slotIndex, 1);
    }
}

