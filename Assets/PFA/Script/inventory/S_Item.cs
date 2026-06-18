using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class S_Item : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity;
    [SerializeField]
    private Sprite icon;
    private S_InventoryManager inventoryManager;

    [SerializeField] 
    private KeyCode pickupKey = KeyCode.F;

    private bool playerInRange;
    [SerializeField] private TMP_Text pickUpText;

    void Start()
    {
        var go = GameObject.Find("Inventory");
        Debug.Log(go);
        inventoryManager = GameObject.Find("UI").GetComponent<S_InventoryManager>();
        pickUpText.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            SoundManager.PlaySound(SoundType.Pick);
            inventoryManager.AddItem(itemName, quantity, icon);

            pickUpText.gameObject.SetActive(false);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Entrez");
            playerInRange = true;

            pickUpText.GetComponent<TMP_Text>().text =

                $"Appuyez sur  ' {pickupKey} ' pour ramasser l'objet : {itemName}";

            pickUpText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Sortie");
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            pickUpText.gameObject.SetActive(false);
        }
    }
}
