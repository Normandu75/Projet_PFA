using UnityEngine.UI;
using UnityEngine;

public class S_InventoryManager : MonoBehaviour
{
    public GameObject InventoryMenu; 
    public S_ItemSlot[] itemSlot;
    private bool menuActivated; 
    public S_Door nearDoor;
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
            S_Controller movement = GameObject.Find("Character").GetComponent<S_Controller>();
            movement.speed = 6;
        }
        else if(Input.GetKeyDown(KeyCode.Tab) && !menuActivated)
        {
            InventoryMenu.SetActive(true);
            menuActivated = true;
            Debug.Log("FAAAAH");
            S_Controller movement = GameObject.Find("Character").GetComponent<S_Controller>();
            movement.speed = 0;
        }
        if (!menuActivated)
        {
            DeselectAllSlots();
        }
            
    } 
    
    public bool UseItem(string itemName)
    { 
        Debug.Log("Objet utilisé : " + itemName);
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if(itemSOs[i].itemName == itemName)
            {
                bool usable = itemSOs[i].UseItem(nearDoor);
                return usable;
            }
        }
        return false;
    } 
    
    public void AddItem(string itemName, int quantity, Sprite icon)
    {
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
    public void SetNearDoor(S_Door door)
    {
        nearDoor = door;
        Debug.Log("Porte détectée");
    }
}
