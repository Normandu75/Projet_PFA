using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text actionText;

    [SerializeField] private Image itemIcon;

    public void SetItem(InventoryItem item, int amount)
    {
        if (item == null)
            return;

        if (nameText != null)
            nameText.text = item.inventoryName;

        if (descriptionText != null)
            descriptionText.text = item.inventoryDescription;

        if (actionText != null)
            actionText.text = item.inventoryAction;

        if (amountText != null)
            amountText.text = $"x{amount}";

        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
        }
    }
}