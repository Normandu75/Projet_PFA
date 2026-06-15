using UnityEngine;
using TMPro;

public class S_Door : MonoBehaviour
{
    public float interactionRadius = 2.5f;
    public string requiredKeyName = "Key";
    public TextMeshProUGUI proximityText;

    private bool isLocked = true;
    private S_InventoryManager inventoryManager;
    private Transform player;

    void Start()
    {
        inventoryManager = GameObject.Find("UI").GetComponent<S_InventoryManager>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (proximityText != null)
        {
            proximityText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, transform.position);
        bool isNear = dist <= interactionRadius;

        if (proximityText != null)
        {
            proximityText.gameObject.SetActive(isNear && isLocked);
        }
        if (isNear && isLocked)
        {
            inventoryManager.SetNearDoor(this);
        }
        else if (inventoryManager.nearDoor == this)
        {
            inventoryManager.SetNearDoor(null);
        }
    }

    // -----------------------------------------
    // OUVRE LA PORTE ICI REF:DEBUG.LOG LIGNE 47
    // -----------------------------------------
    
    public bool TryUnlock(string keyName)
    {
        if (keyName != requiredKeyName) return false;
        Debug.Log("Porte déverrouillée !");
        isLocked = false;
        inventoryManager.SetNearDoor(null);

        if (proximityText != null)
        {
            proximityText.gameObject.SetActive(false);
        }

        Destroy(gameObject);
        return true;
    }
}