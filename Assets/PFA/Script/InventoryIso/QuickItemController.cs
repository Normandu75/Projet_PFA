using UnityEngine;
using UnityEngine.InputSystem;

public class QuickItemController : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    private void Update()
    {
        bool useItem = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                useItem = true;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                useItem = true;
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                useItem = true;
            }
        }

        if (!useItem)
            return;

        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.UseQuickItem(
            playerHealth
        );
    }
}

