using UnityEngine;
using UnityEngine.UI;

public class S_ItemSlot : MonoBehaviour
{
    public string itemName;
    public int quantity;
    public Sprite icon;
    public bool isFull;

    [SerializeField] private Image iconImage;

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
}
