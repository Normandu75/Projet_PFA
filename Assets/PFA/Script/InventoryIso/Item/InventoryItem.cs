using UnityEngine;

public enum InventoryItemType
{
    Health,
    Grenade,
    EnergyDrink
}

[CreateAssetMenu(
    fileName = "NewInventoryItem",
    menuName = "Inventory/Item"
)]
public class InventoryItem : ScriptableObject
{
    [Header("Identity")]
    public string itemName;

    public InventoryItemType itemType;

    [Header("Visual")]
    public Sprite icon;

    [Header("Stack")]
    [Min(1)]
    public int maxStack = 5;

    [Header("Health")]
    public int healAmount = 25;

    [Header("Energy")]
    public int energyAmount = 25;

    [Header("Grenade")]
    public GameObject grenadePrefab;
}
