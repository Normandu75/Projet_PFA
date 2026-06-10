using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class S_ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public string itemName;
    public int quantity;
    public Sprite icon;
    public Sprite emptySprite;
    public bool isFull;
    [SerializeField] private Image iconImage;


    public GameObject selectedShader;
    public bool thisItemSelected;
    private S_InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("UI").GetComponent<S_InventoryManager>();
    }
    public void AddItem(string itemName, int quantity, Sprite icon)
    {
        this.itemName = itemName;
        this.quantity = quantity;
        this.icon = icon;
        isFull = true;
        iconImage.sprite = icon;
        iconImage.enabled = true;
        iconImage.gameObject.SetActive(true);
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }
    public void OnLeftClick()
    {
        if (thisItemSelected)
        {
            bool usable = inventoryManager.UseItem(itemName);
            if(usable)
            {
                this.quantity -= 1;
                if(this.quantity <= 0)
                    EmptySlot();
            }
            Debug.Log("Click sur objet : " + itemName);
        }
        else
        {
            inventoryManager.DeselectAllSlots();
            selectedShader.SetActive(true);
            thisItemSelected = true;
        }
        
    }
    private void EmptySlot()
    {
        quantity = 0;
        itemName = "";
        icon = null;
        isFull = false;                          
        iconImage.sprite = emptySprite;           
        selectedShader.SetActive(false);
        thisItemSelected = false;
    }
    public void OnRightClick()
    {
        
    }
}
