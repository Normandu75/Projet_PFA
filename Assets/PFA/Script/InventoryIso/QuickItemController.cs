using UnityEngine;
using UnityEngine.InputSystem;

public class QuickItemController : MonoBehaviour
{
    [SerializeField]
    private S_HealthBar healthBar;

    private void Update()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryItem quickItem =
            InventoryManager.Instance.GetQuickItem();

        // Une grenade est gérée par GrenadeThrow :
        // LT = viser
        // RT = lancer
        if (quickItem != null &&
            quickItem.itemType == InventoryItemType.Grenade)
        {
            return;
        }

        bool useItem = false;

        // Clavier : F
        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            useItem = true;
        }

        // Souris : bouton latéral avant
        if (Mouse.current != null &&
            Mouse.current.forwardButton.wasPressedThisFrame)
        {
            useItem = true;
        }

        // Manette : X
        if (Gamepad.current != null &&
            Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            useItem = true;
        }

        if (!useItem)
            return;

        if (healthBar == null)
            return;

        InventoryManager.Instance.UseQuickItem(healthBar);
    }
}
