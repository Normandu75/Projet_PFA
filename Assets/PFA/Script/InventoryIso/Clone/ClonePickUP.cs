using UnityEngine;
using UnityEngine.InputSystem;

public class ClonePickUp : MonoBehaviour
{
    [Header("Item Data Asset")]
    [SerializeField] private InventoryItem item;

    [Header("Quantity")]
    [SerializeField] private int amount = 1;

    [Header("UI")]
    [SerializeField] private PickupPanelUI pickupPanel;

    [SerializeField] private bool destroyAfterPickup = true;

    private bool playerInRange;

    private void Start()
    {
        if (pickupPanel != null)
        {
            pickupPanel.SetItem(item, amount);
            pickupPanel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            Pickup();
        }

        if (Keyboard.current != null &&
            Keyboard.current.vKey.wasPressedThisFrame)
        {
            Pickup();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (pickupPanel != null)
        {
            pickupPanel.SetItem(item, amount);
            pickupPanel.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (pickupPanel != null)
            pickupPanel.gameObject.SetActive(false);
    }

    private void Pickup()
    {
        if (InventoryManager.Instance == null || item == null)
            return;

        bool added = InventoryManager.Instance.AddItem(
            item,
            amount
        );

        if (added && destroyAfterPickup)
        {
            Destroy(gameObject);
        }
    }
}
