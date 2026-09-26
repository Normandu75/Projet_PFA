using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory")]
    [SerializeField] private int slotCount = 8;

    [SerializeField]
    private InventorySlotData[] slots;

    [Header("Quick Slot")]
    [SerializeField]
    private int quickSlotIndex = -1;

    public int SlotCount => slotCount;

    public InventorySlotData[] Slots => slots;

    public int QuickSlotIndex => quickSlotIndex;

    public event Action OnInventoryChanged;
    public event Action<int> OnQuickSlotChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeInventory();
    }

    private void InitializeInventory()
    {
        if (slots == null || slots.Length != slotCount)
        {
            slots = new InventorySlotData[slotCount];
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = new InventorySlotData();
            }
        }
    }

    public bool AddItem(InventoryItem item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

        // 1. Remplir les stacks existants
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item && !slots[i].IsFull)
            {
                int space = item.maxStack - slots[i].amount;
                int addAmount = Mathf.Min(space, remaining);

                slots[i].amount += addAmount;
                remaining -= addAmount;

                if (remaining <= 0)
                {
                    NotifyInventoryChanged();
                    return true;
                }
            }
        }

        // 2. Chercher des slots vides
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                int addAmount = Mathf.Min(item.maxStack, remaining);

                slots[i].item = item;
                slots[i].amount = addAmount;

                remaining -= addAmount;

                if (remaining <= 0)
                {
                    NotifyInventoryChanged();
                    return true;
                }
            }
        }

        NotifyInventoryChanged();

        // false = impossible de tout ajouter
        return false;
    }

    public bool RemoveItem(int slotIndex, int amount = 1)
    {
        if (!IsValidIndex(slotIndex))
            return false;

        InventorySlotData slot = slots[slotIndex];

        if (slot.IsEmpty)
            return false;

        slot.amount -= amount;

        if (slot.amount <= 0)
        {
            slot.Clear();

            if (quickSlotIndex == slotIndex)
            {
                quickSlotIndex = -1;
                OnQuickSlotChanged?.Invoke(quickSlotIndex);
            }
        }

        NotifyInventoryChanged();

        return true;
    }

    public bool SetQuickSlot(int index)
    {
        if (!IsValidIndex(index))
            return false;

        if (slots[index].IsEmpty)
            return false;

        quickSlotIndex = index;

        OnQuickSlotChanged?.Invoke(quickSlotIndex);

        return true;
    }

    public InventoryItem GetQuickItem()
    {
        if (!IsValidIndex(quickSlotIndex))
            return null;

        return slots[quickSlotIndex].item;
    }

    public int GetQuickItemAmount()
    {
        if (!IsValidIndex(quickSlotIndex))
            return 0;

        return slots[quickSlotIndex].amount;
    }

    public bool UseQuickItem(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
            return false;

        if (!IsValidIndex(quickSlotIndex))
            return false;

        InventorySlotData slot = slots[quickSlotIndex];

        if (slot.IsEmpty)
            return false;

        InventoryItem item = slot.item;

        switch (item.itemType)
        {
            case InventoryItemType.Health:

                bool healed = playerHealth.Heal(item.healAmount);

                if (healed)
                {
                    RemoveItem(quickSlotIndex, 1);
                    return true;
                }

                break;
        }

        return false;
    }

    public InventorySlotData GetSlot(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return slots[index];
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Length;
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
