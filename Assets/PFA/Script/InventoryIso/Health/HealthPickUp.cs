using UnityEngine;
using UnityEngine.InputSystem;

public class HealthPickup : MonoBehaviour
{
    [Header("Item Data Asset")]
    [SerializeField] private InventoryItem item;

    [Header("Quantity")]
    [SerializeField] private int amount = 1;

    [Header("UI")]
    [SerializeField] private PickupPanelUI pickupPanel;

    [Header("Interaction")]
    [SerializeField] private bool destroyAfterPickup = true;

    private bool playerInRange;

    private void Start()
    {
        // Cache l'UI au démarrage
        if (pickupPanel != null)
            pickupPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        // Bouton A Xbox / Button South
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
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
            // Envoie le Data Asset + quantité au panel
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
        if (InventoryManager.Instance == null)
            return;

        bool added = InventoryManager.Instance.AddItem(
            item,
            amount
        );

        if (added && destroyAfterPickup)
            Destroy(gameObject);
    }
}