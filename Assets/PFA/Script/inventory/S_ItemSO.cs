using UnityEngine;
[CreateAssetMenu]
public class S_ItemSO : ScriptableObject
{
    public string itemName;
    public StatToChange statToChange = new StatToChange();
    public int amountToChangeStat;

    public void UseItem()
    {
        if(statToChange == StatToChange.health)
        {
            GameObject.Find("Character").GetComponent<S_HealthBar>().AddHealth(amountToChangeStat);
            Debug.Log("Heal de 20 HP");
        }
    }

    public enum StatToChange
    {
        none,
        health,
        stamina
    };
}
