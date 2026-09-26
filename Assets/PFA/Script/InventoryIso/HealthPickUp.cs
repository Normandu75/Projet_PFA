using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField]
    private InventoryItem item;

    [SerializeField]
    private int amount = 1;

    [Header("Interaction")]
    [SerializeField]
    private bool destroyAfterPickup = true;

    public void Pickup()
    {
        if (InventoryManager.Instance == null)
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Pickup();
    }
}
