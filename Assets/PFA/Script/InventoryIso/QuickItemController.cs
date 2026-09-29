using UnityEngine;
using UnityEngine.InputSystem;

public class QuickItemController : MonoBehaviour
{
    [SerializeField]
    private S_HealthBar healthBar;

    private void Update()
    {
        bool useItem = false;

        // Clavier : F
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                useItem = true;
            }
        }

        // Souris : bouton latéral avant
        if (Mouse.current != null)
        {
            if (Mouse.current.forwardButton.wasPressedThisFrame)
            {
                useItem = true;
            }
        }
        // Manette : X 
        if (Gamepad.current != null) { if (Gamepad.current.buttonWest.wasPressedThisFrame) { useItem = true; } }

        if (!useItem)
            return;

        if (InventoryManager.Instance == null)
            return;

        if (healthBar == null)
            return;

        InventoryManager.Instance.UseQuickItem(healthBar);
    }
}
