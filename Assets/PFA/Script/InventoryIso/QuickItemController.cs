
using UnityEngine;
using UnityEngine.InputSystem;

public class QuickItemController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private S_HealthBar healthBar;

    [Header("Clone")]
    [SerializeField] private PlayerDecoyAbility playerDecoyAbility;

    private int cloneSlotIndex = -1;
    private DecoyClone activeClone;

    private void Update()
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
            return;

        // Le clone a disparu : retirer 1 objet.
        if (cloneSlotIndex >= 0 && activeClone == null)
        {
            FinishClone(inventory);
        }

        InventoryItem quickItem = inventory.GetQuickItem();

        if (quickItem == null)
            return;

        // Les grenades sont gérées séparément.
        if (quickItem.itemType == InventoryItemType.Grenade)
            return;

        bool useItem =
            (Keyboard.current != null &&
             Keyboard.current.fKey.wasPressedThisFrame) ||

            (Mouse.current != null &&
             Mouse.current.forwardButton.wasPressedThisFrame) ||

            (Gamepad.current != null &&
             Gamepad.current.buttonWest.wasPressedThisFrame);

        if (!useItem)
            return;

        switch (quickItem.itemType)
        {
            case InventoryItemType.Health:
                if (healthBar != null)
                    inventory.UseQuickItem(healthBar);
                break;

            case InventoryItemType.Clone:
                UseClone(inventory);
                break;
        }
    }

    private void UseClone(InventoryManager inventory)
    {
        if (inventory.GetQuickItemAmount() <= 0)
            return;

        if (cloneSlotIndex >= 0 || DecoyClone.IsActive)
            return;

        if (playerDecoyAbility == null)
        {
            Debug.LogWarning(
                "Player Decoy Ability non assigné."
            );
            return;
        }

        // C'est PlayerDecoyAbility qui crée le clone.
        bool activated = playerDecoyAbility.ActivateClone();

        if (!activated)
            return;

        // Mémorise le clone et son slot d'inventaire.
        activeClone = playerDecoyAbility.ActiveClone;
        cloneSlotIndex = inventory.QuickSlotIndex;
    }

    private void FinishClone(InventoryManager inventory)
    {
        int slotIndex = cloneSlotIndex;

        cloneSlotIndex = -1;
        activeClone = null;

        InventorySlotData slot = inventory.GetSlot(slotIndex);

        if (slot == null ||
            slot.IsEmpty ||
            slot.item == null ||
            slot.item.itemType != InventoryItemType.Clone)
        {
            Debug.LogWarning(
                "Clone terminé : objet introuvable dans le slot."
            );
            return;
        }

        // Décrémente avec ton InventoryManager existant.
        inventory.RemoveItem(slotIndex, 1);
    }
}

