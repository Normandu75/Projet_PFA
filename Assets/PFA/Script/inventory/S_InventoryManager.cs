using UnityEngine.UI;
using UnityEngine;

public class S_InventoryManager : MonoBehaviour
{
    public GameObject InventoryMenu; 
    public S_ItemSlot[] itemSlot;
    private bool menuActivated; 
    public S_Door nearDoor;
    public S_ItemSO[] itemSOs;
    
    public GameObject backgroundBackPack; 
    public GameObject tabUi; 
    private S_Controller movement;
    public GameObject locked;
    public GameObject cross;
    
    void Start()
    {
        
        cross.SetActive(false);
        locked.SetActive(false);
        
        movement = GameObject.Find("Character").GetComponent<S_Controller>();
    }
    // -----------------------------------------
    // OUVRE L'INVENTAIRE REF:DEBUG.LOG LIGNE 32
    // -----------------------------------------
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab) && menuActivated)
        {
            SoundManager.PlaySound(SoundType.Inventory1);
            cross.SetActive(false);
            locked.SetActive(false);
            InventoryMenu.SetActive(false);
            backgroundBackPack.SetActive(false);
            tabUi.SetActive(true);
            menuActivated = false;
            Debug.Log("AAAAH");
            movement.speed = 6;
        }
        else if(Input.GetKeyDown(KeyCode.Tab) && !menuActivated)
        {
            SoundManager.PlaySound(SoundType.Inventory2);
            InventoryMenu.SetActive(true);
            backgroundBackPack.SetActive(true);
            cross.SetActive(true);
            locked.SetActive(true);
            tabUi.SetActive(true);
            menuActivated = true;
            Debug.Log("FAAAAH");
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
