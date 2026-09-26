using System;

[Serializable]
public class InventorySlotData
{
    public InventoryItem item;
    public int amount;

    public bool IsEmpty
    {
        get { return item == null || amount <= 0; }
    }

    public bool IsFull
    {
        get
        {
            return item != null && amount >= item.maxStack;
        }
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}