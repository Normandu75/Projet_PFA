using UnityEngine.UI;
using UnityEngine;

public class S_InventoryManager : MonoBehaviour
{
    public GameObject InventoryMenu; 
    public S_ItemSlot[] itemSlot;
    private bool menuActivated; 
    public S_ItemSO[] itemSOs;
    void Start()
    {
        
    }

    void Update()
    {
        //Ne fonctionne pas. La touche n'est pas detecté lorce qu'elle est pressed. 
        if(Input.GetKeyDown(KeyCode.Tab) && menuActivated)
        {
            InventoryMenu.SetActive(false);
            menuActivated = false;
            Debug.Log("AAAAH");
        }
        else if(Input.GetKeyDown(KeyCode.Tab) && !menuActivated)
        {
            InventoryMenu.SetActive(true);
            menuActivated = true;
            Debug.Log("FAAAAH");
        }
    }
    public void UseItem(string itemName)
    {
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if(itemSOs[i].itemName == itemName)
            {
                itemSOs[i].UseItem();
            }
        }
    }
    public void AddItem(string itemName, int quantity, Sprite icon)
    {
        Debug.Log("Item Name : " + itemName + ", Quantity : " + quantity + ", Icon : " + icon);
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if(itemSlot[i].isFull == false)
            {
                itemSlot[i].AddItem(itemName, quantity, icon);
                return;
            }
        }
    }
    public void DeselectAllSlots()
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].selectedShader.SetActive(false);
            itemSlot[i].thisItemSelected = false;
        }
    }
}
