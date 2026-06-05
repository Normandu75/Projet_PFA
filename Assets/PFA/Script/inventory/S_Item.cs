using UnityEngine.UI;
using UnityEngine;

public class S_Item : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity;
    [SerializeField]
    private Sprite icon;
    private S_InventoryManager inventoryManager;

    void Start()
    {
        var go = GameObject.Find("Inventory");
        Debug.Log(go);
        inventoryManager = GameObject.Find("UI").GetComponent<S_InventoryManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            Debug.Log(" Touch ");
            inventoryManager.AddItem(itemName, quantity, icon);
            Destroy(gameObject);
        }
    }
}
